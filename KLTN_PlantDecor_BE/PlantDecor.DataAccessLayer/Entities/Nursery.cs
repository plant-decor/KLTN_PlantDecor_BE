using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class Nursery
{
    public int Id { get; set; }

    public int? ManagerId { get; set; }

    public string? Name { get; set; }

    public string? Address { get; set; }

    public decimal? Area { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? Phone { get; set; }

    public int? Type { get; set; }

    public int? LightCondition { get; set; }

    public int? HumidityLevel { get; set; }

    public bool? HasMistSystem { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? Manager { get; set; }

    public virtual ICollection<PlantInstance> PlantInstances { get; set; } = new List<PlantInstance>();

    public virtual ICollection<CommonPlant> CommonPlants { get; set; } = new List<CommonPlant>();

    public virtual ICollection<NurseryPlantCombo> NurseryPlantCombos { get; set; } = new List<NurseryPlantCombo>();

    public virtual ICollection<NurseryCareService> NurseryCareServices { get; set; } = new List<NurseryCareService>();

    public virtual ICollection<NurseryMaterial> NurseryMaterials { get; set; } = new List<NurseryMaterial>();

    public virtual ICollection<ServiceRegistration> ServiceRegistrations { get; set; } = new List<ServiceRegistration>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
