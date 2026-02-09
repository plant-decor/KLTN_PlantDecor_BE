using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class RefreshToken
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Token { get; set; } = null!;

    public bool? IsRevoked { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    public virtual User? User { get; set; }
}
