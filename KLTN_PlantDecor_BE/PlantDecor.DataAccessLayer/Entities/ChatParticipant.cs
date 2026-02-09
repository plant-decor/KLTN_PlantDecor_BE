using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class ChatParticipant
{
    public int Id { get; set; }

    public int? ChatSessionId { get; set; }

    public int? UserId { get; set; }

    public DateTime? JoinedAt { get; set; }

    public virtual ChatSession? ChatSession { get; set; }

    public virtual User? User { get; set; }
}
