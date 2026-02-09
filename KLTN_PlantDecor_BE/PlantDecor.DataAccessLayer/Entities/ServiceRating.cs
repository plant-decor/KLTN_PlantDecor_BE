using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class ServiceRating
{
    public int Id { get; set; }

    public int? ServiceRegistrationId { get; set; }

    public decimal? Rating { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ServiceRegistration? ServiceRegistration { get; set; }
}
