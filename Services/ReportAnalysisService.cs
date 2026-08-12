using System.Text.RegularExpressions;
using MedVaultAPI.Data;
using MedVaultAPI.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using Tesseract;

namespace MedVaultAPI.Services
{
    /// <summary>
    /// V1 medical report analysis service.
    /// No interface by design - injected directly as a concrete class.
    ///
    /// Pipeline:
    ///   Uploaded report -> extract text -> detect report date -> detect report type
    ///   -> detect medical topics -> extract measurements -> save to database.
    ///
    /// Text extraction has two entry points that both feed the SAME downstream
    /// pipeline below:
    ///   PDF files   -> ExtractTextFromPdf()   (UglyToad.PdfPig, unchanged from V1)
    ///   Image files -> ExtractTextFromImage() (Tesseract OCR)
    /// Everything after text extraction (date/type/topic/measurement detection)
    /// has no idea whether the text came from a PDF or an image, and never should.
    ///
    /// Everything here is intentionally simple (regex + keyword dictionaries) so it
    /// is easy to read, run locally, and extend later (e.g. scanned-PDF OCR, TF-IDF ranking).
    /// </summary>
    public class ReportAnalysisService
    {
        private readonly MedVaultDbContext _db;
        private readonly ILogger<ReportAnalysisService> _logger;

        // OCR configuration - read from appsettings.json if present, otherwise
        // falls back to sensible defaults so nothing breaks if the section is missing.
        private readonly string _tessDataPath;
        private readonly string _ocrLanguage;

        public ReportAnalysisService(
            MedVaultDbContext db,
            IConfiguration configuration,
            ILogger<ReportAnalysisService> logger)
        {
            _db = db;
            _logger = logger;

            var configuredPath = configuration["OCR:TessDataPath"];
            var configuredLanguage = configuration["OCR:Language"];

            _tessDataPath = string.IsNullOrWhiteSpace(configuredPath)
                ? Path.Combine(Directory.GetCurrentDirectory(), "Tessdata")
                : Path.Combine(Directory.GetCurrentDirectory(), configuredPath);

            _ocrLanguage = string.IsNullOrWhiteSpace(configuredLanguage)
                ? "eng"
                : configuredLanguage;
        }

        // ---------------------------------------------------------------
        // Keyword dictionaries - kept public/static so SmartSearchService
        // can reuse the exact same vocabulary when parsing search queries.
        // Easy to extend: just add entries here.
        // ---------------------------------------------------------------

        public static readonly List<(string Type, List<string> Keywords)> ReportTypeKeywords = new()
        {
            ("MRI", new() { "mri", "mri scan", "magnetic resonance" }),
            ("X-Ray", new() { "x-ray", "xray", "radiograph" }),
            ("CT Scan", new() { "ct scan", "computed tomography" }),
            ("Ultrasound", new() { "ultrasound", "sonography" }),
            ("Eye Examination", new() { "eye examination", "ophthalmology", "retina" }),
            ("Blood Test", new() { "blood test", "cbc", "complete blood count", "creatinine", "hemoglobin" }),
        };

        public static readonly Dictionary<string, List<string>> TopicKeywords = new()
        {
            ["Kidney"] = new() { "kidney", "renal", "creatinine", "egfr", "urea" },
            ["Back Pain"] = new() { "back pain", "lower back", "lumbar", "spine", "sciatica", "l4-l5" },
            ["Eye"] = new() { "eye", "retina", "ophthalmology", "vision" },
            ["Blood"] = new() { "blood", "cbc", "hemoglobin", "rbc", "wbc", "platelet" },
        };

        // Non-blood-pressure measurements: name -> (regex, default unit).
        // Regex captures the numeric value in group 1 and, if present, the unit in group 2.
        private static readonly List<(string Type, Regex Pattern, string DefaultUnit)> MeasurementPatterns = new()
        {
            ("Creatinine", new Regex(@"creatinine\s*[:\-]?\s*([\d.]+)\s*(mg/dl)?", RegexOptions.IgnoreCase), "mg/dL"),
            ("Calcium", new Regex(@"calcium\s*[:\-]?\s*([\d.]+)\s*(mg/dl)?", RegexOptions.IgnoreCase), "mg/dL"),
            ("RBC", new Regex(@"\brbc\s*[:\-]?\s*([\d.]+)\s*(million/ul)?", RegexOptions.IgnoreCase), "million/uL"),
            ("WBC", new Regex(@"\bwbc\s*[:\-]?\s*([\d.]+)\s*(/ul|cells/ul)?", RegexOptions.IgnoreCase), "/uL"),
            ("Hemoglobin", new Regex(@"hemoglobin\s*[:\-]?\s*([\d.]+)\s*(g/dl)?", RegexOptions.IgnoreCase), "g/dL"),
            ("Glucose", new Regex(@"glucose\s*[:\-]?\s*([\d.]+)\s*(mg/dl)?", RegexOptions.IgnoreCase), "mg/dL"),
            ("Cholesterol", new Regex(@"cholesterol\s*[:\-]?\s*([\d.]+)\s*(mg/dl)?", RegexOptions.IgnoreCase), "mg/dL"),
            ("eGFR", new Regex(@"egfr\s*[:\-]?\s*([\d.]+)\s*(ml/min)?", RegexOptions.IgnoreCase), "mL/min"),
        };

        private static readonly Regex BloodPressurePattern = new(
            @"blood\s*pressure\s*[:\-]?\s*(\d{2,3})\s*/\s*(\d{2,3})\s*(mmhg)?",
            RegexOptions.IgnoreCase);

        public static readonly List<string> MeasurementTypeNames =
            MeasurementPatterns.Select(m => m.Type).Append("Blood Pressure").ToList();

        // ---------------------------------------------------------------
        // Entry point
        // ---------------------------------------------------------------

        public async Task AnalyzeDocumentAsync(MedDocument document)
        {
            document.ProcessingStatus = "Processing";
            await _db.SaveChangesAsync();

            try
            {
                var text = ExtractText(document);

                if (string.IsNullOrWhiteSpace(text))
                {
                    // No extractable text (e.g. scanned/image-only PDF, or an
                    // unsupported file type). Do not pretend we analyzed it.
                    document.ExtractedText = null;
                    document.ProcessingStatus = "Failed";
                    await _db.SaveChangesAsync();
                    return;
                }

                document.ExtractedText = text;

                var normalizedText = text.ToLowerInvariant();

                document.ReportDate = DetectReportDate(text);
                document.ReportType = DetectReportType(normalizedText);

                await SaveTopicsAsync(document, normalizedText);
                await SaveMeasurementsAsync(document, text);

                document.ProcessingStatus = "Completed";
                await _db.SaveChangesAsync();
            }
            catch
            {
                document.ProcessingStatus = "Failed";
                await _db.SaveChangesAsync();
                // Swallow the exception on purpose: a failed analysis must never
                // take down document creation/upload. Add logging here if desired.
            }
        }

        // ---------------------------------------------------------------
        // 1. Text extraction
        // ---------------------------------------------------------------

        // Dispatches by file extension to the right extractor. Both branches
        // return plain text and feed the exact same downstream pipeline -
        // there is only ONE analysis pipeline below this point, regardless
        // of whether the source was a PDF or an image.
        private string? ExtractText(MedDocument document)
        {
            // Never trust the raw filename for path construction beyond taking
            // just the file name component; we only ever read from Uploads/.
            var safeFileName = Path.GetFileName(document.FileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", safeFileName);

            if (!File.Exists(filePath))
            {
                return null;
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            // PDF files use the existing PdfPig extraction. Unchanged from V1.
            if (extension == ".pdf")
            {
                return ExtractTextFromPdf(filePath);
            }

           
            if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
            {
                return ExtractTextFromImage(filePath);
            }

            // Unsupported extension for V1 analysis (e.g. .docx). Upload itself
            // is unaffected - this only controls whether ReportAnalysisService
            // can analyze the file.
            return null;
        }

        // --- UNCHANGED from the original PDF-only implementation. ---
        private string? ExtractTextFromPdf(string filePath)
        {
            using var pdf = PdfDocument.Open(filePath);

            var textBuilder = new System.Text.StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                textBuilder.AppendLine(page.Text);
            }

            var text = textBuilder.ToString();

            // A text-based PDF should yield a meaningful amount of text.
            // If it's effectively empty, this is likely a scanned/image-only PDF.
            // Scanned-PDF OCR is explicitly out of scope for this version -
            // it continues to be reported as a failed analysis, same as before.
            if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 20)
            {
                return null;
            }

            return text;
        }

        // --- New: image OCR extraction. ---
        // Responsible ONLY for turning an image into text via Tesseract.
        // It must never do date/topic/measurement detection - that stays in
        // the shared pipeline below so PDF and image reports are analyzed
        // identically once we have plain text.
        private string? ExtractTextFromImage(string filePath)
        {
            if (!File.Exists(Path.Combine(_tessDataPath, $"{_ocrLanguage}.traineddata")))
            {
                _logger.LogError(
                    "OCR language data not found at {TessDataPath} for language '{Language}'. " +
                    "Image analysis cannot proceed until '{Language}.traineddata' is placed there.",
                    _tessDataPath, _ocrLanguage, _ocrLanguage);
                return null;
            }

            try
            {
                using var engine = new TesseractEngine(_tessDataPath, _ocrLanguage, EngineMode.Default);
                using var image = Pix.LoadFromFile(filePath);
                using var page = engine.Process(image);

                var text = page.GetText();

                // Same minimum-length rule as PDF extraction, for consistent behavior:
                // "insufficient text" means Failed either way.
                if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 20)
                {
                    return null;
                }

                return text;
            }
            catch (Exception ex)
            {
                // Covers: corrupt/invalid image files, unreadable formats, or any
                // native Tesseract/Leptonica failure. Never let this crash analysis -
                // it's reported as a failed analysis just like any other extraction failure.
                _logger.LogError(ex, "OCR failed for file {FilePath}.", filePath);
                return null;
            }
        }

        // ---------------------------------------------------------------
        // 2. Report date detection
        // ---------------------------------------------------------------

        private static readonly Regex IsoDatePattern = new(@"\b(\d{4})-(\d{1,2})-(\d{1,2})\b");
        private static readonly Regex SlashOrDashDatePattern = new(@"\b(\d{1,2})[/-](\d{1,2})[/-](\d{4})\b");

        private DateTime? DetectReportDate(string text)
        {
            var isoMatch = IsoDatePattern.Match(text);
            if (isoMatch.Success && TryBuildDate(isoMatch.Groups[1].Value, isoMatch.Groups[2].Value, isoMatch.Groups[3].Value, out var isoDate))
            {
                return isoDate;
            }

            var slashMatch = SlashOrDashDatePattern.Match(text);
            if (slashMatch.Success && TryBuildDate(slashMatch.Groups[3].Value, slashMatch.Groups[2].Value, slashMatch.Groups[1].Value, out var slashDate))
            {
                // Groups are (day, month, year) for this pattern -> passed as (year, month, day).
                return slashDate;
            }

            return null;
        }

        private static bool TryBuildDate(string yearStr, string monthStr, string dayStr, out DateTime result)
        {
            result = default;
            if (!int.TryParse(yearStr, out var year)) return false;
            if (!int.TryParse(monthStr, out var month)) return false;
            if (!int.TryParse(dayStr, out var day)) return false;

            if (month < 1 || month > 12) return false;
            if (day < 1 || day > 31) return false;
            if (year < 1900 || year > 2200) return false;

            try
            {
                result = new DateTime(year, month, day);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ---------------------------------------------------------------
        // 3. Report type detection
        // ---------------------------------------------------------------

        private string DetectReportType(string normalizedText)
        {
            foreach (var (type, keywords) in ReportTypeKeywords)
            {
                if (keywords.Any(keyword => normalizedText.Contains(keyword)))
                {
                    return type;
                }
            }

            return "Medical Report";
        }

        // ---------------------------------------------------------------
        // 4. Medical topic detection
        // ---------------------------------------------------------------

        private async Task SaveTopicsAsync(MedDocument document, string normalizedText)
        {
            // Clear previous topics for this document so re-analysis doesn't duplicate rows.
            var existing = _db.MedicalTopic.Where(t => t.DocumentId == document.Id);
            _db.MedicalTopic.RemoveRange(existing);

            foreach (var (topic, keywords) in TopicKeywords)
            {
                var matchCount = keywords.Count(keyword => normalizedText.Contains(keyword));
                if (matchCount == 0)
                {
                    continue;
                }

                // Simple rule-based confidence, not a medically validated score.
                var confidence = matchCount >= 2 ? 0.9 : 0.6;

                _db.MedicalTopic.Add(new MedicalTopic
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = document.UserId,
                    DocumentId = document.Id,
                    Topic = topic,
                    ConfidenceScore = confidence
                });
            }

            await Task.CompletedTask;
        }

        // ---------------------------------------------------------------
        // 5. Measurement extraction
        // ---------------------------------------------------------------

        private async Task SaveMeasurementsAsync(MedDocument document, string text)
        {
            // Clear previous measurements for this document so re-analysis doesn't duplicate rows.
            var existing = _db.MedicalMeasurement.Where(m => m.DocumentId == document.Id);
            _db.MedicalMeasurement.RemoveRange(existing);

            foreach (var (type, pattern, defaultUnit) in MeasurementPatterns)
            {
                var match = pattern.Match(text);
                if (!match.Success) continue;

                if (!double.TryParse(match.Groups[1].Value, out var value)) continue;

                var unit = match.Groups.Count > 2 && match.Groups[2].Success
                    ? match.Groups[2].Value
                    : defaultUnit;

                _db.MedicalMeasurement.Add(new MedicalMeasurement
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = document.UserId,
                    DocumentId = document.Id,
                    MeasurementType = type,
                    Value = value,
                    SecondaryValue = null,
                    Unit = unit,
                    MeasuredDate = document.ReportDate
                });
            }

            var bpMatch = BloodPressurePattern.Match(text);
            if (bpMatch.Success
                && double.TryParse(bpMatch.Groups[1].Value, out var systolic)
                && double.TryParse(bpMatch.Groups[2].Value, out var diastolic))
            {
                _db.MedicalMeasurement.Add(new MedicalMeasurement
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = document.UserId,
                    DocumentId = document.Id,
                    MeasurementType = "Blood Pressure",
                    Value = systolic,
                    SecondaryValue = diastolic,
                    Unit = "mmHg",
                    MeasuredDate = document.ReportDate
                });
            }

            await Task.CompletedTask;
        }
    }
}