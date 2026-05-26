using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities
{
    public partial class ConversationSummarySnapshot
    {
        public int Id { get; set; }
        public int? ConversationId { get; set; }
        public string? Summary { get; set; }
        public string? KeyPointsJson { get; set; }
        public string? NextActionsJson { get; set; }
        public string? StructuredFeaturesJson { get; set; } // For storing any additional structured data extracted from the conversation, e.g. detected intents, entities, sentiment scores, etc.
        public string? SourceWindow { get; set; } // e.g. "3D", "7D", "30D"
        public string? TranscriptHash { get; set; } // To detect if the conversation has changed since the summary was generated
        public DateTime? GeneratedAt { get; set; }
        public virtual ChatSession? ChatSession { get; set; }
    }
}
