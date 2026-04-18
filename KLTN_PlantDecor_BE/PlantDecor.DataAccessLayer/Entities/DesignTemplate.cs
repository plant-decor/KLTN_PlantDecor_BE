using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Entities
{
    public class DesignTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int? Style { get; set; }
        public List<int>? RoomTypes { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public virtual ICollection<DesignTemplateTier> DesignTemplateTiers { get; set; } = new List<DesignTemplateTier>();
        public virtual ICollection<NurseryDesignTemplate> NurseryDesignTemplates { get; set; } = new List<NurseryDesignTemplate>();
        public virtual ICollection<DesignTemplateSpecialization> DesignTemplateSpecializations { get; set; } = new List<DesignTemplateSpecialization>();
    }
}
