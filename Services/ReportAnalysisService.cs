using MedVaultAPI.Data;
using MedVaultAPI.Model;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using Tesseract;

namespace MedVaultAPI.Services
{
    public class ReportAnalysisService
    {
        private readonly MedVaultDbContext _db;
        private readonly ILogger<ReportAnalysisService> _logger;

        private readonly string _tessDataPath;
        private readonly string _ocrLanguage;

        // Minimum length for extracted text to be considered usable (shared by
        // both the PDF and image extraction paths so they agree on the bar).
        private const int MinExtractedTextLength = 20;

        // Tesseract accuracy drops sharply below roughly 300dpi-equivalent
        // resolution, which is common for phone photos of printed reports.
        // Images narrower than this are upscaled before OCR.
        private const int MinRecommendedWidthPx = 1600;

        // Below this mean confidence, OCR output is still used but flagged in
        // logs as potentially unreliable rather than silently trusted.
        private const float MinAcceptableConfidence = 0.6f;

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
            ("ECG", new() { "ecg", "ekg", "electrocardiogram" }),
            ("Lipid Profile", new() { "lipid profile", "cholesterol panel" }),
            ("Thyroid Panel", new() { "thyroid panel", "tsh test" }),
            ("Endoscopy", new() { "endoscopy", "colonoscopy", "gastroscopy" }),
        };

        public static readonly Dictionary<string, List<string>> TopicKeywords = new()
        {
            ["Kidney"] = new() { "kidney", "renal", "creatinine", "egfr", "urea" },
            ["Back Pain"] = new() { "back pain", "lower back", "lumbar", "spine", "sciatica", "l4-l5" },
            ["Eye"] = new() { "eye", "retina", "ophthalmology", "vision" },
            ["Blood"] = new() { "blood", "cbc", "hemoglobin", "rbc", "wbc", "platelet" },
            ["Thyroid"] = new() { "thyroid", "tsh", "t3", "t4" },
            ["Diabetes"] = new() { "diabetes", "glucose", "hba1c", "sugar" },
            ["Liver"] = new() { "liver", "hepatic", "alt", "ast", "sgpt", "sgot", "bilirubin" },
            ["Heart"] = new() { "heart", "cardiac", "ecg", "ekg", "cholesterol", "triglycerides" },
            ["Vitamin"] = new() { "vitamin", "deficiency" },
        };

        // Non-blood-pressure measurements: name -> (regex, default unit).
        // Regex captures the numeric value in group 1 and, if present, the unit in group 2.
        private static readonly List<(string Type, Regex Pattern, string DefaultUnit)> MeasurementPatterns = new()
        {
            ("Creatinine", new Regex(@"creatinine\s*[:\-]?\s*([\d.]+)\s*(mg/dl)?", RegexOptions.IgnoreCase), "mg/dL"),
            ("Calcium", new Regex(@"calcium\s*[:\-]?\s*([\d.]+)\s*(mg/dl)?", RegexOptions.IgnoreCase), "mg/dL"),
            ("Magnesium", new Regex(@"magnesium\s*[:\-]?\s*([\d.]+)\s*(mg/dl)?", RegexOptions.IgnoreCase), "mg/dL"),
            ("Sodium", new Regex(@"sodium\s*[:\-]?\s*([\d.]+)\s*(meq/l)?", RegexOptions.IgnoreCase), "mEq/L"),
            ("Potassium", new Regex(@"potassium\s*[:\-]?\s*([\d.]+)\s*(meq/l)?", RegexOptions.IgnoreCase), "mEq/L"),
            ("Urea", new Regex(@"urea\s*[:\-]?\s*([\d.]+)\s*(mg/dl)?", RegexOptions.IgnoreCase), "mg/dL"),
            ("Uric Acid", new Regex(@"uric\s*acid\s*[:\-]?\s*([\d.]+)\s*(mg/dl)?", RegexOptions.IgnoreCase), "mg/dL"),
            ("Bilirubin", new Regex(@"bilirubin\s*[:\-]?\s*([\d.]+)\s*(mg/dl)?", RegexOptions.IgnoreCase), "mg/dL"),
            ("HbA1c", new Regex(@"hba1c\s*[:\-]?\s*([\d.]+)\s*(%)?", RegexOptions.IgnoreCase), "%"),
            ("TSH", new Regex(@"tsh\s*[:\-]?\s*([\d.]+)\s*(uiu/ml|µiu/ml)?", RegexOptions.IgnoreCase), "µIU/mL"),
            ("Vitamin D", new Regex(@"vitamin\s*d\s*[:\-]?\s*([\d.]+)\s*(ng/ml)?", RegexOptions.IgnoreCase), "ng/mL"),
            ("Vitamin B12", new Regex(@"vitamin\s*b12\s*[:\-]?\s*([\d.]+)\s*(pg/ml)?", RegexOptions.IgnoreCase), "pg/mL"),
            ("Iron", new Regex(@"\biron\s*[:\-]?\s*([\d.]+)\s*(ug/dl|µg/dl)?", RegexOptions.IgnoreCase), "µg/dL"),
            ("Ferritin", new Regex(@"ferritin\s*[:\-]?\s*([\d.]+)\s*(ng/ml)?", RegexOptions.IgnoreCase), "ng/mL"),
            ("Triglycerides", new Regex(@"triglycerides\s*[:\-]?\s*([\d.]+)\s*(mg/dl)?", RegexOptions.IgnoreCase), "mg/dL"),
            ("HDL", new Regex(@"\bhdl\s*[:\-]?\s*([\d.]+)\s*(mg/dl)?", RegexOptions.IgnoreCase), "mg/dL"),
            ("LDL", new Regex(@"\bldl\s*[:\-]?\s*([\d.]+)\s*(mg/dl)?", RegexOptions.IgnoreCase), "mg/dL"),
            ("Platelet Count", new Regex(@"platelet\s*count\s*[:\-]?\s*([\d.]+)\s*(/ul|cells/ul)?", RegexOptions.IgnoreCase), "/uL"),
            ("ALT", new Regex(@"(?:alt|sgpt)\s*[:\-]?\s*([\d.]+)\s*(u/l)?", RegexOptions.IgnoreCase), "U/L"),
            ("AST", new Regex(@"(?:ast|sgot)\s*[:\-]?\s*([\d.]+)\s*(u/l)?", RegexOptions.IgnoreCase), "U/L"),
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
                    // No extractable text (e.g. scanned/image-only PDF, unreadable
                    // image, or an unsupported file type). Do not pretend we analyzed it.
                    document.ExtractedText = null;
                    document.ProcessingStatus = "Unscanned";
                    await _db.SaveChangesAsync();
                    return;
                }

                document.ExtractedText = text;
                var normalizedText = text.ToLowerInvariant();

                document.ReportDate = DetectReportDate(text);
                document.ReportType = DetectReportType(normalizedText);

                await SaveTopicsAsync(document, normalizedText);
                await SaveMeasurementsAsync(document, text);

                document.ProcessingStatus = "Scanned";
                await _db.SaveChangesAsync();
            }
            catch
            {
                document.ProcessingStatus = "Unscanned";
                await _db.SaveChangesAsync();
                // Swallow the exception on purpose: a failed analysis must never
                // take down document creation/upload. Add logging here if desired.
            }
        }

        // ---------------------------------------------------------------
        // 1. Text extraction
        // ---------------------------------------------------------------
        // PDFs go through PdfPig (reads the embedded text layer directly - fast
        // and exact). Images go through Tesseract OCR (pixel-based recognition -
        // preprocessed below to reduce misreads).
        private string? ExtractText(MedDocument document)
        {
            var safeFileName = Path.GetFileName(document.FileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", safeFileName);

            if (!File.Exists(filePath))
            {
                return null;
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (extension == ".pdf")
            {
                return ExtractTextFromPdf(filePath);
            }

            if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
            {
                return ExtractTextFromImage(filePath);
            }

            return null;
        }

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
            return IsUsableExtractedText(text) ? text : null;
        }

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
                using var original = Pix.LoadFromFile(filePath);
                using var preprocessed = PreprocessForOcr(original);

                using var engine = new TesseractEngine(_tessDataPath, _ocrLanguage, EngineMode.Default);

                // Lab/medical reports are dense text blocks (often tabular), not
                // mixed-layout pages - a single-block assumption outperforms
                // Tesseract's "fully automatic" default page segmentation here.
                engine.DefaultPageSegMode = PageSegMode.SingleBlock;

                // Restrict recognition to characters that actually appear in medical
                // reports (letters, digits, and value/unit punctuation). This cuts
                // down on misreads bleeding in from stray marks, logos, or noise.
                engine.SetVariable(
                    "tessedit_char_whitelist",
                    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,:/%-() ");

                using var page = engine.Process(preprocessed);
                var text = page.GetText();

                if (!IsUsableExtractedText(text))
                {
                    return null;
                }

                var meanConfidence = page.GetMeanConfidence();
                if (meanConfidence < MinAcceptableConfidence)
                {
                    // Still return the text (missing data is worse than possibly-
                    // imperfect data for most fields), but flag it - low-confidence
                    // OCR on medical values should be treated as needing review.
                    _logger.LogWarning(
                        "OCR mean confidence {Confidence:P0} is below the {Threshold:P0} threshold for file {FilePath}; " +
                        "extracted measurements may be inaccurate and should be reviewed.",
                        meanConfidence, MinAcceptableConfidence, filePath);
                }

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR failed for file {FilePath}.", filePath);
                return null;
            }
        }

        // Applies simple, low-risk preprocessing before OCR to improve accuracy on
        // scanned/photographed medical reports: upscale if low-resolution, convert
        // to grayscale to remove color noise (letterheads/stamps), and deskew to
        // correct tilt from photos or crooked scans.
        private static Pix PreprocessForOcr(Pix source)
        {
            var working = source;
            var ownsWorking = false;

            if (working.Width < MinRecommendedWidthPx && working.Width > 0)
            {
                var scaleFactor = (float)MinRecommendedWidthPx / working.Width;
                var scaled = working.Scale(scaleFactor, scaleFactor);
                if (ownsWorking) working.Dispose();
                working = scaled;
                ownsWorking = true;
            }

            var gray = working.ConvertRGBToGray();
            if (ownsWorking) working.Dispose();
            working = gray;
            ownsWorking = true;

            var deskewed = working.Deskew();
            if (ownsWorking) working.Dispose();
            working = deskewed;

            return working;
        }

        // Shared "is this text actually usable" check for both the PDF and OCR
        // extraction paths, so they can't silently drift out of agreement.
        private static bool IsUsableExtractedText(string? text) =>
            !string.IsNullOrWhiteSpace(text) && text.Trim().Length >= MinExtractedTextLength;

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
            if (bpMatch.Success &&
                double.TryParse(bpMatch.Groups[1].Value, out var systolic) &&
                double.TryParse(bpMatch.Groups[2].Value, out var diastolic))
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