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
        DepositPaid = 3, // Đã đặt cọc, chờ hoàn thành
        InProgress = 4, // Đang thực hiện
        AwaitFinalPayment = 5, // Đã hoàn thành, chờ thanh toán phần còn lại
        Completed = 6, // Đã hoàn thành
        Rejected = 7, // Đã bị từ chối
        Cancelled = 8 // Đã bị hủy
    }
}
