using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class CareServicePackage
{
    public int Id { get; set; }

    public int? ParentServiceId { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Features { get; set; }

    public string? Frequency { get; set; }

    public int? DurationDays { get; set; }

    public int? ServiceType { get; set; }

    public int? DifficultyLevel { get; set; }

    public int? AreaLimit { get; set; }

    public decimal? UnitPrice { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<CareServicePackage> InverseParentService { get; set; } = new List<CareServicePackage>();

    public virtual CareServicePackage? ParentService { get; set; }

    public virtual ICollection<ServiceRegistration> ServiceRegistrations { get; set; } = new List<ServiceRegistration>();
}
