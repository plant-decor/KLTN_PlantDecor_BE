using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Entities
{
    public class DesignTemplateSpecialization
    {
        public int DesignTemplateId { get; set; }
        public int SpecializationId { get; set; }
        public virtual DesignTemplate DesignTemplate { get; set; } = null!;
        public virtual Specialization Specialization { get; set; } = null!;
    }
}
