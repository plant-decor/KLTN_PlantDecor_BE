using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CreateNurseryCareServiceRequestDto
    {
        [Required]
        public int CareServicePackageId { get; set; }
    }
}
