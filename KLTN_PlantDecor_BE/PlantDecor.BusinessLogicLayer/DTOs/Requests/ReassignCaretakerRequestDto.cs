using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class ReassignCaretakerRequestDto
    {
        [Required]
        public int NewCaretakerId { get; set; }
    }
}
