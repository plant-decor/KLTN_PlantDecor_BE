using Microsoft.Extensions.Configuration;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Text;
using System.Text.Json;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class LangflowService : ILangflowService
    {
        private readonly HttpClient _httpClient;
        private readonly string _webhookUrl;
        private readonly string _embeddingUrl;

        public LangflowService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _webhookUrl = config["Langflow:WebhookUrl"] ?? "";
            _embeddingUrl = config["Langflow:EmbeddingUrl"] ?? "";
        }

        public async Task<float[]?> GenerateEmbeddingAsync(string text)
        {
            if (string.IsNullOrEmpty(_embeddingUrl))
            {
                throw new InvalidOperationException("Langflow:EmbeddingUrl is not configured");
            }

            try
            {
                var payload = new { text };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_embeddingUrl, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<EmbeddingResponse>(responseContent);

                return result?.Embedding;
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error generating embedding: {ex.Message}");
                return null;
            }
        }

        public async Task<string> IngestDataAsync<T>(T entity, Guid entityId, string entityType) where T : class
        {
            if (string.IsNullOrEmpty(_webhookUrl))
            {
                throw new InvalidOperationException("Langflow:WebhookUrl is not configured");
            }

            var payload = new
            {
                entity_id = entityId.ToString(),
                entity_type = entityType,
                data = entity
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_webhookUrl, content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        private class EmbeddingResponse
        {
            public float[]? Embedding { get; set; }
        }
    }
}
