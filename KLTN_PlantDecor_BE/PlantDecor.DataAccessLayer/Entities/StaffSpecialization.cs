using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Entities
{
    public class StaffSpecialization
    {
        public int StaffId { get; set; }
        public int SpecializationId { get; set; }
        public virtual User Staff { get; set; } = null!;
        public virtual Specialization Specialization { get; set; } = null!;
    }
}
