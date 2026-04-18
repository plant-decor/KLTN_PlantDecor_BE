using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Enums
{
    public enum DesignRegistrationStatus
    {
        PendingApproval = 1, // Đang chờ xử lý
        AwaitDeposit = 2, // Đã được phê duyệt, chờ đặt cọc
        Active = 3, // Đã được phê duyệt
        Rejected = 4, // Đã bị từ chối
        Completed = 5, // Đã hoàn thành
        Cancelled = 6 // Đã bị hủy
    }
}
