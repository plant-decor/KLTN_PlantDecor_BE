using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Entities
{
    public class DesignRegistration
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? OrderId { get; set; }
        public int NurseryId { get; set; }
        public int DesignTemplateTierId { get; set; }
        public int? AssignedCaretakerId { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? Width { get; set; }
        public decimal? Length { get; set; }
        public string? CurrentStateImageUrl { get; set; }
        public string Address { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? CustomerNote { get; set; }
        public string? CancelReason { get; set; }
        public int Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public virtual DesignTemplateTier DesignTemplateTier { get; set; } = null!;
        public virtual Nursery Nursery { get; set; } = null!;
        public virtual Order? Order { get; set; }
        public virtual User User { get; set; } = null!;
        public virtual ICollection<DesignTask> DesignTasks { get; set; } = new List<DesignTask>();
        public virtual User? AssignedCaretaker { get; set; }
    }
}
