using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Entities
{
    public partial class CustomerSurvey
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        
        public bool? HasPets { get; set; }
        
        public bool? HasChildren { get; set; }

        public decimal? MaxBudget { get; set; }

        public int ExperienceLevel  { get; set; }

        public int PreferredPlacement { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual User User { get; set; } = null!;

    }
}
