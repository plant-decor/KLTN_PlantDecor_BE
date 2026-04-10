using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface ICareServicePackageRepository : IGenericRepository<CareServicePackage>
    {
        Task<List<CareServicePackage>> GetAllActiveAsync();
        Task<CareServicePackage?> GetByIdWithDetailsAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        /// <summary>Trả về các gói dịch vụ có ít nhất 1 vựa đang kinh doanh</summary>
        Task<List<CareServicePackage>> GetPackagesWithNurseriesAsync();
        /// <summary>Trả về các gói dịch vụ mà vựa chưa đang kinh doanh (active)</summary>
        Task<List<CareServicePackage>> GetNotActivelyOfferedByNurseryAsync(int nurseryId);
        Task AddSpecializationsAsync(int packageId, List<int> specializationIds);
    }
}
