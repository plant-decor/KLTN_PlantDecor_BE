using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class AssignServiceRegistrationCaretakerRequestDto
    {
        [Required]
        public int CaretakerId { get; set; }
    }
}