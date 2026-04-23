using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using PlantDecor.BusinessLogicLayer.Exceptions;
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

        public async Task<List<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts)
        {
            var inputs = texts?
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .ToList() ?? new List<string>();

            if (inputs.Count == 0)
            {
                _logger.LogWarning("No valid texts provided for batch embedding generation");
                return new List<float[]>();
            }

            try
            {
                var embeddingClient = _client.GetEmbeddingClient(_embeddingDeploymentName);
                // Mỗi batch = 32 phần tử
                const int maxBatchSize = 32;
                const int maxRetryAttempts = 3;

                var allEmbeddings = new List<float[]>(inputs.Count);

                // start += 32 → nhảy từng đoạn 32 phần tử
                for (var start = 0; start < inputs.Count; start += maxBatchSize)
                {
                    var batch = inputs.Skip(start).Take(maxBatchSize).ToList();

                    var attempt = 0;
                    while (true)
                    {
                        try
                        {
                            var response = await embeddingClient.GenerateEmbeddingsAsync(batch);
                            if (response?.Value == null)
                            {
                                throw new InvalidOperationException("Embedding API returned null response for batch");
                            }

                            var batchEmbeddings = new List<float[]>();
                            foreach (var embedding in response.Value)
                            {
                                batchEmbeddings.Add(embedding.ToFloats().ToArray());
                            }

                            if (batchEmbeddings.Count != batch.Count)
                            {
                                throw new InvalidOperationException(
                                    $"Embedding count mismatch: expected {batch.Count}, got {batchEmbeddings.Count}");
                            }

                            allEmbeddings.AddRange(batchEmbeddings);
                            break;
                        }
                        catch (Exception ex) when (IsRateLimitException(ex) && attempt < maxRetryAttempts)
                        {
                            attempt++;
                            var delaySeconds = (int)Math.Pow(2, attempt);
                            _logger.LogWarning(
                                ex,
                                "Rate limited while generating embeddings batch. Attempt {Attempt}/{MaxAttempts}, waiting {DelaySeconds}s",
                                attempt,
                                maxRetryAttempts,
                                delaySeconds);
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                        }
                    }
                }

                _logger.LogInformation("Generated {Count} embeddings in batch mode", allEmbeddings.Count);
                return allEmbeddings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embeddings in batch mode");
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
                throw new BadRequestException("Failed to analyze image. Please ensure the image is valid and try again.");
            }
        }

        public async Task<string?> AnalyzeImagesAsync(IReadOnlyCollection<string> imageBase64List, string prompt)
        {
            if (imageBase64List == null || imageBase64List.Count == 0)
            {
                _logger.LogWarning("No images provided for multi-image analysis");
                return null;
            }

            var normalizedImages = imageBase64List
                .Where(image => !string.IsNullOrWhiteSpace(image))
                .ToList();

            if (normalizedImages.Count == 0)
            {
                _logger.LogWarning("Only empty images were provided for multi-image analysis");
                return null;
            }

            try
            {
                var chatClient = _client.GetChatClient(_visionDeploymentName);

                var contentParts = new List<ChatMessageContentPart>
                {
                    ChatMessageContentPart.CreateTextPart(prompt)
                };

                foreach (var imageBase64 in normalizedImages)
                {
                    contentParts.Add(
                        ChatMessageContentPart.CreateImagePart(
                            BinaryData.FromBytes(Convert.FromBase64String(imageBase64)),
                            "image/jpeg"));
                }

                var messages = new List<ChatMessage>
                {
                    new UserChatMessage(contentParts.ToArray())
                };

                var options = new ChatCompletionOptions
                {
                    Temperature = 0.5f,
                    MaxOutputTokenCount = 2000
                };

                var response = await chatClient.CompleteChatAsync(messages, options);

                if (response?.Value?.Content?.Count > 0)
                {
                    var result = response.Value.Content[0].Text;
                    _logger.LogInformation("Analyzed {ImageCount} images successfully in one request", normalizedImages.Count);
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing multiple images");
                throw new BadRequestException("Failed to analyze images. Please ensure the images are valid and try again.");
            }
        }

        private static bool IsRateLimitException(Exception ex)
        {
            var current = ex;
            while (current != null)
            {
                var message = current.Message;
                if (!string.IsNullOrWhiteSpace(message) &&
                    (message.Contains("429", StringComparison.OrdinalIgnoreCase) ||
                     message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                     message.Contains("too many requests", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                current = current.InnerException;
            }

            return false;
        }
    }
}
