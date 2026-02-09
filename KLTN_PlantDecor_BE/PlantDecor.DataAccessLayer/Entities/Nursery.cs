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
}
