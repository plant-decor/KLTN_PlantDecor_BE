using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Entities
{
    public class DepositPolicy
    {
        public int Id { get; set; }
        public decimal MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int DepositPercentage { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
