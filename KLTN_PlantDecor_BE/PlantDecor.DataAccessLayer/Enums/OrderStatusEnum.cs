namespace PlantDecor.DataAccessLayer.Enums
{
    public enum OrderStatusEnum
    {
        Pending = 0, // tạo đơn hàng nhưng chưa thanh toán //Service
        DepositPaid = 1, // đã thanh toán tiền đặt cọc, chờ thanh toán phần còn lại
        Paid = 2, // đã thanh toán đầy đủ, chờ xử lý đơn hàng //Paid
        Assigned = 3, // đã được phân công cho nhân viên xử lý, chờ xử lý đơn hàng
        Shipping = 4, // đang vận chuyển, chờ giao hàng
        Delivered = 5, //   đã giao hàng, chờ xác nhận đã nhận được hàng
        RemainingPaymentPending = 6,// đã giao hàng nhưng chưa thanh toán phần còn lại, chờ thanh toán phần còn lại
        Completed = 7, // đã hoàn thành, đã thanh toán đầy đủ và xác nhận đã nhận được hàng //Service
        Cancelled = 8, // đã hủy, có thể do khách hàng hủy trước khi thanh toán hoặc do nhân viên hủy sau khi thanh toán //Service
        Failed = 9, // đã thất bại, có thể do thanh toán thất bại hoặc do vấn đề xử lý đơn hàng //Service
        RefundRequested = 10, // khách hàng đã yêu cầu hoàn tiền, chờ xử lý yêu cầu hoàn tiền
        Refunded = 11, // đã hoàn tiền, đã xử lý yêu cầu hoàn tiền và hoàn tiền thành công
        Rejected = 12, // yêu cầu hoàn tiền đã bị từ chối, có thể do yêu cầu hoàn tiền không hợp lệ hoặc do vấn đề xử lý yêu cầu hoàn tiền
        PendingConfirmation = 13 // đơn đã giao tới, hệ thống sẽ tự động xác nhận đã nhận được hàng sau một khoảng thời gian nếu không có khiếu nại từ khách hàng
    }
}
