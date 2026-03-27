using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Enums
{
    public enum NurseryOrderStatus
    {
        Pending = 0, // mới tạo đơn hàng nhưng chưa thanh toán
        Paid = 1, // đã thanh toán đầy đủ, chờ xử lý đơn hàng
        DepositPaid = 2,// đã thanh toán tiền đặt cọc, chờ thanh toán phần còn lại
        Assigned = 3,// đã được phân công cho nhân viên xử lý, chờ xử lý đơn hàng
        Shipping = 4, // đang vận chuyển, chờ giao hàng
        Delivered = 5, // đã giao hàng, chờ xác nhận đã nhận được hàng
        Cancelled = 6 // đã hủy, có thể do khách hàng hủy trước khi thanh toán hoặc do nhân viên hủy sau khi thanh toán
    }
}
