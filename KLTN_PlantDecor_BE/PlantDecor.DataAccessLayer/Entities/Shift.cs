using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Entities
{
    public partial class Shift
    {
        public int Id { get; set; }
        public string ShiftName { get; set; } = null!;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public virtual ICollection<ServiceProgress> ServiceProgresses { get; set; } = new List<ServiceProgress>();
        public virtual ICollection<ServiceRegistration> ServiceRegistrations { get; set; } = new List<ServiceRegistration>();
    }
}
