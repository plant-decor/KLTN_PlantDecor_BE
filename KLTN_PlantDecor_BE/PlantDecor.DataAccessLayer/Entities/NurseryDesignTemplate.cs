using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Entities
{
    public class NurseryDesignTemplate
    {
        public int Id { get; set; }
        public int NurseryId { get; set; }
        public int DesignTemplateId { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public virtual DesignTemplate DesignTemplate { get; set; } = null!;
        public virtual Nursery Nursery { get; set; } = null!;
    }
}
