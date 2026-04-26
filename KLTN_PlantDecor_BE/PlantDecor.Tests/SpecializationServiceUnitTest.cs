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

public class SpecializationServiceUnitTest
{
    private static SpecializationService CreateSut(Mock<IUnitOfWork> unitOfWork)
    {
        return new SpecializationService(unitOfWork.Object);
    }

    [Fact]
    public async Task Normal_CreateAsync_ShouldCreateSpecialization_WhenNameIsUnique()
    {
        var specialization = new Specialization
        {
            Id = 77,
            Name = "Pruning",
            Description = "Plant pruning",
            IsActive = true
        };

        var specializationRepository = new Mock<ISpecializationRepository>(MockBehavior.Loose);
        specializationRepository.Setup(r => r.ExistsByNameAsync("Pruning", null)).ReturnsAsync(false);
        specializationRepository.Setup(r => r.PrepareCreate(It.IsAny<Specialization>()))
            .Callback<Specialization>(entity => entity.Id = specialization.Id);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.SpecializationRepository).Returns(specializationRepository.Object);
        unitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(unitOfWork);

        var result = await sut.CreateAsync(new SpecializationRequestDto
        {
            Name = "Pruning",
            Description = "Plant pruning"
        });

        result.Id.Should().Be(specialization.Id);
        result.Name.Should().Be("Pruning");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Normal_AssignToStaffAsync_ShouldAssignSpecialization_WhenStaffBelongsToNursery()
    {
        var nursery = new Nursery { Id = 15, Name = "Nursery E", IsActive = true };
        var specialization = new Specialization
        {
            Id = 5,
            Name = "Hydroponics",
            Description = "Hydroponic care",
            IsActive = true
        };
        var staff = new User
        {
            Id = 200,
            Username = "Staff A",
            Email = "staff@example.com",
            RoleId = (int)RoleEnum.Caretaker,
            NurseryId = nursery.Id,
            StaffSpecializations = new List<StaffSpecialization>()
        };
        var updatedStaff = new User
        {
            Id = staff.Id,
            Username = staff.Username,
            Email = staff.Email,
            RoleId = staff.RoleId,
            NurseryId = staff.NurseryId,
            StaffSpecializations = new List<StaffSpecialization>
            {
                new StaffSpecialization
                {
                    StaffId = staff.Id,
                    SpecializationId = specialization.Id,
                    Specialization = specialization
                }
            }
        };

        var nurseryRepository = new Mock<INurseryRepository>(MockBehavior.Loose);
        nurseryRepository.Setup(r => r.GetByManagerIdAsync(1)).ReturnsAsync(nursery);

        var userRepository = new Mock<IUserRepository>(MockBehavior.Loose);
        userRepository.SetupSequence(r => r.GetCaretakerByIdWithSpecializationsAsync(staff.Id, nursery.Id))
            .ReturnsAsync(staff)
            .ReturnsAsync(updatedStaff);

        var specializationRepository = new Mock<ISpecializationRepository>(MockBehavior.Loose);
        specializationRepository.Setup(r => r.GetByIdAsync(specialization.Id)).ReturnsAsync(specialization);
        specializationRepository.Setup(r => r.GetStaffSpecializationAsync(staff.Id, specialization.Id)).ReturnsAsync((StaffSpecialization?)null);
        specializationRepository.Setup(r => r.AddStaffSpecializationAsync(It.IsAny<StaffSpecialization>())).Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.NurseryRepository).Returns(nurseryRepository.Object);
        unitOfWork.SetupGet(x => x.UserRepository).Returns(userRepository.Object);
        unitOfWork.SetupGet(x => x.SpecializationRepository).Returns(specializationRepository.Object);

        var sut = CreateSut(unitOfWork);

        var result = await sut.AssignToStaffAsync(1, staff.Id, specialization.Id);

        result.Id.Should().Be(staff.Id);
        result.Specializations.Should().ContainSingle(s => s.Id == specialization.Id && s.Name == specialization.Name);
    }

    [Fact]
    public async Task Normal_CreateAsync_ShouldCreateSpecialization_WhenDescriptionProvided()
    {
        var specializationRepository = new Mock<ISpecializationRepository>(MockBehavior.Loose);
        specializationRepository.Setup(r => r.ExistsByNameAsync("Irrigation", null)).ReturnsAsync(false);
        specializationRepository.Setup(r => r.PrepareCreate(It.IsAny<Specialization>())).Callback<Specialization>(x => x.Id = 88);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.SpecializationRepository).Returns(specializationRepository.Object);
        unitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var result = await CreateSut(unitOfWork).CreateAsync(new SpecializationRequestDto { Name = "Irrigation", Description = "Watering systems" });
        result.Id.Should().Be(88);
        result.Description.Should().Be("Watering systems");
    }

    [Fact]
    public async Task Boundary_CreateAsync_ShouldSetEmptyDescription_WhenDescriptionIsNull()
    {
        var specializationRepository = new Mock<ISpecializationRepository>(MockBehavior.Loose);
        specializationRepository.Setup(r => r.ExistsByNameAsync("NoDesc", null)).ReturnsAsync(false);

        Specialization? captured = null;
        specializationRepository.Setup(r => r.PrepareCreate(It.IsAny<Specialization>()))
            .Callback<Specialization>(x =>
            {
                captured = x;
                x.Id = 89;
            });

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.SpecializationRepository).Returns(specializationRepository.Object);
        unitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var result = await CreateSut(unitOfWork).CreateAsync(new SpecializationRequestDto { Name = "NoDesc", Description = null });

        result.Id.Should().Be(89);
        captured!.Description.Should().BeEmpty();
    }

    [Fact]
    public async Task Boundary_AssignToStaffAsync_ShouldAssign_WhenStaffHasNoSpecializationsInitially()
    {
        var nursery = new Nursery { Id = 16, IsActive = true };
        var staff = new User
        {
            Id = 210,
            Username = "Staff B",
            Email = "staffb@example.com",
            StaffSpecializations = new List<StaffSpecialization>()
        };

        var specialized = new Specialization { Id = 6, Name = "Pest Control", Description = "Pest", IsActive = true };
        var updatedStaff = new User
        {
            Id = staff.Id,
            Username = staff.Username,
            Email = staff.Email,
            StaffSpecializations = new List<StaffSpecialization>
            {
                new StaffSpecialization { StaffId = staff.Id, SpecializationId = specialized.Id, Specialization = specialized }
            }
        };

        var nurseryRepository = new Mock<INurseryRepository>(MockBehavior.Loose);
        nurseryRepository.Setup(r => r.GetByManagerIdAsync(2)).ReturnsAsync(nursery);

        var userRepository = new Mock<IUserRepository>(MockBehavior.Loose);
        userRepository.SetupSequence(r => r.GetCaretakerByIdWithSpecializationsAsync(210, 16))
            .ReturnsAsync(staff)
            .ReturnsAsync(updatedStaff);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.NurseryRepository).Returns(nurseryRepository.Object);
        unitOfWork.SetupGet(x => x.UserRepository).Returns(userRepository.Object);

        var specRepo = new Mock<ISpecializationRepository>(MockBehavior.Loose);
        specRepo.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(specialized);
        specRepo.Setup(r => r.GetStaffSpecializationAsync(210, 6)).ReturnsAsync((StaffSpecialization?)null);
        specRepo.Setup(r => r.AddStaffSpecializationAsync(It.IsAny<StaffSpecialization>())).Returns(Task.CompletedTask);
        unitOfWork.SetupGet(x => x.SpecializationRepository).Returns(specRepo.Object);

        var result = await CreateSut(unitOfWork).AssignToStaffAsync(2, 210, 6);
        result.Specializations.Should().ContainSingle();
    }

    [Fact]
    public async Task Abnormal_CreateAsync_ShouldThrow_WhenNameAlreadyExists()
    {
        var specRepo = new Mock<ISpecializationRepository>(MockBehavior.Loose);
        specRepo.Setup(r => r.ExistsByNameAsync("Pruning", null)).ReturnsAsync(true);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Loose);
        uow.SetupGet(x => x.SpecializationRepository).Returns(specRepo.Object);

        var act = () => CreateSut(uow).CreateAsync(new SpecializationRequestDto { Name = "Pruning" });
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("A specialization named 'Pruning' already exists");
    }

    [Fact]
    public async Task Abnormal_AssignToStaffAsync_ShouldThrow_WhenManagerHasNoNursery()
    {
        var uow = new Mock<IUnitOfWork>(MockBehavior.Loose);
        uow.SetupGet(x => x.NurseryRepository).Returns(Mock.Of<INurseryRepository>(r => r.GetByManagerIdAsync(99) == Task.FromResult<Nursery?>(null)));

        var act = () => CreateSut(uow).AssignToStaffAsync(99, 1, 1);
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You are not a manager of any nursery");
    }

    [Fact]
    public async Task AssignToStaffAsync_ShouldThrowNotFound_WhenStaffNotInNursery()
    {
        var nurseryRepository = new Mock<INurseryRepository>(MockBehavior.Loose);
        nurseryRepository.Setup(r => r.GetByManagerIdAsync(1)).ReturnsAsync(new Nursery { Id = 20, IsActive = true });

        var userRepository = new Mock<IUserRepository>(MockBehavior.Loose);
        userRepository.Setup(r => r.GetCaretakerByIdWithSpecializationsAsync(300, 20)).ReturnsAsync((User?)null);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.NurseryRepository).Returns(nurseryRepository.Object);
        unitOfWork.SetupGet(x => x.UserRepository).Returns(userRepository.Object);

        var act = () => CreateSut(unitOfWork).AssignToStaffAsync(1, 300, 5);
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Caretaker 300 not found in your nursery");
    }

    [Fact]
    public async Task AssignToStaffAsync_ShouldThrowBadRequest_WhenSpecializationInactive()
    {
        var nursery = new Nursery { Id = 21, IsActive = true };
        var staff = new User { Id = 301, Username = "s", Email = "s@x.com", StaffSpecializations = new List<StaffSpecialization>() };
        var spec = new Specialization { Id = 6, Name = "Inactive", IsActive = false };

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.NurseryRepository).Returns(Mock.Of<INurseryRepository>(r => r.GetByManagerIdAsync(1) == Task.FromResult<Nursery?>(nursery)));
        unitOfWork.SetupGet(x => x.UserRepository).Returns(Mock.Of<IUserRepository>(r => r.GetCaretakerByIdWithSpecializationsAsync(301, 21) == Task.FromResult<User?>(staff)));

        var specRepo = new Mock<ISpecializationRepository>(MockBehavior.Loose);
        specRepo.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(spec);
        unitOfWork.SetupGet(x => x.SpecializationRepository).Returns(specRepo.Object);

        var act = () => CreateSut(unitOfWork).AssignToStaffAsync(1, 301, 6);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Specialization is not active");
    }

    [Fact]
    public async Task AssignToStaffAsync_ShouldThrowBadRequest_WhenAssignmentAlreadyExists()
    {
        var nursery = new Nursery { Id = 22, IsActive = true };
        var staff = new User { Id = 302, Username = "s2", Email = "s2@x.com", StaffSpecializations = new List<StaffSpecialization>() };
        var spec = new Specialization { Id = 7, Name = "Pest", IsActive = true };

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.NurseryRepository).Returns(Mock.Of<INurseryRepository>(r => r.GetByManagerIdAsync(1) == Task.FromResult<Nursery?>(nursery)));
        unitOfWork.SetupGet(x => x.UserRepository).Returns(Mock.Of<IUserRepository>(r => r.GetCaretakerByIdWithSpecializationsAsync(302, 22) == Task.FromResult<User?>(staff)));

        var specRepo = new Mock<ISpecializationRepository>(MockBehavior.Loose);
        specRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(spec);
        specRepo.Setup(r => r.GetStaffSpecializationAsync(302, 7)).ReturnsAsync(new StaffSpecialization { StaffId = 302, SpecializationId = 7 });
        unitOfWork.SetupGet(x => x.SpecializationRepository).Returns(specRepo.Object);

        var act = () => CreateSut(unitOfWork).AssignToStaffAsync(1, 302, 7);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Staff already has this specialization");
    }
}