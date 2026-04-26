using FluentAssertions;
using Moq;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Services;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Interfaces;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.Tests;

public class ServiceRatingServiceUnitTest
{
    private static ServiceRatingService CreateSut(Mock<IUnitOfWork> unitOfWork)
    {
        return new ServiceRatingService(unitOfWork.Object);
    }

    [Fact]
    public async Task Normal_CreateRatingAsync_ShouldCreateRating_WhenCustomerRatesCompletedRegistration()
    {
        var customer = new User
        {
            Id = 100,
            Username = "Customer",
            Email = "customer@example.com"
        };

        var registration = new ServiceRegistration
        {
            Id = 700,
            UserId = customer.Id,
            Status = (int)ServiceRegistrationStatusEnum.Completed
        };

        var rating = new ServiceRating
        {
            Id = 55,
            ServiceRegistrationId = registration.Id,
            UserId = customer.Id,
            Rating = 5,
            Description = "Great service",
            CreatedAt = DateTime.Now,
            User = customer
        };

        var registrationRepository = new Mock<IServiceRegistrationRepository>(MockBehavior.Loose);
        registrationRepository.Setup(r => r.GetByIdWithDetailsAsync(registration.Id)).ReturnsAsync(registration);

        var ratingRepository = new Mock<IServiceRatingRepository>(MockBehavior.Loose);
        ratingRepository.Setup(r => r.ExistsForRegistrationAsync(registration.Id)).ReturnsAsync(false);
        ratingRepository.Setup(r => r.PrepareCreate(It.IsAny<ServiceRating>()))
            .Callback<ServiceRating>(entity => entity.Id = rating.Id);
        ratingRepository.Setup(r => r.GetByRegistrationIdAsync(registration.Id)).ReturnsAsync(rating);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceRegistrationRepository).Returns(registrationRepository.Object);
        unitOfWork.SetupGet(x => x.ServiceRatingRepository).Returns(ratingRepository.Object);
        unitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(unitOfWork);

        var request = new CreateServiceRatingRequestDto
        {
            ServiceRegistrationId = registration.Id,
            Rating = 5,
            Description = "Great service"
        };

        var result = await sut.CreateRatingAsync(customer.Id, request);

        result.Id.Should().Be(rating.Id);
        result.Rating.Should().Be(5);
        result.Customer!.Id.Should().Be(customer.Id);
        ratingRepository.Verify(r => r.PrepareCreate(It.IsAny<ServiceRating>()), Times.Once);
    }

    [Fact]
    public async Task Abnormal_CreateRatingAsync_ShouldThrowForbidden_WhenUserDoesNotOwnRegistration()
    {
        var registrationRepository = new Mock<IServiceRegistrationRepository>(MockBehavior.Loose);
        registrationRepository.Setup(r => r.GetByIdWithDetailsAsync(700)).ReturnsAsync(new ServiceRegistration
        {
            Id = 700,
            UserId = 999,
            Status = (int)ServiceRegistrationStatusEnum.Completed
        });

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceRegistrationRepository).Returns(registrationRepository.Object);

        var sut = CreateSut(unitOfWork);

        var act = () => sut.CreateRatingAsync(100, new CreateServiceRatingRequestDto
        {
            ServiceRegistrationId = 700,
            Rating = 5,
            Description = "Great service"
        });

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You are not the customer of this service registration");
    }

    [Fact]
    public async Task Normal_CreateRatingAsync_ShouldCreateRating_WhenDescriptionIsNull()
    {
        var registration = new ServiceRegistration { Id = 701, UserId = 101, Status = (int)ServiceRegistrationStatusEnum.Completed };
        var created = new ServiceRating { Id = 56, ServiceRegistrationId = 701, UserId = 101, Rating = 4, Description = null, User = new User { Id = 101, Email = "u@x.com" } };

        var registrationRepository = new Mock<IServiceRegistrationRepository>(MockBehavior.Loose);
        registrationRepository.Setup(r => r.GetByIdWithDetailsAsync(701)).ReturnsAsync(registration);

        var ratingRepository = new Mock<IServiceRatingRepository>(MockBehavior.Loose);
        ratingRepository.Setup(r => r.ExistsForRegistrationAsync(701)).ReturnsAsync(false);
        ratingRepository.Setup(r => r.GetByRegistrationIdAsync(701)).ReturnsAsync(created);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceRegistrationRepository).Returns(registrationRepository.Object);
        unitOfWork.SetupGet(x => x.ServiceRatingRepository).Returns(ratingRepository.Object);
        unitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(unitOfWork);
        var result = await sut.CreateRatingAsync(101, new CreateServiceRatingRequestDto { ServiceRegistrationId = 701, Rating = 4 });

        result.Id.Should().Be(56);
        result.Description.Should().BeNull();
    }

    [Fact]
    public async Task Normal_CreateRatingAsync_ShouldCreateRating_AtMiddleValue()
    {
        var registration = new ServiceRegistration { Id = 702, UserId = 102, Status = (int)ServiceRegistrationStatusEnum.Completed };
        var created = new ServiceRating { Id = 57, ServiceRegistrationId = 702, UserId = 102, Rating = 3, User = new User { Id = 102, Email = "m@x.com" } };

        var registrationRepository = new Mock<IServiceRegistrationRepository>(MockBehavior.Loose);
        registrationRepository.Setup(r => r.GetByIdWithDetailsAsync(702)).ReturnsAsync(registration);

        var ratingRepository = new Mock<IServiceRatingRepository>(MockBehavior.Loose);
        ratingRepository.Setup(r => r.ExistsForRegistrationAsync(702)).ReturnsAsync(false);
        ratingRepository.Setup(r => r.GetByRegistrationIdAsync(702)).ReturnsAsync(created);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceRegistrationRepository).Returns(registrationRepository.Object);
        unitOfWork.SetupGet(x => x.ServiceRatingRepository).Returns(ratingRepository.Object);
        unitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var result = await CreateSut(unitOfWork).CreateRatingAsync(102, new CreateServiceRatingRequestDto { ServiceRegistrationId = 702, Rating = 3 });
        result.Rating.Should().Be(3);
    }

    [Fact]
    public async Task Boundary_CreateRatingAsync_ShouldCreateRating_AtLowerBound()
    {
        var registration = new ServiceRegistration { Id = 703, UserId = 103, Status = (int)ServiceRegistrationStatusEnum.Completed };
        var created = new ServiceRating { Id = 58, ServiceRegistrationId = 703, UserId = 103, Rating = 1, User = new User { Id = 103, Email = "lb@x.com" } };

        var registrationRepository = new Mock<IServiceRegistrationRepository>(MockBehavior.Loose);
        registrationRepository.Setup(r => r.GetByIdWithDetailsAsync(703)).ReturnsAsync(registration);

        var ratingRepository = new Mock<IServiceRatingRepository>(MockBehavior.Loose);
        ratingRepository.Setup(r => r.ExistsForRegistrationAsync(703)).ReturnsAsync(false);
        ratingRepository.Setup(r => r.GetByRegistrationIdAsync(703)).ReturnsAsync(created);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceRegistrationRepository).Returns(registrationRepository.Object);
        unitOfWork.SetupGet(x => x.ServiceRatingRepository).Returns(ratingRepository.Object);
        unitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var result = await CreateSut(unitOfWork).CreateRatingAsync(103, new CreateServiceRatingRequestDto { ServiceRegistrationId = 703, Rating = 1 });
        result.Rating.Should().Be(1);
    }

    [Fact]
    public async Task Boundary_CreateRatingAsync_ShouldCreateRating_AtUpperBound()
    {
        var registration = new ServiceRegistration { Id = 704, UserId = 104, Status = (int)ServiceRegistrationStatusEnum.Completed };
        var created = new ServiceRating { Id = 59, ServiceRegistrationId = 704, UserId = 104, Rating = 5, User = new User { Id = 104, Email = "ub@x.com" } };

        var registrationRepository = new Mock<IServiceRegistrationRepository>(MockBehavior.Loose);
        registrationRepository.Setup(r => r.GetByIdWithDetailsAsync(704)).ReturnsAsync(registration);

        var ratingRepository = new Mock<IServiceRatingRepository>(MockBehavior.Loose);
        ratingRepository.Setup(r => r.ExistsForRegistrationAsync(704)).ReturnsAsync(false);
        ratingRepository.Setup(r => r.GetByRegistrationIdAsync(704)).ReturnsAsync(created);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceRegistrationRepository).Returns(registrationRepository.Object);
        unitOfWork.SetupGet(x => x.ServiceRatingRepository).Returns(ratingRepository.Object);
        unitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var result = await CreateSut(unitOfWork).CreateRatingAsync(104, new CreateServiceRatingRequestDto { ServiceRegistrationId = 704, Rating = 5 });
        result.Rating.Should().Be(5);
    }

    [Fact]
    public async Task Abnormal_CreateRatingAsync_ShouldThrowBadRequest_WhenAlreadyRated()
    {
        var registrationRepository = new Mock<IServiceRegistrationRepository>(MockBehavior.Loose);
        registrationRepository.Setup(r => r.GetByIdWithDetailsAsync(706)).ReturnsAsync(new ServiceRegistration
        {
            Id = 706,
            UserId = 106,
            Status = (int)ServiceRegistrationStatusEnum.Completed
        });

        var ratingRepository = new Mock<IServiceRatingRepository>(MockBehavior.Loose);
        ratingRepository.Setup(r => r.ExistsForRegistrationAsync(706)).ReturnsAsync(true);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceRegistrationRepository).Returns(registrationRepository.Object);
        unitOfWork.SetupGet(x => x.ServiceRatingRepository).Returns(ratingRepository.Object);

        var act = () => CreateSut(unitOfWork).CreateRatingAsync(106, new CreateServiceRatingRequestDto { ServiceRegistrationId = 706, Rating = 4 });
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("You have already submitted a rating for this service");
    }

    [Fact]
    public async Task CreateRatingAsync_ShouldThrowNotFound_WhenRegistrationNotFound()
    {
        var registrationRepository = new Mock<IServiceRegistrationRepository>(MockBehavior.Loose);
        registrationRepository.Setup(r => r.GetByIdWithDetailsAsync(999)).ReturnsAsync((ServiceRegistration?)null);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceRegistrationRepository).Returns(registrationRepository.Object);

        var sut = CreateSut(unitOfWork);
        var act = () => sut.CreateRatingAsync(1, new CreateServiceRatingRequestDto { ServiceRegistrationId = 999, Rating = 5 });

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("ServiceRegistration 999 not found");
    }

    [Fact]
    public async Task CreateRatingAsync_ShouldThrowBadRequest_WhenRegistrationNotCompleted()
    {
        var registrationRepository = new Mock<IServiceRegistrationRepository>(MockBehavior.Loose);
        registrationRepository.Setup(r => r.GetByIdWithDetailsAsync(707)).ReturnsAsync(new ServiceRegistration
        {
            Id = 707,
            UserId = 107,
            Status = (int)ServiceRegistrationStatusEnum.Active
        });

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceRegistrationRepository).Returns(registrationRepository.Object);

        var sut = CreateSut(unitOfWork);
        var act = () => sut.CreateRatingAsync(107, new CreateServiceRatingRequestDto { ServiceRegistrationId = 707, Rating = 4 });

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("You can only rate a service registration after it is completed");
    }
}