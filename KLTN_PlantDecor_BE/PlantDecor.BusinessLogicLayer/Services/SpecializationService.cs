using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class SpecializationService : ISpecializationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SpecializationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<SpecializationResponseDto>> GetAllAsync()
        {
            var all = await _unitOfWork.SpecializationRepository.GetAllAsync();
            return all.OrderBy(s => s.Name).Select(MapToDto).ToList();
        }

        public async Task<List<SpecializationResponseDto>> GetAllActiveAsync()
        {
            var active = await _unitOfWork.SpecializationRepository.GetAllActiveAsync();
            return active.Select(MapToDto).ToList();
        }

        public async Task<SpecializationResponseDto> GetByIdAsync(int id)
        {
            var spec = await _unitOfWork.SpecializationRepository.GetByIdAsync(id);
            if (spec == null)
                throw new NotFoundException($"Specialization {id} not found");
            return MapToDto(spec);
        }

        public async Task<SpecializationResponseDto> CreateAsync(SpecializationRequestDto request)
        {
            if (await _unitOfWork.SpecializationRepository.ExistsByNameAsync(request.Name))
                throw new BadRequestException($"A specialization named '{request.Name}' already exists");

            var entity = new Specialization
            {
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                IsActive = true
            };

            _unitOfWork.SpecializationRepository.PrepareCreate(entity);
            await _unitOfWork.SaveAsync();
            return MapToDto(entity);
        }

        public async Task<SpecializationResponseDto> UpdateAsync(int id, UpdateSpecializationRequestDto request)
        {
            var entity = await _unitOfWork.SpecializationRepository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"Specialization {id} not found");

            if (request.Name != null && request.Name != entity.Name)
            {
                if (await _unitOfWork.SpecializationRepository.ExistsByNameAsync(request.Name, id))
                    throw new BadRequestException($"A specialization named '{request.Name}' already exists");
                entity.Name = request.Name;
            }

            if (request.Description != null) entity.Description = request.Description;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;

            _unitOfWork.SpecializationRepository.PrepareUpdate(entity);
            await _unitOfWork.SaveAsync();
            return MapToDto(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _unitOfWork.SpecializationRepository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"Specialization {id} not found");

            entity.IsActive = false;
            _unitOfWork.SpecializationRepository.PrepareUpdate(entity);
            await _unitOfWork.SaveAsync();
        }

        public async Task<StaffWithSpecializationsResponseDto> AssignToStaffAsync(int managerId, int staffId, int specializationId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

            var staff = await _unitOfWork.UserRepository.GetCaretakerByIdWithSpecializationsAsync(staffId, nursery.Id);
            if (staff == null)
                throw new NotFoundException($"Caretaker {staffId} not found in your nursery");

            var spec = await _unitOfWork.SpecializationRepository.GetByIdAsync(specializationId);
            if (spec == null)
                throw new NotFoundException($"Specialization {specializationId} not found");

            if (!spec.IsActive)
                throw new BadRequestException("Specialization is not active");

            var existing = await _unitOfWork.SpecializationRepository.GetStaffSpecializationAsync(staffId, specializationId);
            if (existing != null)
                throw new BadRequestException("Staff already has this specialization");

            var assignment = new StaffSpecialization
            {
                StaffId = staffId,
                SpecializationId = specializationId
            };

            await _unitOfWork.SpecializationRepository.AddStaffSpecializationAsync(assignment);

            var updated = await _unitOfWork.UserRepository.GetCaretakerByIdWithSpecializationsAsync(staffId, nursery.Id);
            return NurseryService.MapToStaffDtoPublic(updated!);
        }

        public async Task<StaffWithSpecializationsResponseDto> RemoveFromStaffAsync(int managerId, int staffId, int specializationId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

            var staff = await _unitOfWork.UserRepository.GetCaretakerByIdWithSpecializationsAsync(staffId, nursery.Id);
            if (staff == null)
                throw new NotFoundException($"Caretaker {staffId} not found in your nursery");

            var existing = await _unitOfWork.SpecializationRepository.GetStaffSpecializationAsync(staffId, specializationId);
            if (existing == null)
                throw new NotFoundException("Staff does not have this specialization");

            await _unitOfWork.SpecializationRepository.RemoveStaffSpecializationAsync(existing);

            var updated = await _unitOfWork.UserRepository.GetCaretakerByIdWithSpecializationsAsync(staffId, nursery.Id);
            return NurseryService.MapToStaffDtoPublic(updated!);
        }

        public async Task<List<StaffWithSpecializationsResponseDto>> GetEligibleCaretakersForPackageAsync(int managerId, int packageId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

            var pkg = await _unitOfWork.CareServicePackageRepository.GetByIdWithDetailsAsync(packageId);
            if (pkg == null)
                throw new NotFoundException($"CareServicePackage {packageId} not found");

            var requiredSpecIds = pkg.CareServiceSpecializations.Select(cs => cs.SpecializationId).ToHashSet();

            var allStaff = await _unitOfWork.UserRepository.GetCaretakersByNurseryIdAsync(nursery.Id);

            if (requiredSpecIds.Count == 0)
                return allStaff.Select(NurseryService.MapToStaffDtoPublic).ToList();

            return allStaff
                .Where(u => requiredSpecIds.All(reqId => u.StaffSpecializations.Any(ss => ss.SpecializationId == reqId)))
                .Select(NurseryService.MapToStaffDtoPublic)
                .ToList();
        }

        private static SpecializationResponseDto MapToDto(Specialization s) => new()
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            IsActive = s.IsActive
        };
    }
}
