using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class CareServicePackage
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Features { get; set; }

    public int? VisitPerWeek { get; set; }

    public int? DurationDays { get; set; }

    public int? ServiceType { get; set; }

    public int? AreaLimit { get; set; }

    public decimal? UnitPrice { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<NurseryCareService> NurseryCareServices { get; set; } = new List<NurseryCareService>();
}
