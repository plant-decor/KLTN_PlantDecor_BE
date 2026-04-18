using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Entities
{
    public class DesignTask
    {
        public int Id { get; set; }
        public int DesignRegistrationId { get; set; }
        public int? AssignedStaffId { get; set; }
        public DateOnly? ScheduledDate { get; set; }
        public int TaskType { get; set; }
        public string? ReportImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int Status { get; set; }
        public virtual DesignRegistration DesignRegistration { get; set; } = null!;
        public virtual User? AssignedStaff { get; set; }
        public virtual ICollection<TaskMaterialUsage> TaskMaterialUsages { get; set; } = new List<TaskMaterialUsage>();
    }
}
