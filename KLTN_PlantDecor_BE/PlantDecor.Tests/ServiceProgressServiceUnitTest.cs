using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Services;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Interfaces;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.Tests;

public class ServiceProgressServiceUnitTest
{
    private static ServiceProgressService CreateSut(Mock<IUnitOfWork> unitOfWork, Mock<ICloudinaryService> cloudinaryService)
    {
        return new ServiceProgressService(unitOfWork.Object, cloudinaryService.Object);
    }

    [Fact]
    public async Task Normal_ReassignCaretakerAsync_ShouldUpdateCaretaker_WhenCaretakerIsEligible()
    {
        var nursery = new Nursery { Id = 12, Name = "Nursery C", IsActive = true };
        var registration = new ServiceRegistration
        {
            Id = 300,
            NurseryCareService = new NurseryCareService
            {
                Id = 40,
                NurseryId = nursery.Id,
                Nursery = nursery,
                CareServicePackage = new CareServicePackage { Id = 70, Name = "Care" }
            },
            ServiceProgresses = new List<ServiceProgress>()
        };

        var newCaretaker = new User
        {
            Id = 900,
            Username = "New caretaker",
            Email = "care@example.com",
            RoleId = (int)RoleEnum.Caretaker,
            Status = (int)UserStatusEnum.Active,
            IsVerified = true,
            NurseryId = nursery.Id,
            StaffSpecializations = new List<StaffSpecialization>()
        };

        var initialProgress = new ServiceProgress
        {
            Id = 500,
            ServiceRegistrationId = registration.Id,
            ServiceRegistration = registration,
            CaretakerId = 800,
            Caretaker = new User { Id = 800, Username = "Old caretaker", Email = "old@example.com" },
            ShiftId = 6,
            TaskDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            Status = (int)ServiceProgressStatusEnum.Assigned,
            Shift = new Shift { Id = 6, ShiftName = "Afternoon", StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(17, 0) }
        };

        var updatedProgress = new ServiceProgress
        {
            Id = initialProgress.Id,
            ServiceRegistrationId = registration.Id,
            ServiceRegistration = registration,
            CaretakerId = newCaretaker.Id,
            Caretaker = newCaretaker,
            ShiftId = initialProgress.ShiftId,
            TaskDate = initialProgress.TaskDate,
            Status = (int)ServiceProgressStatusEnum.Assigned,
            Shift = initialProgress.Shift
        };

        registration.ServiceProgresses.Add(updatedProgress);

        var progressRepository = new Mock<IServiceProgressRepository>(MockBehavior.Loose);
        progressRepository.SetupSequence(r => r.GetByIdWithDetailsAsync(initialProgress.Id))
            .ReturnsAsync(initialProgress)
            .ReturnsAsync(updatedProgress);
        progressRepository.Setup(r => r.GetConflictingCaretakerIdsAsync(initialProgress.ShiftId, It.IsAny<List<DateOnly>>()))
            .ReturnsAsync(new HashSet<int>());
        progressRepository.Setup(r => r.PrepareUpdate(It.IsAny<ServiceProgress>()));

        var registrationRepository = new Mock<IServiceRegistrationRepository>(MockBehavior.Loose);
        registrationRepository.Setup(r => r.GetByIdWithDetailsAsync(registration.Id)).ReturnsAsync(registration);
        registrationRepository.Setup(r => r.PrepareUpdate(It.IsAny<ServiceRegistration>()));

        var nurseryRepository = new Mock<INurseryRepository>(MockBehavior.Loose);
        nurseryRepository.Setup(r => r.GetByManagerIdAsync(1)).ReturnsAsync(nursery);

        var userRepository = new Mock<IUserRepository>(MockBehavior.Loose);
        userRepository.Setup(r => r.GetByIdAsync(newCaretaker.Id)).ReturnsAsync(newCaretaker);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceProgressRepository).Returns(progressRepository.Object);
        unitOfWork.SetupGet(x => x.ServiceRegistrationRepository).Returns(registrationRepository.Object);
        unitOfWork.SetupGet(x => x.NurseryRepository).Returns(nurseryRepository.Object);
        unitOfWork.SetupGet(x => x.UserRepository).Returns(userRepository.Object);
        unitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(unitOfWork, new Mock<ICloudinaryService>(MockBehavior.Loose));

        var result = await sut.ReassignCaretakerAsync(1, initialProgress.Id, newCaretaker.Id);

        result.Caretaker!.Id.Should().Be(newCaretaker.Id);
        result.Status.Should().Be((int)ServiceProgressStatusEnum.Assigned);
        progressRepository.Verify(r => r.PrepareUpdate(It.IsAny<ServiceProgress>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task Normal_SubmitIncidentReportAsync_ShouldStoreIncidentData_WhenUploadSucceeds()
    {
        var nursery = new Nursery { Id = 13, Name = "Nursery D", IsActive = true };
        var progress = new ServiceProgress
        {
            Id = 600,
            CaretakerId = 901,
            Caretaker = new User { Id = 901, Username = "Caretaker", Email = "c@example.com" },
            ServiceRegistrationId = 400,
            ServiceRegistration = new ServiceRegistration
            {
                Id = 400,
                NurseryCareService = new NurseryCareService
                {
                    Id = 50,
                    NurseryId = nursery.Id,
                    Nursery = nursery,
                    CareServicePackage = new CareServicePackage { Id = 80, Name = "Package", ServiceType = (int)CareServiceTypeEnum.OneTime }
                }
            },
            ShiftId = 7,
            Shift = new Shift { Id = 7, ShiftName = "Morning", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0) },
            TaskDate = DateOnly.FromDateTime(DateTime.Today),
            Status = (int)ServiceProgressStatusEnum.InProgress
        };

        var progressRepository = new Mock<IServiceProgressRepository>(MockBehavior.Loose);
        progressRepository.Setup(r => r.GetByIdWithDetailsAsync(progress.Id)).ReturnsAsync(progress);
        progressRepository.Setup(r => r.PrepareUpdate(It.IsAny<ServiceProgress>()));

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceProgressRepository).Returns(progressRepository.Object);
        unitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var cloudinary = new Mock<ICloudinaryService>(MockBehavior.Loose);
        cloudinary.Setup(x => x.ValidateDocumentFile(It.IsAny<IFormFile>(), 10)).Returns((true, string.Empty));
        cloudinary.Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync(new FileUploadResponse { SecureUrl = "https://cdn.example.com/incident.jpg", PublicId = "incident-1" });

        var sut = CreateSut(unitOfWork, cloudinary);

        var file = new Mock<IFormFile>().Object;
        var request = new SubmitIncidentReportRequestDto { IncidentReason = "  Broken pot  " };

        var result = await sut.SubmitIncidentReportAsync(901, progress.Id, request, file);

        result.HasIncidents.Should().BeTrue();
        result.IncidentReason.Should().Be("Broken pot");
        result.IncidentImageUrl.Should().Be("https://cdn.example.com/incident.jpg");
        progressRepository.Verify(r => r.PrepareUpdate(progress), Times.Once);
    }

    [Fact]
    public async Task Normal_SubmitIncidentReportAsync_ShouldTrimIncidentReason()
    {
        var progress = new ServiceProgress
        {
            Id = 601,
            CaretakerId = 901,
            Status = (int)ServiceProgressStatusEnum.InProgress,
            ServiceRegistration = new ServiceRegistration
            {
                NurseryCareService = new NurseryCareService
                {
                    Nursery = new Nursery { Id = 1, Name = "N" },
                    CareServicePackage = new CareServicePackage { Id = 1, Name = "P", ServiceType = (int)CareServiceTypeEnum.OneTime }
                }
            },
            Shift = new Shift { Id = 1, ShiftName = "M", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(10, 0) }
        };

        var progressRepository = new Mock<IServiceProgressRepository>(MockBehavior.Loose);
        progressRepository.Setup(r => r.GetByIdWithDetailsAsync(601)).ReturnsAsync(progress);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceProgressRepository).Returns(progressRepository.Object);
        unitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var cloudinary = new Mock<ICloudinaryService>(MockBehavior.Loose);
        cloudinary.Setup(x => x.ValidateDocumentFile(It.IsAny<IFormFile>(), 10)).Returns((true, string.Empty));
        cloudinary.Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync(new FileUploadResponse { SecureUrl = "https://cdn.example.com/trim.jpg" });

        var sut = CreateSut(unitOfWork, cloudinary);
        var result = await sut.SubmitIncidentReportAsync(901, 601, new SubmitIncidentReportRequestDto { IncidentReason = "   Need support   " }, new Mock<IFormFile>().Object);

        result.IncidentReason.Should().Be("Need support");
    }

    [Fact]
    public async Task Boundary_ReassignCaretakerAsync_ShouldSkipConflictCheck_WhenTaskDateIsNull()
    {
        var nursery = new Nursery { Id = 20, IsActive = true };
        var registration = new ServiceRegistration
        {
            Id = 700,
            NurseryCareService = new NurseryCareService { Id = 201, NurseryId = 20, Nursery = nursery, CareServicePackage = new CareServicePackage { Id = 2, Name = "P" } },
            ServiceProgresses = new List<ServiceProgress>()
        };
        var progress = new ServiceProgress
        {
            Id = 710,
            ServiceRegistrationId = registration.Id,
            ServiceRegistration = registration,
            ShiftId = 3,
            TaskDate = null,
            Status = (int)ServiceProgressStatusEnum.Assigned,
            Shift = new Shift { Id = 3, ShiftName = "S", StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }
        };
        var newCaretaker = new User
        {
            Id = 902,
            RoleId = (int)RoleEnum.Caretaker,
            Status = (int)UserStatusEnum.Active,
            IsVerified = true,
            NurseryId = nursery.Id,
            StaffSpecializations = new List<StaffSpecialization>(),
            Username = "C",
            Email = "c@x.com"
        };

        var progressRepository = new Mock<IServiceProgressRepository>(MockBehavior.Loose);
        progressRepository.SetupSequence(r => r.GetByIdWithDetailsAsync(710))
            .ReturnsAsync(progress)
            .ReturnsAsync(new ServiceProgress
            {
                Id = 710,
                ServiceRegistrationId = registration.Id,
                ServiceRegistration = registration,
                CaretakerId = 902,
                Caretaker = newCaretaker,
                Status = (int)ServiceProgressStatusEnum.Assigned,
                ShiftId = 3,
                Shift = progress.Shift
            });

        var registrationRepository = new Mock<IServiceRegistrationRepository>(MockBehavior.Loose);
        registrationRepository.Setup(r => r.GetByIdWithDetailsAsync(registration.Id)).ReturnsAsync(registration);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceProgressRepository).Returns(progressRepository.Object);
        unitOfWork.SetupGet(x => x.ServiceRegistrationRepository).Returns(registrationRepository.Object);
        unitOfWork.SetupGet(x => x.NurseryRepository).Returns(Mock.Of<INurseryRepository>(r => r.GetByManagerIdAsync(1) == Task.FromResult<Nursery?>(nursery)));
        unitOfWork.SetupGet(x => x.UserRepository).Returns(Mock.Of<IUserRepository>(r => r.GetByIdAsync(902) == Task.FromResult<User?>(newCaretaker)));
        unitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(unitOfWork, new Mock<ICloudinaryService>(MockBehavior.Loose));
        var result = await sut.ReassignCaretakerAsync(1, 710, 902);

        result.Caretaker!.Id.Should().Be(902);
        progressRepository.Verify(r => r.GetConflictingCaretakerIdsAsync(It.IsAny<int>(), It.IsAny<List<DateOnly>>()), Times.Never);
    }

    [Fact]
    public async Task Boundary_SubmitIncidentReportAsync_ShouldThrow_WhenIncidentReasonIsWhitespace()
    {
        var progressRepository = new Mock<IServiceProgressRepository>(MockBehavior.Loose);
        progressRepository.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(new ServiceProgress { Id = 1, CaretakerId = 1, Status = 1 });

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceProgressRepository).Returns(progressRepository.Object);

        var sut = CreateSut(unitOfWork, new Mock<ICloudinaryService>(MockBehavior.Loose));

        var act = () => sut.SubmitIncidentReportAsync(1, 1, new SubmitIncidentReportRequestDto { IncidentReason = "   " }, new Mock<IFormFile>().Object);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("IncidentReason is required");
    }

    [Fact]
    public async Task Abnormal_ReassignCaretakerAsync_ShouldThrow_WhenSelectedUserIsNotCaretaker()
    {
        var nursery = new Nursery { Id = 30, IsActive = true };
        var progress = new ServiceProgress
        {
            Id = 720,
            ServiceRegistration = new ServiceRegistration
            {
                NurseryCareService = new NurseryCareService { NurseryId = nursery.Id, Nursery = nursery, CareServicePackage = new CareServicePackage { Id = 1, Name = "P" } }
            },
            Status = (int)ServiceProgressStatusEnum.Assigned,
            ShiftId = 1,
            TaskDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            Shift = new Shift { Id = 1, ShiftName = "M", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(10, 0) }
        };

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.NurseryRepository).Returns(Mock.Of<INurseryRepository>(r => r.GetByManagerIdAsync(1) == Task.FromResult<Nursery?>(nursery)));
        unitOfWork.SetupGet(x => x.ServiceProgressRepository).Returns(Mock.Of<IServiceProgressRepository>(r => r.GetByIdWithDetailsAsync(720) == Task.FromResult<ServiceProgress?>(progress)));
        unitOfWork.SetupGet(x => x.UserRepository).Returns(Mock.Of<IUserRepository>(r => r.GetByIdAsync(999) == Task.FromResult<User?>(new User { Id = 999, RoleId = (int)RoleEnum.Staff })));

        var sut = CreateSut(unitOfWork, new Mock<ICloudinaryService>(MockBehavior.Loose));
        var act = () => sut.ReassignCaretakerAsync(1, 720, 999);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Selected user is not a caretaker");
    }

    [Fact]
    public async Task Abnormal_SubmitIncidentReportAsync_ShouldThrow_WhenProgressIsCancelled()
    {
        var progressRepository = new Mock<IServiceProgressRepository>(MockBehavior.Loose);
        progressRepository.Setup(r => r.GetByIdWithDetailsAsync(2)).ReturnsAsync(new ServiceProgress
        {
            Id = 2,
            CaretakerId = 1,
            Status = (int)ServiceProgressStatusEnum.Cancelled
        });

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceProgressRepository).Returns(progressRepository.Object);

        var sut = CreateSut(unitOfWork, new Mock<ICloudinaryService>(MockBehavior.Loose));
        var act = () => sut.SubmitIncidentReportAsync(1, 2, new SubmitIncidentReportRequestDto { IncidentReason = "x" }, new Mock<IFormFile>().Object);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Cannot submit incident report for a cancelled task");
    }

    [Fact]
    public async Task SubmitIncidentReportAsync_ShouldThrowNotFound_WhenProgressDoesNotExist()
    {
        var progressRepository = new Mock<IServiceProgressRepository>(MockBehavior.Loose);
        progressRepository.Setup(r => r.GetByIdWithDetailsAsync(404)).ReturnsAsync((ServiceProgress?)null);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceProgressRepository).Returns(progressRepository.Object);

        var sut = CreateSut(unitOfWork, new Mock<ICloudinaryService>(MockBehavior.Loose));
        var act = () => sut.SubmitIncidentReportAsync(1, 404, new SubmitIncidentReportRequestDto { IncidentReason = "x" }, new Mock<IFormFile>().Object);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("ServiceProgress 404 not found");
    }

    [Fact]
    public async Task SubmitIncidentReportAsync_ShouldThrowBadRequest_WhenUploadedFileInvalid()
    {
        var progressRepository = new Mock<IServiceProgressRepository>(MockBehavior.Loose);
        progressRepository.Setup(r => r.GetByIdWithDetailsAsync(405)).ReturnsAsync(new ServiceProgress
        {
            Id = 405,
            CaretakerId = 2,
            Status = (int)ServiceProgressStatusEnum.InProgress
        });

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.ServiceProgressRepository).Returns(progressRepository.Object);

        var cloudinary = new Mock<ICloudinaryService>(MockBehavior.Loose);
        cloudinary.Setup(c => c.ValidateDocumentFile(It.IsAny<IFormFile>(), 10)).Returns((false, "Invalid file type"));

        var sut = CreateSut(unitOfWork, cloudinary);
        var act = () => sut.SubmitIncidentReportAsync(2, 405, new SubmitIncidentReportRequestDto { IncidentReason = "x" }, new Mock<IFormFile>().Object);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Invalid file type");
    }

    [Fact]
    public async Task ReassignCaretakerAsync_ShouldThrowBadRequest_WhenScheduleConflictExists()
    {
        var nursery = new Nursery { Id = 40, IsActive = true };
        var progress = new ServiceProgress
        {
            Id = 730,
            ShiftId = 5,
            TaskDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            Status = (int)ServiceProgressStatusEnum.Assigned,
            ServiceRegistration = new ServiceRegistration
            {
                NurseryCareService = new NurseryCareService
                {
                    NurseryId = nursery.Id,
                    Nursery = nursery,
                    CareServicePackage = new CareServicePackage { Id = 1, Name = "pkg" }
                }
            },
            Shift = new Shift { Id = 5, ShiftName = "S", StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }
        };

        var newCaretaker = new User
        {
            Id = 910,
            RoleId = (int)RoleEnum.Caretaker,
            Status = (int)UserStatusEnum.Active,
            IsVerified = true,
            NurseryId = nursery.Id
        };

        var progressRepo = new Mock<IServiceProgressRepository>(MockBehavior.Loose);
        progressRepo.Setup(r => r.GetByIdWithDetailsAsync(730)).ReturnsAsync(progress);
        progressRepo.Setup(r => r.GetConflictingCaretakerIdsAsync(5, It.IsAny<List<DateOnly>>())).ReturnsAsync(new HashSet<int> { 910 });

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.NurseryRepository).Returns(Mock.Of<INurseryRepository>(r => r.GetByManagerIdAsync(1) == Task.FromResult<Nursery?>(nursery)));
        unitOfWork.SetupGet(x => x.ServiceProgressRepository).Returns(progressRepo.Object);
        unitOfWork.SetupGet(x => x.UserRepository).Returns(Mock.Of<IUserRepository>(r => r.GetByIdAsync(910) == Task.FromResult<User?>(newCaretaker)));

        var sut = CreateSut(unitOfWork, new Mock<ICloudinaryService>(MockBehavior.Loose));
        var act = () => sut.ReassignCaretakerAsync(1, 730, 910);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Caretaker has a schedule conflict on this session's shift and date");
    }
}