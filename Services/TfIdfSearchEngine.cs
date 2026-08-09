using MedVaultAPI.Model;
using System.Text.RegularExpressions;

namespace MedVaultAPI.Services.Search
{
    public class RankedDocumentResult
    {
        public MedDocument Document { get; set; } = null!;
        public double Score { get; set; }
    }

    public class TfIdfSearchEngine
    {
        public List<RankedDocumentResult> RankDocuments(string searchQuery, List<MedDocument> documents)
        {
            var results = new List<RankedDocumentResult>();

            if (string.IsNullOrWhiteSpace(searchQuery) || !documents.Any())
            {
                return documents.Select(d => new RankedDocumentResult { Document = d, Score = 1.0 }).ToList();
            }

            // 1. Tokenize search query and documents
            var queryTerms = Tokenize(searchQuery);
            if (!queryTerms.Any())
            {
                return documents.Select(d => new RankedDocumentResult { Document = d, Score = 1.0 }).ToList();
            }

            var docTokenMap = documents.ToDictionary(
                d => d.Id,
                d => Tokenize($"{d.Name} {d.FileName} {d.ExtractedText ?? ""}")
            );

            int totalDocs = documents.Count;

            // 2. Compute Inverse Document Frequency (IDF) for Query Terms
            var idfMap = new Dictionary<string, double>();
            foreach (var term in queryTerms.Distinct())
            {
                int docsWithTerm = docTokenMap.Values.Count(tokens => tokens.Contains(term));
                // IDF formula with smooth smoothing to prevent divide-by-zero
                idfMap[term] = Math.Log((double)(totalDocs + 1) / (docsWithTerm + 1)) + 1.0;
            }

            // 3. Build Query TF-IDF Vector
            var queryVector = new Dictionary<string, double>();
            foreach (var term in queryTerms.Distinct())
            {
                double tf = (double)queryTerms.Count(t => t == term) / queryTerms.Count;
                queryVector[term] = tf * idfMap[term];
            }

            // 4. Calculate Cosine Similarity for each document
            foreach (var doc in documents)
            {
                var docTokens = docTokenMap[doc.Id];
                if (!docTokens.Any())
                {
                    results.Add(new RankedDocumentResult { Document = doc, Score = 0.0 });
                    continue;
                }

                var docVector = new Dictionary<string, double>();
                foreach (var term in queryTerms.Distinct())
                {
                    double tf = (double)docTokens.Count(t => t == term) / docTokens.Count;
                    docVector[term] = tf * idfMap[term];
                }

                double similarityScore = CalculateCosineSimilarity(queryVector, docVector);
                results.Add(new RankedDocumentResult { Document = doc, Score = similarityScore });
            }

            return results.OrderByDescending(r => r.Score).ToList();
        }

        private List<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();

            // Normalize text: lowercase and remove non-alphanumeric characters
            string cleanText = Regex.Replace(text.ToLowerInvariant(), @"[^a-z0-9\s]", " ");

            // Split into tokens, ignoring single characters and empty entries
            return cleanText
                .Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 1)
                .ToList();
        }

        private double CalculateCosineSimilarity(Dictionary<string, double> vecA, Dictionary<string, double> vecB)
        {
            double dotProduct = 0.0;
            double magnitudeA = 0.0;
            double magnitudeB = 0.0;

            foreach (var key in vecA.Keys)
            {
                double valA = vecA[key];
                double valB = vecB.GetValueOrDefault(key, 0.0);

                dotProduct += valA * valB;
                magnitudeA += valA * valA;
                magnitudeB += valB * valB;
            }

            if (magnitudeA == 0.0 || magnitudeB == 0.0)
            {
                return 0.0;
            }

            return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        }
    }
}