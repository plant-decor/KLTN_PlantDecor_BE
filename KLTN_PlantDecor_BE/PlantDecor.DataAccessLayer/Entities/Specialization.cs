using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Entities
{
    public class Specialization
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public bool IsActive { get; set; }

        public ICollection<CareServiceSpecialization> CareServiceSpecializations { get; set; } = new List<CareServiceSpecialization>();
        public ICollection<StaffSpecialization> StaffSpecializations { get; set; } = new List<StaffSpecialization>();
        public ICollection<DesignTemplateSpecialization> DesignTemplateSpecializations { get; set; } = new List<DesignTemplateSpecialization>();
    }
}
