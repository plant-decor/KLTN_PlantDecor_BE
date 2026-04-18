using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Entities
{
    public class DesignTemplateTierItem
    {
        public int Id { get; set; }
        public int DesignTemplateTierId { get; set; }
        public int? MaterialId { get; set; }
        public int? PlantId { get; set; }
        public int ItemType { get; set; }
        public decimal Quantity { get; set; }
        public DateTime? CreatedAt { get; set; }
        public virtual DesignTemplateTier DesignTemplateTier { get; set; } = null!;
    }
}
