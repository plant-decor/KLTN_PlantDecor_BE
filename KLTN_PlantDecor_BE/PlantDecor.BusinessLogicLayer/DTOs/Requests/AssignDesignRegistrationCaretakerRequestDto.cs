using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class AssignDesignRegistrationCaretakerRequestDto
    {
        [Required]
        public int CaretakerId { get; set; }

        public DateOnly? StartDate { get; set; }
    }
}
