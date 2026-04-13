namespace PlantDecor.DataAccessLayer.Enums
{
    public enum LayoutDesignStatusEnum
    {
        Processing = 0, // Đang xử lý
        PlantRecommendationCompleted = 1, // Hoàn thành gợi ý cây
        Failed = 2, // Thất bại
        ImageGenerationCompleted = 3 // Hoàn thành tạo ảnh
    }
}
