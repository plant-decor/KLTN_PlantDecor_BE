using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Text.Json;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class AzureOpenAIService : IAzureOpenAIService
    {
        private readonly AzureOpenAIClient _client;
        private readonly string _embeddingDeploymentName;
        private readonly string _chatDeploymentName;
        private readonly string _visionDeploymentName;
        private readonly ILogger<AzureOpenAIService> _logger;

        public AzureOpenAIService(IConfiguration config, ILogger<AzureOpenAIService> logger)
        {
            _logger = logger;

            var endpoint = config["AzureOpenAI:Endpoint"]
                ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured");
            var apiKey = config["AzureOpenAI:ApiKey"]
                ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured");

            _embeddingDeploymentName = config["AzureOpenAI:EmbeddingDeployment"] ?? "text-embedding-3-small";
            _chatDeploymentName = config["AzureOpenAI:ChatDeployment"] ?? "gpt-4.1";
            _visionDeploymentName = config["AzureOpenAI:VisionDeployment"] ?? "gpt-4.1";

            _client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        }

        public async Task<float[]?> GenerateEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Empty text provided for embedding generation");
                return null;
            }

            try
            {
                var embeddingClient = _client.GetEmbeddingClient(_embeddingDeploymentName);
                var response = await embeddingClient.GenerateEmbeddingAsync(text);

                if (response?.Value != null)
                {
                    var embedding = response.Value.ToFloats().ToArray();
                    _logger.LogInformation($"Generated embedding with {embedding.Length} dimensions");
                    return embedding;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding");
                throw;
            }
        }

        public async Task<string?> GenerateChatCompletionAsync(string systemPrompt, string userMessage, float temperature = 0.7f)
        {
            try
            {
                var chatClient = _client.GetChatClient(_chatDeploymentName);

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userMessage)
                };

                var options = new ChatCompletionOptions
                {
                    Temperature = temperature,
                    MaxOutputTokenCount = 2000
                };

                var response = await chatClient.CompleteChatAsync(messages, options);

                if (response?.Value?.Content?.Count > 0)
                {
                    var result = response.Value.Content[0].Text;
                    _logger.LogInformation("Generated chat completion successfully");
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating chat completion");
                throw;
            }
        }

        public async Task<string?> GenerateJsonResponseAsync(string systemPrompt, string userMessage)
        {
            try
            {
                var chatClient = _client.GetChatClient(_chatDeploymentName);

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt + "\n\nYou must respond in valid JSON format only."),
                    new UserChatMessage(userMessage)
                };

                var options = new ChatCompletionOptions
                {
                    Temperature = 0.3f,
                    MaxOutputTokenCount = 2000,
                    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                };

                var response = await chatClient.CompleteChatAsync(messages, options);

                if (response?.Value?.Content?.Count > 0)
                {
                    var result = response.Value.Content[0].Text;

                    // Validate JSON
                    JsonDocument.Parse(result);

                    _logger.LogInformation("Generated JSON response successfully");
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JSON response");
                throw;
            }
        }

        public async Task<string?> AnalyzeImageAsync(string imageBase64, string prompt)
        {
            if (string.IsNullOrWhiteSpace(imageBase64))
            {
                _logger.LogWarning("Empty image provided for analysis");
                return null;
            }

            try
            {
                var chatClient = _client.GetChatClient(_visionDeploymentName);

                var messages = new List<ChatMessage>
                {
                    new UserChatMessage(
                        ChatMessageContentPart.CreateTextPart(prompt),
                        ChatMessageContentPart.CreateImagePart(
                            BinaryData.FromBytes(Convert.FromBase64String(imageBase64)),
                            "image/jpeg"
                        )
                    )
                };

                var options = new ChatCompletionOptions
                {
                    Temperature = 0.5f,
                    MaxOutputTokenCount = 1500
                };

                var response = await chatClient.CompleteChatAsync(messages, options);

                if (response?.Value?.Content?.Count > 0)
                {
                    var result = response.Value.Content[0].Text;
                    _logger.LogInformation("Analyzed image successfully");
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing image");
                throw;
            }
        }
    }
}
