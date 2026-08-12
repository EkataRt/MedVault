using System.Text.RegularExpressions;
using MedVaultAPI.Data;
using MedVaultAPI.Model;
using MedVaultAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace MedVaultAPI.Services
{

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

                var score = ScoreDocument(
                    document,
                    docTopics,
                    docMeasurements,
                    parsed);

                if (score <= 0)
                {
                    continue;
                }

                var resultMeasurements = docMeasurements;

                if (parsed.Measurement != null)
                {
                    resultMeasurements = docMeasurements
                        .Where(m => string.Equals(
                            m.MeasurementType,
                            parsed.Measurement,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                folderNamesById.TryGetValue(
                    document.FolderId,
                    out var folderName);

                results.Add(new SmartSearchResult
                {
                    DocumentId = document.Id,
                    DocumentName = document.Name,
                    FileName = document.FileName,
                    FolderId = document.FolderId,
                    FolderName = folderName,
                    ReportType = document.ReportType,
                    ReportDate = document.ReportDate,
                    UploadDate = document.CreatedAt,
                    Topics = docTopics
                        .Select(t => t.Topic)
                        .Distinct()
                        .ToList(),

                    Measurements = resultMeasurements,
                    Score = score
                });
            }

            var ordered = results
                .OrderByDescending(r => r.Score)
                .ThenByDescending(
                    r => r.ReportDate ?? DateTime.MinValue)
                .ToList();

            return ordered;
        }

        private double ScoreDocument(MedDocument document, List<MedicalTopic> topics, List<MedicalMeasurement> measurements, ParsedQuery parsed)
        {


            if (parsed.Year.HasValue)
            {
                if (!document.ReportDate.HasValue ||
                    document.ReportDate.Value.Year != parsed.Year.Value)
                {
                    return 0;
                }
            }

            if (parsed.StartDate.HasValue && parsed.EndDate.HasValue)
            {
                if (!document.ReportDate.HasValue ||
                    document.ReportDate.Value.Date < parsed.StartDate.Value.Date ||
                    document.ReportDate.Value.Date > parsed.EndDate.Value.Date)
                {
                    return 0;
                }
            }

            double score = 0;



            string? matchedMeasurement = parsed.Measurement;


            if (matchedMeasurement == null &&
                parsed.Topic == null &&
                !string.IsNullOrWhiteSpace(parsed.RawQuery))
            {
                var normalizedQuery = parsed.RawQuery.Trim().ToLowerInvariant();

                var possibleMeasurements = ReportAnalysisService.MeasurementTypeNames
                    .Where(m =>
                        m.Length >= 4 &&
                        normalizedQuery.Length >= 4 &&
                        m.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (possibleMeasurements.Count == 1)
                {
                    matchedMeasurement = possibleMeasurements[0];
                }
            }

            if (matchedMeasurement != null)
            {
                var measurementMatch = measurements.Any(m =>
                    string.Equals(
                        m.MeasurementType,
                        matchedMeasurement,
                        StringComparison.OrdinalIgnoreCase));


                if (!measurementMatch)
                {
                    return 0;
                }

                score += 5;
            }



            if (parsed.Topic != null)
            {
                var topicMatch = topics.Any(t =>
                    string.Equals(t.Topic, parsed.Topic, StringComparison.OrdinalIgnoreCase));

                if (topicMatch)
                    score += 5;
                else
                    return 0;   // disqualify, consistent with ReportType handling
            }



            if (parsed.ReportType != null)
            {
                if (string.Equals(
                    document.ReportType,
                    parsed.ReportType,
                    StringComparison.OrdinalIgnoreCase))
                {
                    score += 4;
                }
                else
                {
                    return 0;
                }
            }

            if (parsed.Year.HasValue ||
                (parsed.StartDate.HasValue && parsed.EndDate.HasValue))
            {
                score += 3;
            }


            var normalizedText = document.ExtractedText?.ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(normalizedText))
            {
                var keywordsToCheck = new List<string>();

                if (parsed.Topic != null &&
                    ReportAnalysisService.TopicKeywords.TryGetValue(
                        parsed.Topic,
                        out var topicKeywords))
                {
                    keywordsToCheck.AddRange(topicKeywords);
                }

                if (matchedMeasurement != null)
                {
                    keywordsToCheck.Add(matchedMeasurement.ToLowerInvariant());
                }

                if (parsed.ReportType != null)
                {
                    var reportType = ReportAnalysisService.ReportTypeKeywords
                        .FirstOrDefault(rt =>
                            string.Equals(
                                rt.Type,
                                parsed.ReportType,
                                StringComparison.OrdinalIgnoreCase));

                    if (reportType.Keywords != null)
                    {
                        keywordsToCheck.AddRange(reportType.Keywords);
                    }
                }

                if (keywordsToCheck.Any(k =>
                    !string.IsNullOrWhiteSpace(k) &&
                    normalizedText.Contains(k)))
                {
                    score += 2;
                }
            }



            if (!string.IsNullOrWhiteSpace(document.Name) &&
                !string.IsNullOrWhiteSpace(parsed.RawQuery) &&
                document.Name.Contains(
                    parsed.RawQuery,
                    StringComparison.OrdinalIgnoreCase))
            {
                score += 1;
            }

            return score;
        }


        private static readonly Regex YearPattern =
            new(@"\b(19|20)\d{2}\b");

        private static readonly Regex LastNMonthsPattern =
            new(@"last\s+(\d+)\s+month", RegexOptions.IgnoreCase);

        private static readonly Regex LastNYearsPattern =
            new(@"last\s+(\d+)\s+year", RegexOptions.IgnoreCase);

        private static readonly Regex LastMonthPattern =
            new(@"last\s+month\b", RegexOptions.IgnoreCase);

        private static readonly Regex LastYearPattern =
            new(@"last\s+year\b", RegexOptions.IgnoreCase);

        private static readonly Regex BetweenMonthsPattern =
            new(@"between\s+([a-zA-Z]+)\s+and\s+([a-zA-Z]+)", RegexOptions.IgnoreCase);

        private static readonly Dictionary<string, int> MonthNumbersByName =
            new(StringComparer.OrdinalIgnoreCase)
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

            var result = new ParsedQuery
            {
                RawQuery = query ?? string.Empty
            };



            foreach (var (topic, keywords) in ReportAnalysisService.TopicKeywords)
            {
                if (keywords
                    .OrderByDescending(k => k.Length)
                    .Any(keyword => lowered.Contains(keyword)))
                {
                    result.Topic = topic;
                    break;
                }
            }

            if (result.Topic == null)
            {
                foreach (var measurementType in ReportAnalysisService.MeasurementTypeNames
                             .OrderByDescending(m => m.Length))
                {
                    var normalizedMeasurement = measurementType.ToLowerInvariant();

                    if (lowered.Contains(normalizedMeasurement))
                    {
                        result.Measurement = measurementType;
                        break;
                    }
                }
            }



            if (result.Topic == null && result.Measurement == null && lowered.Length >= 4)
            {
                var possibleMeasurements = ReportAnalysisService.MeasurementTypeNames
                    .Where(measurement =>
                        measurement.Length >= 4 &&
                        measurement.StartsWith(
                            lowered,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();


                if (possibleMeasurements.Count == 1)
                {
                    result.Measurement = possibleMeasurements[0];
                }
            }


            foreach (var (type, keywords) in ReportAnalysisService.ReportTypeKeywords)
            {
                if (keywords
                    .OrderByDescending(k => k.Length)
                    .Any(keyword => lowered.Contains(keyword)))
                {
                    result.ReportType = type;
                    break;
                }
            }

            var now = DateTime.UtcNow.Date;

            var lastNMonthsMatch = LastNMonthsPattern.Match(lowered);
            var lastNYearsMatch = LastNYearsPattern.Match(lowered);
            var betweenMatch = BetweenMonthsPattern.Match(lowered);


            if (lastNMonthsMatch.Success &&
                int.TryParse(
                    lastNMonthsMatch.Groups[1].Value,
                    out var months) &&
                months > 0)
            {
                result.StartDate = now.AddMonths(-months);
                result.EndDate = now;
            }

            else if (lastNYearsMatch.Success &&
                     int.TryParse(
                         lastNYearsMatch.Groups[1].Value,
                         out var years) &&
                     years > 0)
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


            else if (betweenMatch.Success &&
                     MonthNumbersByName.TryGetValue(
                         betweenMatch.Groups[1].Value,
                         out var startMonth) &&
                     MonthNumbersByName.TryGetValue(
                         betweenMatch.Groups[2].Value,
                         out var endMonth))
            {
                var year = now.Year;


                if (startMonth <= endMonth)
                {
                    result.StartDate = new DateTime(year, startMonth, 1);

                    result.EndDate = new DateTime(
                        year,
                        endMonth,
                        DateTime.DaysInMonth(year, endMonth));
                }
                else
                {

                    result.StartDate = new DateTime(year, startMonth, 1);

                    result.EndDate = new DateTime(
                        year + 1,
                        endMonth,
                        DateTime.DaysInMonth(year + 1, endMonth));
                }
            }
            else
            {
                var yearMatch = YearPattern.Match(lowered);

                if (yearMatch.Success &&
                    int.TryParse(yearMatch.Value, out var year))
                {
                    result.Year = year;
                }
            }

            return result;
        }
    }
}