using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Entities
{
    public class PackagePlantSuitability
    {
        public int Id { get; set; }
        public int CareServicePackageId { get; set; }
        public int? CategoryId { get; set; }
        public int? CareDifficultyLevel { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public virtual CareServicePackage CareServicePackage { get; set; } = null!;
        public virtual Category? Category { get; set; }
    }
}
