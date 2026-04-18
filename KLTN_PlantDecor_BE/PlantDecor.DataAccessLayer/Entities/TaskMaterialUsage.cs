using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Entities
{
    public class TaskMaterialUsage
    {
        public int Id { get; set; }
        public int DesignTaskId { get; set; }
        public int MaterialId { get; set; }
        public decimal? ActualQuantity { get; set; }
        public string? Note { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DesignTask DesignTask { get; set; } = null!;
        public Material Material { get; set; } = null!;
    }
}
