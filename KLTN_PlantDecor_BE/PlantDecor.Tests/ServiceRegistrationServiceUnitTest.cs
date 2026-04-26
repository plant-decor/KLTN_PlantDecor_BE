using System.Text.Json;
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

public class ServiceRegistrationServiceUnitTest
{
    private static CreateServiceRegistrationRequestDto BuildCreateRequest(int packageId, int shiftId, int? nurseryId = null) => new()
    {
        CareServicePackageId = packageId,
        PreferredNurseryId = nurseryId,
        ServiceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
        PreferredShiftId = shiftId,
        Address = "123 Test Street",
        Phone = "0912345678",
        Latitude = 10.77m,
        Longitude = 106.69m
    };

    private static ServiceRegistrationService CreateSut(Mock<IUnitOfWork> unitOfWork)
    {
        return new ServiceRegistrationService(unitOfWork.Object);
    }

    private static Mock<IUnitOfWork> CreateUnitOfWork(
        Mock<IServiceRegistrationRepository> registrationRepository,
        Mock<IServiceProgressRepository>? progressRepository = null)
    {
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceRegistrationRepository).Returns(registrationRepository.Object);
        unitOfWork.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);
        if (progressRepository != null)
        {
            unitOfWork.SetupGet(x => x.ServiceProgressRepository).Returns(progressRepository.Object);
        }
        return unitOfWork;
    }

    [Fact]
    public async Task Normal_CreateAsync_ShouldCreateRegistration_WhenPreferredNurseryIsAvailable()
    {
        var nursery = new Nursery { Id = 7, Name = "Nursery A", IsActive = true };
        var shift = new Shift { Id = 5, ShiftName = "Morning", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0) };
        var package = new CareServicePackage
        {
            Id = 20,
            Name = "One time package",
            ServiceType = (int)CareServiceTypeEnum.OneTime,
            DurationDays = 1,
            IsActive = true,
            CareServiceSpecializations = new List<CareServiceSpecialization>(),
            PackagePlantSuitabilities = new List<PackagePlantSuitability>()
        };
        var caretaker = new User
        {
            Id = 1000,
            Username = "Caretaker 1",
            Email = "caretaker@example.com",
            RoleId = (int)RoleEnum.Caretaker,
            Status = (int)UserStatusEnum.Active,
            IsVerified = true,
            NurseryId = nursery.Id
        };
        var nurseryService = new NurseryCareService
        {
            Id = 88,
            NurseryId = nursery.Id,
            Nursery = nursery,
            CareServicePackageId = package.Id,
            CareServicePackage = package,
            IsActive = true
        };
        var createdRegistration = new ServiceRegistration
        {
            Id = 100,
            UserId = 500,
            NurseryCareServiceId = nurseryService.Id,
            NurseryCareService = nurseryService,
            PrefferedShift = shift,
            Status = (int)ServiceRegistrationStatusEnum.PendingApproval,
            ServiceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            Address = "123 Test Street",
            Phone = "0912345678",
            ScheduleDaysOfWeek = JsonSerializer.Serialize(new List<int>())
        };

        var shiftRepository = new Mock<IShiftRepository>(MockBehavior.Loose);
        shiftRepository.Setup(r => r.GetByIdAsync(shift.Id)).ReturnsAsync(shift);

        var packageRepository = new Mock<ICareServicePackageRepository>(MockBehavior.Loose);
        packageRepository.Setup(r => r.GetByIdWithDetailsAsync(package.Id)).ReturnsAsync(package);

        var nurseryRepository = new Mock<INurseryRepository>(MockBehavior.Loose);
        nurseryRepository.Setup(r => r.GetByIdAsync(nursery.Id)).ReturnsAsync(nursery);
        nurseryRepository.Setup(r => r.GetByManagerIdAsync(It.IsAny<int>())).ReturnsAsync((Nursery?)null);

        var nurseryCareServiceRepository = new Mock<INurseryCareServiceRepository>(MockBehavior.Loose);
        nurseryCareServiceRepository.Setup(r => r.GetByNurseryIdAsync(nursery.Id)).ReturnsAsync(new List<NurseryCareService> { nurseryService });
        nurseryCareServiceRepository.Setup(r => r.GetActiveByPackageIdAsync(package.Id)).ReturnsAsync(new List<NurseryCareService> { nurseryService });

        var userRepository = new Mock<IUserRepository>(MockBehavior.Loose);
        userRepository.Setup(r => r.GetCaretakersByNurseryIdAsync(nursery.Id)).ReturnsAsync(new List<User> { caretaker });

        var serviceRegistrationRepository = new Mock<IServiceRegistrationRepository>(MockBehavior.Loose);
        serviceRegistrationRepository.Setup(r => r.CountOpenAssignmentsByCaretakerIdsAsync(It.IsAny<List<int>>(), It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, int>());
        serviceRegistrationRepository.Setup(r => r.PrepareCreate(It.IsAny<ServiceRegistration>()))
            .Callback<ServiceRegistration>(r => r.Id = createdRegistration.Id);
        serviceRegistrationRepository.Setup(r => r.GetByIdWithDetailsAsync(createdRegistration.Id)).ReturnsAsync(createdRegistration);

        var progressRepository = new Mock<IServiceProgressRepository>(MockBehavior.Loose);
        progressRepository.Setup(r => r.GetConflictingCaretakerIdsAsync(shift.Id, It.IsAny<List<DateOnly>>()))
            .ReturnsAsync(new HashSet<int>());

        var unitOfWork = CreateUnitOfWork(serviceRegistrationRepository, progressRepository);
        unitOfWork.SetupGet(x => x.ShiftRepository).Returns(shiftRepository.Object);
        unitOfWork.SetupGet(x => x.CareServicePackageRepository).Returns(packageRepository.Object);
        unitOfWork.SetupGet(x => x.NurseryRepository).Returns(nurseryRepository.Object);
        unitOfWork.SetupGet(x => x.NurseryCareServiceRepository).Returns(nurseryCareServiceRepository.Object);
        unitOfWork.SetupGet(x => x.UserRepository).Returns(userRepository.Object);

        var sut = CreateSut(unitOfWork);

        var request = BuildCreateRequest(package.Id, shift.Id, nursery.Id);

        var result = await sut.CreateAsync(500, request);

        result.Id.Should().Be(createdRegistration.Id);
        result.Status.Should().Be((int)ServiceRegistrationStatusEnum.PendingApproval);
        result.NurseryCareService!.Id.Should().Be(nurseryService.Id);
        result.PrefferedShift!.Id.Should().Be(shift.Id);
        unitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task Normal_RescheduleAsync_ShouldUpdateSchedule_WhenRequestIsValid()
    {
        var nursery = new Nursery { Id = 9, Name = "Nursery B", IsActive = true };
        var shift = new Shift { Id = 6, ShiftName = "Afternoon", StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(17, 0) };
        var package = new CareServicePackage
        {
            Id = 30,
            Name = "Package",
            ServiceType = (int)CareServiceTypeEnum.OneTime,
            DurationDays = 1,
            IsActive = true,
            CareServiceSpecializations = new List<CareServiceSpecialization>(),
            PackagePlantSuitabilities = new List<PackagePlantSuitability>()
        };
        var service = new NurseryCareService
        {
            Id = 99,
            NurseryId = nursery.Id,
            Nursery = nursery,
            CareServicePackageId = package.Id,
            CareServicePackage = package,
            IsActive = true
        };
        var registration = new ServiceRegistration
        {
            Id = 200,
            Status = (int)ServiceRegistrationStatusEnum.PendingApproval,
            NurseryCareService = service,
            NurseryCareServiceId = service.Id,
            ServiceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(4)),
            PreferredShiftId = 5,
            Address = "123 Test Street",
            Phone = "0912345678",
            ScheduleDaysOfWeek = JsonSerializer.Serialize(new List<int>())
        };

        var registrationRepository = new Mock<IServiceRegistrationRepository>(MockBehavior.Loose);
        registrationRepository.Setup(r => r.GetByIdWithDetailsAsync(registration.Id)).ReturnsAsync(registration);
        registrationRepository.Setup(r => r.GetByIdAsync(registration.Id)).ReturnsAsync(registration);
        registrationRepository.Setup(r => r.PrepareUpdate(It.IsAny<ServiceRegistration>()));

        var shiftRepository = new Mock<IShiftRepository>(MockBehavior.Loose);
        shiftRepository.Setup(r => r.GetByIdAsync(shift.Id)).ReturnsAsync(shift);

        var packageRepository = new Mock<ICareServicePackageRepository>(MockBehavior.Loose);
        packageRepository.Setup(r => r.GetByIdWithDetailsAsync(package.Id)).ReturnsAsync(package);

        var nurseryRepository = new Mock<INurseryRepository>(MockBehavior.Loose);
        nurseryRepository.Setup(r => r.GetByManagerIdAsync(1)).ReturnsAsync(nursery);

        var unitOfWork = CreateUnitOfWork(registrationRepository);
        unitOfWork.SetupGet(x => x.ShiftRepository).Returns(shiftRepository.Object);
        unitOfWork.SetupGet(x => x.CareServicePackageRepository).Returns(packageRepository.Object);
        unitOfWork.SetupGet(x => x.NurseryRepository).Returns(nurseryRepository.Object);

        var sut = CreateSut(unitOfWork);

        var request = new UpdateServiceRegistrationScheduleRequestDto
        {
            ServiceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(6)),
            PreferredShiftId = shift.Id
        };

        var result = await sut.RescheduleAsync(1, registration.Id, request);

        result.ServiceDate.Should().Be(DateOnly.FromDateTime(DateTime.Today.AddDays(6)));
        result.TotalSessions.Should().Be(1);
        registrationRepository.Verify(r => r.PrepareUpdate(registration), Times.Once);
    }

    [Fact]
    public async Task Normal_RescheduleAsync_ShouldKeepOriginalServiceDate_WhenOnlyShiftIsProvided()
    {
        var nursery = new Nursery { Id = 10, IsActive = true };
        var shift = new Shift { Id = 8, ShiftName = "Evening", StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(20, 0) };
        var package = new CareServicePackage { Id = 31, ServiceType = (int)CareServiceTypeEnum.OneTime, DurationDays = 1, IsActive = true };
        var service = new NurseryCareService { Id = 101, NurseryId = nursery.Id, Nursery = nursery, CareServicePackageId = package.Id, CareServicePackage = package, IsActive = true };
        var originalDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5));
        var registration = new ServiceRegistration
        {
            Id = 201,
            Status = (int)ServiceRegistrationStatusEnum.PendingApproval,
            NurseryCareService = service,
            ServiceDate = originalDate,
            PreferredShiftId = 7,
            Address = "123",
            Phone = "0912345678",
            ScheduleDaysOfWeek = JsonSerializer.Serialize(new List<int>())
        };

        var registrationRepository = new Mock<IServiceRegistrationRepository>(MockBehavior.Loose);
        registrationRepository.Setup(r => r.GetByIdWithDetailsAsync(201)).ReturnsAsync(registration);
        registrationRepository.Setup(r => r.GetByIdAsync(201)).ReturnsAsync(registration);
        registrationRepository.Setup(r => r.PrepareUpdate(It.IsAny<ServiceRegistration>()));

        var shiftRepository = new Mock<IShiftRepository>(MockBehavior.Loose);
        shiftRepository.Setup(r => r.GetByIdAsync(shift.Id)).ReturnsAsync(shift);

        var packageRepository = new Mock<ICareServicePackageRepository>(MockBehavior.Loose);
        packageRepository.Setup(r => r.GetByIdWithDetailsAsync(package.Id)).ReturnsAsync(package);

        var nurseryRepository = new Mock<INurseryRepository>(MockBehavior.Loose);
        nurseryRepository.Setup(r => r.GetByManagerIdAsync(1)).ReturnsAsync(nursery);

        var unitOfWork = CreateUnitOfWork(registrationRepository);
        unitOfWork.SetupGet(x => x.ShiftRepository).Returns(shiftRepository.Object);
        unitOfWork.SetupGet(x => x.CareServicePackageRepository).Returns(packageRepository.Object);
        unitOfWork.SetupGet(x => x.NurseryRepository).Returns(nurseryRepository.Object);

        var sut = CreateSut(unitOfWork);
        var result = await sut.RescheduleAsync(1, 201, new UpdateServiceRegistrationScheduleRequestDto { PreferredShiftId = shift.Id });

        result.ServiceDate.Should().Be(originalDate);
        result.TotalSessions.Should().Be(1);
    }

    [Fact]
    public async Task Boundary_CreateAsync_ShouldThrow_WhenServiceDateInPast()
    {
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        var sut = CreateSut(unitOfWork);

        var request = BuildCreateRequest(1, 1, 1);
        request.ServiceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

        var act = () => sut.CreateAsync(1, request);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("ServiceDate cannot be in the past");
    }

    [Fact]
    public async Task Boundary_RescheduleAsync_ShouldThrow_WhenNoFieldsProvided()
    {
        var sut = CreateSut(new Mock<IUnitOfWork>(MockBehavior.Loose));

        var act = () => sut.RescheduleAsync(1, 1, new UpdateServiceRegistrationScheduleRequestDto());
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("At least ServiceDate or PreferredShiftId must be provided");
    }

    [Fact]
    public async Task Abnormal_CreateAsync_ShouldThrow_WhenShiftNotFound()
    {
        var shiftRepository = new Mock<IShiftRepository>(MockBehavior.Loose);
        shiftRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync((Shift?)null);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ShiftRepository).Returns(shiftRepository.Object);
        var sut = CreateSut(unitOfWork);

        var act = () => sut.CreateAsync(1, BuildCreateRequest(20, 5, 7));
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Shift 5 not found");
    }

    [Fact]
    public async Task Abnormal_RescheduleAsync_ShouldThrowForbidden_WhenRegistrationNotInOperatorNursery()
    {
        var nursery = new Nursery { Id = 11, IsActive = true };
        var otherService = new NurseryCareService { Id = 120, NurseryId = 99, Nursery = new Nursery { Id = 99, IsActive = true } };
        var registration = new ServiceRegistration
        {
            Id = 202,
            Status = (int)ServiceRegistrationStatusEnum.PendingApproval,
            NurseryCareService = otherService,
            ServiceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            PreferredShiftId = 2
        };

        var registrationRepository = new Mock<IServiceRegistrationRepository>(MockBehavior.Loose);
        registrationRepository.Setup(r => r.GetByIdWithDetailsAsync(202)).ReturnsAsync(registration);

        var nurseryRepository = new Mock<INurseryRepository>(MockBehavior.Loose);
        nurseryRepository.Setup(r => r.GetByManagerIdAsync(1)).ReturnsAsync(nursery);

        var unitOfWork = CreateUnitOfWork(registrationRepository);
        unitOfWork.SetupGet(x => x.NurseryRepository).Returns(nurseryRepository.Object);

        var sut = CreateSut(unitOfWork);

        var act = () => sut.RescheduleAsync(1, 202, new UpdateServiceRegistrationScheduleRequestDto { PreferredShiftId = 3 });
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("This registration does not belong to your nursery");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowBadRequest_WhenPackageInactive()
    {
        var shiftRepository = new Mock<IShiftRepository>(MockBehavior.Loose);
        shiftRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Shift { Id = 5, StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0) });

        var packageRepository = new Mock<ICareServicePackageRepository>(MockBehavior.Loose);
        packageRepository.Setup(r => r.GetByIdWithDetailsAsync(20)).ReturnsAsync(new CareServicePackage
        {
            Id = 20,
            IsActive = false,
            ServiceType = (int)CareServiceTypeEnum.OneTime,
            DurationDays = 1
        });

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ShiftRepository).Returns(shiftRepository.Object);
        unitOfWork.SetupGet(x => x.CareServicePackageRepository).Returns(packageRepository.Object);

        var sut = CreateSut(unitOfWork);
        var act = () => sut.CreateAsync(1, BuildCreateRequest(20, 5, 7));

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("This care service package is not currently active");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowBadRequest_WhenPreferredNurseryDoesNotOfferPackage()
    {
        var shift = new Shift { Id = 5, StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0) };
        var package = new CareServicePackage
        {
            Id = 20,
            IsActive = true,
            ServiceType = (int)CareServiceTypeEnum.OneTime,
            DurationDays = 1
        };

        var shiftRepository = new Mock<IShiftRepository>(MockBehavior.Loose);
        shiftRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(shift);

        var packageRepository = new Mock<ICareServicePackageRepository>(MockBehavior.Loose);
        packageRepository.Setup(r => r.GetByIdWithDetailsAsync(20)).ReturnsAsync(package);

        var nurseryRepository = new Mock<INurseryRepository>(MockBehavior.Loose);
        nurseryRepository.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new Nursery { Id = 7, IsActive = true });

        var nurseryCareServiceRepository = new Mock<INurseryCareServiceRepository>(MockBehavior.Loose);
        nurseryCareServiceRepository.Setup(r => r.GetByNurseryIdAsync(7)).ReturnsAsync(new List<NurseryCareService>());

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ShiftRepository).Returns(shiftRepository.Object);
        unitOfWork.SetupGet(x => x.CareServicePackageRepository).Returns(packageRepository.Object);
        unitOfWork.SetupGet(x => x.NurseryRepository).Returns(nurseryRepository.Object);
        unitOfWork.SetupGet(x => x.NurseryCareServiceRepository).Returns(nurseryCareServiceRepository.Object);

        var sut = CreateSut(unitOfWork);
        var act = () => sut.CreateAsync(1, BuildCreateRequest(20, 5, 7));

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Preferred nursery does not offer the selected service package");
    }

    [Fact]
    public async Task RescheduleAsync_ShouldThrowBadRequest_WhenStatusIsNotAllowed()
    {
        var nursery = new Nursery { Id = 20, IsActive = true };
        var registration = new ServiceRegistration
        {
            Id = 300,
            Status = (int)ServiceRegistrationStatusEnum.Active,
            NurseryCareService = new NurseryCareService { NurseryId = nursery.Id, Nursery = nursery, CareServicePackageId = 1 }
        };

        var registrationRepository = new Mock<IServiceRegistrationRepository>(MockBehavior.Loose);
        registrationRepository.Setup(r => r.GetByIdWithDetailsAsync(300)).ReturnsAsync(registration);

        var nurseryRepository = new Mock<INurseryRepository>(MockBehavior.Loose);
        nurseryRepository.Setup(r => r.GetByManagerIdAsync(1)).ReturnsAsync(nursery);

        var unitOfWork = CreateUnitOfWork(registrationRepository);
        unitOfWork.SetupGet(x => x.NurseryRepository).Returns(nurseryRepository.Object);

        var sut = CreateSut(unitOfWork);
        var act = () => sut.RescheduleAsync(1, 300, new UpdateServiceRegistrationScheduleRequestDto { PreferredShiftId = 3 });

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Only registrations in WaitingForNursery, PendingApproval or AwaitPayment can be rescheduled");
    }

    [Fact]
    public async Task RescheduleAsync_ShouldThrowNotFound_WhenTargetShiftNotFound()
    {
        var nursery = new Nursery { Id = 21, IsActive = true };
        var registration = new ServiceRegistration
        {
            Id = 301,
            Status = (int)ServiceRegistrationStatusEnum.PendingApproval,
            ServiceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            NurseryCareService = new NurseryCareService { NurseryId = nursery.Id, Nursery = nursery, CareServicePackageId = 1 }
        };

        var registrationRepository = new Mock<IServiceRegistrationRepository>(MockBehavior.Loose);
        registrationRepository.Setup(r => r.GetByIdWithDetailsAsync(301)).ReturnsAsync(registration);

        var nurseryRepository = new Mock<INurseryRepository>(MockBehavior.Loose);
        nurseryRepository.Setup(r => r.GetByManagerIdAsync(1)).ReturnsAsync(nursery);

        var shiftRepository = new Mock<IShiftRepository>(MockBehavior.Loose);
        shiftRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Shift?)null);

        var unitOfWork = CreateUnitOfWork(registrationRepository);
        unitOfWork.SetupGet(x => x.NurseryRepository).Returns(nurseryRepository.Object);
        unitOfWork.SetupGet(x => x.ShiftRepository).Returns(shiftRepository.Object);

        var sut = CreateSut(unitOfWork);
        var act = () => sut.RescheduleAsync(1, 301, new UpdateServiceRegistrationScheduleRequestDto { PreferredShiftId = 99 });

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Shift 99 not found");
    }
}