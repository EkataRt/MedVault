using System.Text.RegularExpressions;

namespace MedVaultAPI.Services.Search
{
    public class ParsedQuery
    {
        public string? CanonicalMeasurement { get; set; }
        public string? ReportType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string RemainingSearchTerms { get; set; } = string.Empty;
    }

    public class QueryParser
    {
        // Dictionary for mapping user terms to canonical measurement keys
        private readonly Dictionary<string, string> _measurementDictionary = new(StringComparer.OrdinalIgnoreCase)
        {
            { "creatinine", "CREATININE" },
            { "serum creatinine", "CREATININE" },
            { "s. creatinine", "CREATININE" },
            { "egfr", "EGFR" },
            { "gfr", "EGFR" },
            { "calcium", "CALCIUM" },
            { "blood pressure", "BLOOD_PRESSURE" },
            { "bp", "BLOOD_PRESSURE" },
            { "rbc", "RBC" },
            { "red blood cell", "RBC" }
        };

        // Supported report types
        private readonly HashSet<string> _reportTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "MRI", "X-Ray", "XRay", "Ultrasound", "Blood Test", "Urine Test", "CT Scan"
        };

        public ParsedQuery Parse(string rawQuery)
        {
            var result = new ParsedQuery();
            string query = rawQuery.Trim();

            // 1. EXTRACT DATES & STRIP FROM QUERY STRING
            query = ExtractDates(query, result);

            // 2. EXTRACT CANONICAL MEASUREMENT
            foreach (var kvp in _measurementDictionary)
            {
                if (Regex.IsMatch(query, $@"\b{Regex.Escape(kvp.Key)}\b", RegexOptions.IgnoreCase))
                {
                    result.CanonicalMeasurement = kvp.Value;
                    // Remove matched measurement word from query text
                    query = Regex.Replace(query, $@"\b{Regex.Escape(kvp.Key)}\b", "", RegexOptions.IgnoreCase);
                    break;
                }
            }

            // 3. EXTRACT REPORT TYPE
            foreach (var type in _reportTypes)
            {
                if (Regex.IsMatch(query, $@"\b{Regex.Escape(type)}\b", RegexOptions.IgnoreCase))
                {
                    result.ReportType = type;
                    query = Regex.Replace(query, $@"\b{Regex.Escape(type)}\b", "", RegexOptions.IgnoreCase);
                    break;
                }
            }

            // 4. CLEAN UP REMAINING TEXT FOR TF-IDF
            // Remove stop words (reports, test, results, from, show, my, etc.)
            string cleanTerms = Regex.Replace(query, @"\b(reports?|tests?|results?|history|from|between|in|show|my|the|and)\b", "", RegexOptions.IgnoreCase);
            result.RemainingSearchTerms = Regex.Replace(cleanTerms, @"\s+", " ").Trim();

            return result;
        }

        private string ExtractDates(string text, ParsedQuery parsed)
        {
            DateTime now = DateTime.UtcNow;

            // Pattern A: "last X months" / "last X years"
            var relativeMatch = Regex.Match(text, @"last\s+(\d+)\s+(month|months|year|years)", RegexOptions.IgnoreCase);
            if (relativeMatch.Success)
            {
                int count = int.Parse(relativeMatch.Groups[1].Value);
                string unit = relativeMatch.Groups[2].Value.ToLower();

                parsed.EndDate = now;
                parsed.StartDate = unit.StartsWith("year") ? now.AddYears(-count) : now.AddMonths(-count);

                return text.Replace(relativeMatch.Value, "", StringComparison.OrdinalIgnoreCase);
            }

            // Pattern B: "from 2025" or "in 2026"
            var yearMatch = Regex.Match(text, @"(?:from|in)?\s*\b(20\d{2})\b", RegexOptions.IgnoreCase);
            if (yearMatch.Success)
            {
                int year = int.Parse(yearMatch.Groups[1].Value);
                parsed.StartDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                parsed.EndDate = new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

                return text.Replace(yearMatch.Value, "", StringComparison.OrdinalIgnoreCase);
            }

            // Pattern C: "between [Month] and [Month]" (e.g. between March and June)
            var rangeMatch = Regex.Match(text, @"between\s+([a-zA-Z]+)\s+and\s+([a-zA-Z]+)", RegexOptions.IgnoreCase);
            if (rangeMatch.Success)
            {
                if (DateTime.TryParseExact(rangeMatch.Groups[1].Value, "MMMM", null, System.Globalization.DateTimeStyles.None, out var startM) &&
                    DateTime.TryParseExact(rangeMatch.Groups[2].Value, "MMMM", null, System.Globalization.DateTimeStyles.None, out var endM))
                {
                    parsed.StartDate = new DateTime(now.Year, startM.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    parsed.EndDate = new DateTime(now.Year, endM.Month, DateTime.DaysInMonth(now.Year, endM.Month), 23, 59, 59, DateTimeKind.Utc);
                }

                return text.Replace(rangeMatch.Value, "", StringComparison.OrdinalIgnoreCase);
            }

            return text;
        }
    }
}