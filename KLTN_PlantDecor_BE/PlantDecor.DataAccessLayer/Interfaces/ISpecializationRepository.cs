using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface ISpecializationRepository : IGenericRepository<Specialization>
    {
        Task<List<Specialization>> GetAllActiveAsync();
        Task<Specialization?> GetByIdWithRelationsAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<StaffSpecialization?> GetStaffSpecializationAsync(int staffId, int specializationId);
        Task AddStaffSpecializationAsync(StaffSpecialization assignment);
        Task RemoveStaffSpecializationAsync(StaffSpecialization existing);
        Task<List<StaffSpecialization>> GetStaffSpecializationsAsync(int staffId);
        Task ReplaceStaffSpecializationsAsync(int staffId, List<int> specializationIds);
    }
}
