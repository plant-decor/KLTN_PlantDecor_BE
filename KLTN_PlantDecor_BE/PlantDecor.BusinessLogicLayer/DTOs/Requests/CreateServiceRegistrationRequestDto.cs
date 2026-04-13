using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CreateServiceRegistrationRequestDto
    {
        [Required]
        public int NurseryCareServiceId { get; set; }

        [Required]
        public DateOnly ServiceDate { get; set; }

        /// <summary>
        /// Ngày trong tuần chăm sóc, dạng mảng int theo DayOfWeek: 1=Mon, ..., 6=Sat
        /// Ví dụ: [1, 3] = Thứ 2 và Thứ 4
        /// </summary>
        [Required]
        public List<int> ScheduleDaysOfWeek { get; set; } = new();

        [Required]
        public int PreferredShiftId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$", ErrorMessage = "Số điện thoại không hợp lệ (phải là số điện thoại Việt Nam 10 chữ số)")]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Note { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
