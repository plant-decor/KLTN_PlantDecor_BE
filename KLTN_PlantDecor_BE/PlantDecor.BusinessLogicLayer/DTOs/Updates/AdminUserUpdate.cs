using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Updates
{
    public class AdminUserUpdate
    {
        public RoleEnum? Role { get; set; }
        public UserStatusEnum? Status { get; set; }
        public int? NurseryId { get; set; }  // For assigning shippers to nursery

        public bool isVerified { get; set; }
    }
}
