using Pgvector;

namespace PlantDecor.DataAccessLayer.Entities
{
    public class Embedding
    {
        public Guid Id { get; set; }
        public string EntityType { get; set; } = null!;
        public Guid EntityId { get; set; }
        public string Content { get; set; } = null!;
        public Vector? EmbeddingVector { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
