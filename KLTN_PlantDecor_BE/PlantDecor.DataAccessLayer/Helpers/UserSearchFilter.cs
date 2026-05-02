using PlantDecor.DataAccessLayer.Enums;
using System;

namespace PlantDecor.DataAccessLayer.Helpers
{
    public class UserSearchFilter
    {
        public string? Keyword { get; set; }
        public RoleEnum? Role { get; set; }
        public UserStatusEnum? Status { get; set; }
        public bool? IsVerified { get; set; }
        public int? NurseryId { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }
}
