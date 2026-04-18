using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CreateNurseryDesignTemplateRequestDto
    {
        [Required]
        public int DesignTemplateId { get; set; }
    }
}
