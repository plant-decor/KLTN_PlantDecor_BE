namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CreateOrderRequestDto
    {
        // Thông tin giao hàng
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? CustomerName { get; set; }
        public string? Note { get; set; }

        // Chiến lược thanh toán: FullPayment = 1, Deposit = 2
        public int PaymentStrategy { get; set; }

        // Loại đơn hàng: PlantInstance = 1, OtherProduct = 2
        public int OrderType { get; set; }

        // OPTION 1: Checkout từ Cart (OtherProduct)
        // Nếu null hoặc rỗng → checkout toàn bộ Cart
        // Nếu có giá trị → chỉ checkout các CartItem được chọn
        public List<int>? CartItemIds { get; set; }

        // OPTION 2: Mua ngay PlantInstance (không qua Cart)
        public int? PlantInstanceId { get; set; }
    }
}
