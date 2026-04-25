using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class ServiceRatingService : IServiceRatingService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ServiceRatingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceRatingResponseDto> CreateRatingAsync(int userId, CreateServiceRatingRequestDto request)
        {
            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(request.ServiceRegistrationId);
            if (registration == null)
                throw new NotFoundException($"ServiceRegistration {request.ServiceRegistrationId} not found");

            // Only the customer who owns this registration can rate
            if (registration.UserId != userId)
                throw new ForbiddenException("You are not the customer of this service registration");

            // Only allow rating when service is Completed
            if (registration.Status != (int)ServiceRegistrationStatusEnum.Completed)
                throw new BadRequestException("You can only rate a service registration after it is completed");

            // One rating per registration
            var alreadyRated = await _unitOfWork.ServiceRatingRepository.ExistsForRegistrationAsync(request.ServiceRegistrationId);
            if (alreadyRated)
                throw new BadRequestException("You have already submitted a rating for this service");

            var rating = new ServiceRating
            {
                ServiceRegistrationId = request.ServiceRegistrationId,
                UserId = userId,
                Rating = request.Rating,
                Description = request.Description,
                CreatedAt = DateTime.Now
            };

            _unitOfWork.ServiceRatingRepository.PrepareCreate(rating);
            await _unitOfWork.SaveAsync();

            var created = await _unitOfWork.ServiceRatingRepository.GetByRegistrationIdAsync(request.ServiceRegistrationId);
            return MapToDto(created!);
        }

        public async Task<ServiceRatingResponseDto> GetByRegistrationIdAsync(int registrationId)
        {
            var rating = await _unitOfWork.ServiceRatingRepository.GetByRegistrationIdAsync(registrationId);
            if (rating == null)
                throw new NotFoundException("No rating found for this service registration");

            return MapToDto(rating);
        }

        public static ServiceRatingResponseDto MapToDto(ServiceRating r) => new ServiceRatingResponseDto
        {
            Id = r.Id,
            ServiceRegistrationId = r.ServiceRegistrationId,
            Rating = r.Rating,
            Description = r.Description,
            CreatedAt = r.CreatedAt,
            Customer = r.User == null ? null : r.User.ToUserSummary()
        };
    }
}
