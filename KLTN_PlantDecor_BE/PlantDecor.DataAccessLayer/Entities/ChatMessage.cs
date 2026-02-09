using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class ChatMessage
{
    public int Id { get; set; }

    public int? ChatSessionId { get; set; }

    public int? Sender { get; set; }

    public string? Content { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ChatSession? ChatSession { get; set; }
}
