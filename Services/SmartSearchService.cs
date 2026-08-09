using MedVaultAPI.Data;
using MedVaultAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace MedVaultAPI.Services.Search
{
    // Result container models
    public class SmartSearchResult
    {
        public List<DocumentSearchResult> Documents { get; set; } = new();
        public List<MedicalMeasurement> Measurements { get; set; } = new();
    }

    public class DocumentSearchResult
    {
        public MedDocument Document { get; set; } = null!;
        public string FolderName { get; set; } = string.Empty;
        public double RelevanceScore { get; set; }
    }

    public class SmartSearchService
    {
        private readonly MedVaultDbContext _db;
        private readonly QueryParser _queryParser;
        private readonly TfIdfSearchEngine _tfidfEngine;

        public SmartSearchService(
            MedVaultDbContext db,
            QueryParser queryParser,
            TfIdfSearchEngine tfidfEngine)
        {
            _db = db;
            _queryParser = queryParser;
            _tfidfEngine = tfidfEngine;
        }

        public async Task<SmartSearchResult> SearchAsync(string userId, string rawQuery)
        {
            var result = new SmartSearchResult();

            // 1. Parse natural language query into structured intent
            var parsedQuery = _queryParser.Parse(rawQuery);

            // 2. PATH A: Structured Measurement Search (e.g., "creatinine results from last 6 months")
            if (!string.IsNullOrEmpty(parsedQuery.CanonicalMeasurement))
            {
                var measurementQuery = _db.MedicalMeasurements
                    .Include(m => m.Document)
                    .Where(m => m.UserId == userId &&
                                m.MeasurementType == parsedQuery.CanonicalMeasurement);

                if (parsedQuery.StartDate.HasValue)
                    measurementQuery = measurementQuery.Where(m => m.MeasuredDate >= parsedQuery.StartDate.Value);

                if (parsedQuery.EndDate.HasValue)
                    measurementQuery = measurementQuery.Where(m => m.MeasuredDate <= parsedQuery.EndDate.Value);

                result.Measurements = await measurementQuery
                    .OrderByDescending(m => m.MeasuredDate)
                    .ToListAsync();
            }

            // 3. PATH B: Document & Topic Search across ALL user folders
            var docQuery = _db.MedDocument
                .Where(d => d.UserId == userId && d.ProcessingStatus == "Processed");

            if (parsedQuery.StartDate.HasValue)
                docQuery = docQuery.Where(d => d.ReportDate >= parsedQuery.StartDate.Value);

            if (parsedQuery.EndDate.HasValue)
                docQuery = docQuery.Where(d => d.ReportDate <= parsedQuery.EndDate.Value);

            if (!string.IsNullOrEmpty(parsedQuery.ReportType))
                docQuery = docQuery.Where(d => d.ReportType == parsedQuery.ReportType);

            var candidateDocuments = await docQuery.ToListAsync();

            // Map folder names for cross-folder result UI display
            var folderIds = candidateDocuments.Select(d => d.FolderId).Distinct().ToList();
            var folderMap = await _db.Folder
                .Where(f => folderIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, f => f.Name);

            // 4. Semantic Ranking using TF-IDF & Cosine Similarity
            if (!string.IsNullOrWhiteSpace(parsedQuery.RemainingSearchTerms) && candidateDocuments.Any())
            {
                var rankedDocs = _tfidfEngine.RankDocuments(parsedQuery.RemainingSearchTerms, candidateDocuments);

                result.Documents = rankedDocs
                    .Where(r => r.Score >= 0.1) // Filter low relevance matches
                    .Select(r => new DocumentSearchResult
                    {
                        Document = r.Document,
                        FolderName = folderMap.GetValueOrDefault(r.Document.FolderId, "Unassigned"),
                        RelevanceScore = r.Score
                    })
                    .ToList();
            }
            else
            {
                result.Documents = candidateDocuments
                    .Select(d => new DocumentSearchResult
                    {
                        Document = d,
                        FolderName = folderMap.GetValueOrDefault(d.FolderId, "Unassigned"),
                        RelevanceScore = 1.0
                    })
                    .ToList();
            }

            return result;
        }
    }
}