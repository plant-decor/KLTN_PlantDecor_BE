namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IAzureOpenAIService
    {
        /// <summary>
        /// Generate embeddings for text using Azure OpenAI Embeddings API
        /// </summary>
        /// <param name="text">Text to generate embeddings for</param>
        /// <returns>Float array of embeddings (1536 dimensions for text-embedding-ada-002)</returns>
        Task<float[]?> GenerateEmbeddingAsync(string text);

        /// <summary>
        /// Generate chat completion using Azure OpenAI Chat API
        /// </summary>
        /// <param name="systemPrompt">System prompt to set context</param>
        /// <param name="userMessage">User message/query</param>
        /// <param name="temperature">Creativity level (0.0-1.0)</param>
        /// <returns>AI response text</returns>
        Task<string?> GenerateChatCompletionAsync(string systemPrompt, string userMessage, float temperature = 0.7f);

        /// <summary>
        /// Analyze room image using Azure OpenAI Vision API (GPT-4 Vision)
        /// </summary>
        /// <param name="imageBase64">Base64 encoded image</param>
        /// <param name="prompt">Analysis prompt</param>
        /// <returns>AI analysis result</returns>
        Task<string?> AnalyzeImageAsync(string imageBase64, string prompt);

        /// <summary>
        /// Generate chat completion with JSON response mode
        /// </summary>
        /// <param name="systemPrompt">System prompt</param>
        /// <param name="userMessage">User message</param>
        /// <returns>JSON formatted response</returns>
        Task<string?> GenerateJsonResponseAsync(string systemPrompt, string userMessage);
    }
}
