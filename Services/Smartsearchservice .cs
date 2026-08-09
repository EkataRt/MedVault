using System.Text.RegularExpressions;
using MedVaultAPI.Data;
using MedVaultAPI.Model;
using MedVaultAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace MedVaultAPI.Services
{
    /// <summary>
    /// V1 Smart Search service. No interface by design.
    ///
    /// Pipeline: parse query into simple filters (topic / measurement / report type /
    /// year / date range) -> load the user's documents (+ topics + measurements) ->
    /// score each document with simple rule-based scoring -> sort -> return.
    ///
    /// This is deliberately not NLP/TF-IDF. The "ParsedQuery" step and the scoring
    /// step are separated so a future version can swap in TF-IDF + cosine similarity
    /// against ExtractedText without touching the controller or the parsing logic.
    /// </summary>
    public class SmartSearchService
    {
        private readonly MedVaultDbContext _db;

        public SmartSearchService(MedVaultDbContext db)
        {
            _db = db;
        }

        private class ParsedQuery
        {
            public string RawQuery = string.Empty;
            public string? Topic;
            public string? Measurement;
            public string? ReportType;
            public int? Year;
            public DateTime? StartDate;
            public DateTime? EndDate;
        }

        public async Task<object> SearchAsync(string userId, string query)
        {
            var parsed = ParseQuery(query);

            // Pull the user's documents plus their related topics/measurements.
            // Deliberately NOT scoped to a folder - Smart Search spans the whole account.
            var documents = await _db.MedDocument
                .Where(d => d.UserId == userId)
                .ToListAsync();

            if (documents.Count == 0)
            {
                return new List<SmartSearchResult>();
            }

            var documentIds = documents.Select(d => d.Id).ToList();

            var topicsByDocument = (await _db.MedicalTopic
                    .Where(t => t.UserId == userId && documentIds.Contains(t.DocumentId))
                    .ToListAsync())
                .GroupBy(t => t.DocumentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var measurementsByDocument = (await _db.MedicalMeasurement
                    .Where(m => m.UserId == userId && documentIds.Contains(m.DocumentId))
                    .ToListAsync())
                .GroupBy(m => m.DocumentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var folderNamesById = await _db.Folder
                .Where(f => f.UserId == userId)
                .ToDictionaryAsync(f => f.Id, f => f.Name);

            var results = new List<SmartSearchResult>();

            foreach (var document in documents)
            {
                topicsByDocument.TryGetValue(document.Id, out var docTopics);
                measurementsByDocument.TryGetValue(document.Id, out var docMeasurements);
                docTopics ??= new List<MedicalTopic>();
                docMeasurements ??= new List<MedicalMeasurement>();

                var score = ScoreDocument(document, docTopics, docMeasurements, parsed);

                if (score <= 0)
                {
                    continue;
                }

                folderNamesById.TryGetValue(document.FolderId, out var folderName);

                results.Add(new SmartSearchResult
                {
                    DocumentId = document.Id,
                    DocumentName = document.Name,
                    FileName = document.FileName,
                    FolderId = document.FolderId,
                    FolderName = folderName,
                    ReportType = document.ReportType,
                    ReportDate = document.ReportDate,
                    Topics = docTopics.Select(t => t.Topic).Distinct().ToList(),
                    Measurements = docMeasurements,
                    Score = score
                });
            }

            var ordered = results
                .OrderByDescending(r => r.Score)
                .ThenByDescending(r => r.ReportDate ?? DateTime.MinValue)
                .ToList();

            return ordered;
        }

        // ---------------------------------------------------------------
        // Scoring (section 24 of the spec)
        // ---------------------------------------------------------------

        private double ScoreDocument(
            MedDocument document,
            List<MedicalTopic> topics,
            List<MedicalMeasurement> measurements,
            ParsedQuery parsed)
        {
            // Hard date filter: if the query specified a year or a date range,
            // documents without a matching ReportDate are excluded entirely.
            if (parsed.Year.HasValue)
            {
                if (!document.ReportDate.HasValue || document.ReportDate.Value.Year != parsed.Year.Value)
                {
                    return 0;
                }
            }

            if (parsed.StartDate.HasValue && parsed.EndDate.HasValue)
            {
                if (!document.ReportDate.HasValue
                    || document.ReportDate.Value.Date < parsed.StartDate.Value.Date
                    || document.ReportDate.Value.Date > parsed.EndDate.Value.Date)
                {
                    return 0;
                }
            }

            double score = 0;

            if (parsed.Topic != null && topics.Any(t => string.Equals(t.Topic, parsed.Topic, StringComparison.OrdinalIgnoreCase)))
            {
                score += 5;
            }

            if (parsed.Measurement != null && measurements.Any(m => string.Equals(m.MeasurementType, parsed.Measurement, StringComparison.OrdinalIgnoreCase)))
            {
                score += 5;
            }

            if (parsed.ReportType != null && string.Equals(document.ReportType, parsed.ReportType, StringComparison.OrdinalIgnoreCase))
            {
                score += 4;
            }

            if (parsed.Year.HasValue || (parsed.StartDate.HasValue && parsed.EndDate.HasValue))
            {
                // We already hard-filtered above, so reaching here means it matched.
                score += 3;
            }

            var normalizedText = document.ExtractedText?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(normalizedText))
            {
                var keywordsToCheck = new List<string>();
                if (parsed.Topic != null) keywordsToCheck.AddRange(ReportAnalysisService.TopicKeywords[parsed.Topic]);
                if (parsed.Measurement != null) keywordsToCheck.Add(parsed.Measurement.ToLowerInvariant());
                if (parsed.ReportType != null)
                {
                    var reportKeywords = ReportAnalysisService.ReportTypeKeywords
                        .FirstOrDefault(rt => rt.Type == parsed.ReportType).Keywords;
                    if (reportKeywords != null) keywordsToCheck.AddRange(reportKeywords);
                }
                if (keywordsToCheck.Count == 0) keywordsToCheck.Add(parsed.RawQuery.ToLowerInvariant());

                if (keywordsToCheck.Any(k => !string.IsNullOrWhiteSpace(k) && normalizedText.Contains(k)))
                {
                    score += 2;
                }
            }

            if (!string.IsNullOrEmpty(document.Name)
                && document.Name.Contains(parsed.RawQuery, StringComparison.OrdinalIgnoreCase))
            {
                score += 1;
            }

            return score;
        }

        // ---------------------------------------------------------------
        // Query parsing (sections 21-22 of the spec)
        // ---------------------------------------------------------------

        private static readonly Regex YearPattern = new(@"\b(19|20)\d{2}\b");
        private static readonly Regex LastNMonthsPattern = new(@"last\s+(\d+)\s+month", RegexOptions.IgnoreCase);
        private static readonly Regex LastNYearsPattern = new(@"last\s+(\d+)\s+year", RegexOptions.IgnoreCase);
        private static readonly Regex LastMonthPattern = new(@"last\s+month\b", RegexOptions.IgnoreCase);
        private static readonly Regex LastYearPattern = new(@"last\s+year\b", RegexOptions.IgnoreCase);
        private static readonly Regex BetweenMonthsPattern = new(@"between\s+([a-zA-Z]+)\s+and\s+([a-zA-Z]+)", RegexOptions.IgnoreCase);

        private static readonly Dictionary<string, int> MonthNumbersByName = new(StringComparer.OrdinalIgnoreCase)
        {
            ["january"] = 1,
            ["february"] = 2,
            ["march"] = 3,
            ["april"] = 4,
            ["may"] = 5,
            ["june"] = 6,
            ["july"] = 7,
            ["august"] = 8,
            ["september"] = 9,
            ["october"] = 10,
            ["november"] = 11,
            ["december"] = 12
        };

        private ParsedQuery ParseQuery(string query)
        {
            var lowered = (query ?? string.Empty).Trim().ToLowerInvariant();
            var result = new ParsedQuery { RawQuery = query ?? string.Empty };

            // Topic: check longest/most specific keywords first (e.g. "back pain" before "eye").
            foreach (var (topic, keywords) in ReportAnalysisService.TopicKeywords)
            {
                if (keywords.Any(keyword => lowered.Contains(keyword)))
                {
                    result.Topic = topic;
                    break;
                }
            }

            // Measurement (checked independently of topic - "creatinine" is both a
            // Kidney topic keyword and its own measurement).
            foreach (var measurementType in ReportAnalysisService.MeasurementTypeNames)
            {
                if (lowered.Contains(measurementType.ToLowerInvariant()))
                {
                    result.Measurement = measurementType;
                    break;
                }
            }

            // Report type
            foreach (var (type, keywords) in ReportAnalysisService.ReportTypeKeywords)
            {
                if (keywords.Any(keyword => lowered.Contains(keyword)))
                {
                    result.ReportType = type;
                    break;
                }
            }

            // Relative date ranges take priority over an absolute year if both somehow match.
            var now = DateTime.UtcNow.Date;

            var lastNMonthsMatch = LastNMonthsPattern.Match(lowered);
            var lastNYearsMatch = LastNYearsPattern.Match(lowered);
            var betweenMatch = BetweenMonthsPattern.Match(lowered);

            if (lastNMonthsMatch.Success && int.TryParse(lastNMonthsMatch.Groups[1].Value, out var months))
            {
                result.StartDate = now.AddMonths(-months);
                result.EndDate = now;
            }
            else if (lastNYearsMatch.Success && int.TryParse(lastNYearsMatch.Groups[1].Value, out var years))
            {
                result.StartDate = now.AddYears(-years);
                result.EndDate = now;
            }
            else if (LastMonthPattern.IsMatch(lowered))
            {
                result.StartDate = now.AddMonths(-1);
                result.EndDate = now;
            }
            else if (LastYearPattern.IsMatch(lowered))
            {
                result.StartDate = now.AddYears(-1);
                result.EndDate = now;
            }
            else if (betweenMatch.Success
                     && MonthNumbersByName.TryGetValue(betweenMatch.Groups[1].Value, out var startMonth)
                     && MonthNumbersByName.TryGetValue(betweenMatch.Groups[2].Value, out var endMonth))
            {
                // Month-only range: assumption documented in README - uses the current year.
                var year = now.Year;
                result.StartDate = new DateTime(year, startMonth, 1);
                result.EndDate = new DateTime(year, endMonth, DateTime.DaysInMonth(year, endMonth));
            }
            else
            {
                // Only look for a standalone year if no relative/between range was found.
                var yearMatch = YearPattern.Match(lowered);
                if (yearMatch.Success && int.TryParse(yearMatch.Value, out var year))
                {
                    result.Year = year;
                }
            }

            return result;
        }
    }
}