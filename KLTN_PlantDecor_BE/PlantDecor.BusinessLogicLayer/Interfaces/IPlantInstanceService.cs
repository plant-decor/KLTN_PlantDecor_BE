using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using Microsoft.AspNetCore.Http;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IPlantInstanceService
    {
        #region Manager Operations

        /// <summary>
        /// Batch tạo nhiều PlantInstance cho một nursery
        /// POST /api/manager/nurseries/{nurseryId}/plant-instances/batch
        /// </summary>
        Task<BatchCreatePlantInstanceResponseDto> BatchCreateAsync(int nurseryId, int managerId, BatchCreatePlantInstanceRequestDto request);

        /// <summary>
        /// Lấy chi tiết một PlantInstance theo ID
        /// GET /api/manager/plant-instances/{instanceId}
        /// </summary>
        Task<PlantInstanceResponseDto> GetByIdAsync(int instanceId, int managerId);

        /// <summary>
        /// Lấy danh sách PlantInstance theo nursery (phân trang + lọc theo status)
        /// GET /api/manager/nurseries/{nurseryId}/plant-instances
        /// </summary>
        Task<PaginatedResult<PlantInstanceListResponseDto>> GetByNurseryIdAsync(int nurseryId, int managerId, Pagination pagination, int? statusFilter = null);

        /// <summary>
        /// Lấy tổng hợp thông tin plant theo nursery
        /// GET /api/manager/nurseries/{nurseryId}/plants-summary
        /// </summary>
        Task<List<NurseryPlantSummaryDto>> GetPlantsSummaryByNurseryAsync(int nurseryId, int managerId);

        /// <summary>
        /// Cập nhật status một PlantInstance
        /// PATCH /api/manager/plant-instances/{instanceId}/status
        /// </summary>
        Task<PlantInstanceResponseDto> UpdateStatusAsync(int instanceId, int managerId, UpdatePlantInstanceStatusDto request);

        /// <summary>
        /// Cập nhật thông tin PlantInstance
        /// PATCH /api/manager/plant-instances/{instanceId}
        /// </summary>
        Task<PlantInstanceResponseDto> UpdateAsync(int instanceId, int managerId, UpdatePlantInstanceRequestDto request);

        /// <summary>
        /// Batch cập nhật status nhiều PlantInstance
        /// PATCH /api/manager/plant-instances/batch-status
        /// </summary>
        Task<BatchUpdateStatusResponseDto> BatchUpdateStatusAsync(int managerId, BatchUpdatePlantInstanceStatusDto request);

        /// <summary>
        /// Upload ảnh cho PlantInstance
        /// POST /api/manager/plant-instances/{instanceId}/images
        /// </summary>
        Task<PlantInstanceResponseDto> UploadPlantInstanceThumbnailAsync(int instanceId, int managerId, IFormFile file);
        Task<PlantInstanceResponseDto> UploadPlantInstanceImagesAsync(int instanceId, int managerId, List<IFormFile> files);
        Task<PlantInstanceResponseDto> SetPrimaryInstanceImageAsync(int instanceId, int managerId, int imageId);
        Task<PlantInstanceResponseDto> ReplaceInstanceImageAsync(int instanceId, int managerId, int imageId, IFormFile file);
        Task<PlantInstanceResponseDto> DeleteInstanceImageAsync(int instanceId, int managerId, int imageId);

        #endregion

        #region Shop Operations

        /// <summary>
        /// Lấy danh sách nursery đang có plant available
        /// GET /api/plants/{id}/nurseries
        /// </summary>
        Task<List<PlantNurseryAvailabilityDto>> GetNurseriesByPlantIdAsync(int plantId);

        /// <summary>
        /// Lấy danh sách PlantInstance available theo nursery (Shop - phân trang)
        /// GET /api/nurseries/{nurseryId}/plant-instances
        /// </summary>
        Task<PaginatedResult<PlantInstanceListResponseDto>> GetAvailableByNurseryIdAsync(int nurseryId, Pagination pagination, int? plantId = null);

        /// <summary>
        /// Lấy chi tiết PlantInstance (Shop)
        /// GET /api/plant-instances/{instanceId}
        /// </summary>
        Task<PlantInstanceResponseDto> GetInstanceDetailAsync(int instanceId);

        /// <summary>
        /// Tìm kiếm PlantInstance available cho shop (toàn hệ thống hoặc theo vựa)
        /// POST /api/shop/plant-instances/search
        /// </summary>
        Task<PaginatedResult<PlantInstanceListResponseDto>> SearchAvailableForShopAsync(Pagination pagination, int? nurseryId = null, int? plantId = null);

        #endregion
    }
}
