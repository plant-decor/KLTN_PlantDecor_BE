using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Entities
{
    public class DesignTemplateTier
    {
        public int Id { get; set; }
        public int DesignTemplateId { get; set; }
        public string TierName { get; set; } = null!;
        public decimal MinArea { get; set; }
        public decimal MaxArea { get; set; }
        public decimal PackagePrice { get; set; }
        public string ScopedOfWork { get; set; } = null!;
        public int EstimatedDays { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public virtual DesignTemplate DesignTemplate { get; set; } = null!;
        public virtual ICollection<DesignRegistration> DesignRegistrations { get; set; } = new List<DesignRegistration>();
        public virtual ICollection<DesignTemplateTierItem> DesignTemplateTierItems { get; set; } = new List<DesignTemplateTierItem>();
    }
}
