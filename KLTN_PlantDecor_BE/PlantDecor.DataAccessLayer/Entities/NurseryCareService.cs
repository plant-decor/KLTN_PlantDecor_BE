using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class NurseryCareService
{
    public int Id { get; set; }

    public int CareServicePackageId { get; set; }

    public int NurseryId { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual CareServicePackage CareServicePackage { get; set; } = null!;

    public virtual Nursery Nursery { get; set; } = null!;

    public virtual ICollection<ServiceRegistration> ServiceRegistrations { get; set; } = new List<ServiceRegistration>();
}
