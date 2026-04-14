using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Enums
{
    public enum ServiceRegistrationStatusEnum
    {
        PendingApproval = 1, // Đăng ký dịch vụ đang chờ phê duyệt
        AwaitPayment = 2, // Đăng ký dịch vụ đã được phê duyệt nhưng chưa thanh toán
        Active = 3, // Đăng ký dịch vụ đã được phê duyệt và thanh toán, đang hoạt động
        Completed = 4, // Dịch vụ đã hoàn thành
        Cancelled = 5, // Đăng ký dịch vụ đã bị hủy (bởi khách hàng)
        Rejected = 6, // Đăng ký dịch vụ bị từ chối (bởi manager)
    }
}
