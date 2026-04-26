using FluentAssertions;
using Moq;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Services;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Interfaces;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.Tests;

public class NurseryServiceUnitTest
{
    private static NurseryService CreateSut(Mock<IUnitOfWork> uow, Mock<ICacheService> cache)
        => new(uow.Object, cache.Object);

    private static NurseryRequestDto CreateValidCreateRequest(string name = "Nursery A")
        => new()
        {
            Name = name,
            Address = "Addr",
            Area = 10,
            Latitude = 10,
            Longitude = 20,
            Phone = "0912345678",
            IsActive = true
        };

    private static Nursery CreateNurseryEntity(int id, string name)
        => new()
        {
            Id = id,
            Name = name,
            Address = "Addr",
            Phone = "0912345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

    private static Mock<ICacheService> CreateCacheMock()
    {
        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        cache.Setup(c => c.RemoveDataAsync(It.IsAny<string>())).ReturnsAsync(new object());
        cache.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        return cache;
    }

    // ----------------------------
    // CreateNurseryAsync (3 normal, 2 boundary, 1 abnormal)
    // ----------------------------

    [Fact]
    public async Task CreateNurseryAsync_ShouldCreateAndCommit_AndInvalidateCache_Normal()
    {
        var request = CreateValidCreateRequest("Nursery A");
        var createdEntity = CreateNurseryEntity(1, request.Name);

        var nurseryRepo = new Mock<INurseryRepository>(MockBehavior.Strict);
        nurseryRepo.Setup(r => r.ExistsByNameAsync(request.Name, null)).ReturnsAsync(false);
        nurseryRepo.Setup(r => r.PrepareCreate(It.IsAny<Nursery>()))
            .Callback<Nursery>(n => n.Id = 1);
        nurseryRepo.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(createdEntity);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.NurseryRepository).Returns(nurseryRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var cache = CreateCacheMock();
        var sut = CreateSut(uow, cache);

        var result = await sut.CreateNurseryAsync(request);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be(request.Name);

        uow.Verify(x => x.BeginTransactionAsync(), Times.Once);
        uow.Verify(x => x.CommitTransactionAsync(), Times.Once);
        uow.Verify(x => x.RollbackTransactionAsync(), Times.Never);
        uow.Verify(x => x.SaveAsync(), Times.Once);
        nurseryRepo.Verify(r => r.PrepareCreate(It.IsAny<Nursery>()), Times.Once);
        cache.Verify(c => c.RemoveByPrefixAsync("nurseries_all_"), Times.Once);
        cache.Verify(c => c.RemoveByPrefixAsync("nurseries_active_"), Times.Once);
    }

    [Fact]
    public async Task CreateNurseryAsync_ShouldReloadWithDetails_Normal()
    {
        var request = CreateValidCreateRequest("Nursery B");
        var createdEntity = CreateNurseryEntity(2, request.Name);

        var nurseryRepo = new Mock<INurseryRepository>(MockBehavior.Strict);
        nurseryRepo.Setup(r => r.ExistsByNameAsync(request.Name, null)).ReturnsAsync(false);
        nurseryRepo.Setup(r => r.PrepareCreate(It.IsAny<Nursery>()))
            .Callback<Nursery>(n => n.Id = 2);
        nurseryRepo.Setup(r => r.GetByIdWithDetailsAsync(2)).ReturnsAsync(createdEntity);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.NurseryRepository).Returns(nurseryRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, CreateCacheMock());

        var result = await sut.CreateNurseryAsync(request);

        nurseryRepo.Verify(r => r.GetByIdWithDetailsAsync(2), Times.Once);
        result.Id.Should().Be(2);
    }

    [Fact]
    public async Task CreateNurseryAsync_ShouldPersistIsActive_Normal()
    {
        var request = CreateValidCreateRequest("Nursery C");
        request.IsActive = false;

        Nursery? prepared = null;

        var nurseryRepo = new Mock<INurseryRepository>(MockBehavior.Strict);
        nurseryRepo.Setup(r => r.ExistsByNameAsync(request.Name, null)).ReturnsAsync(false);
        nurseryRepo.Setup(r => r.PrepareCreate(It.IsAny<Nursery>()))
            .Callback<Nursery>(n =>
            {
                n.Id = 3;
                prepared = n;
            });
        nurseryRepo.Setup(r => r.GetByIdWithDetailsAsync(3)).ReturnsAsync(CreateNurseryEntity(3, request.Name));

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.NurseryRepository).Returns(nurseryRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, CreateCacheMock());

        var _ = await sut.CreateNurseryAsync(request);

        prepared.Should().NotBeNull();
        prepared!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreateNurseryAsync_ShouldAllowNameLength200_Boundary()
    {
        var name200 = new string('a', 200);
        var request = CreateValidCreateRequest(name200);

        var nurseryRepo = new Mock<INurseryRepository>(MockBehavior.Strict);
        nurseryRepo.Setup(r => r.ExistsByNameAsync(name200, null)).ReturnsAsync(false);
        nurseryRepo.Setup(r => r.PrepareCreate(It.IsAny<Nursery>()))
            .Callback<Nursery>(n => n.Id = 4);
        nurseryRepo.Setup(r => r.GetByIdWithDetailsAsync(4)).ReturnsAsync(CreateNurseryEntity(4, name200));

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.NurseryRepository).Returns(nurseryRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, CreateCacheMock());

        var result = await sut.CreateNurseryAsync(request);

        result.Name.Should().Be(name200);
    }

    [Fact]
    public async Task CreateNurseryAsync_ShouldAllowAreaZero_Boundary()
    {
        var request = CreateValidCreateRequest("Nursery Area0");
        request.Area = 0;

        var nurseryRepo = new Mock<INurseryRepository>(MockBehavior.Strict);
        nurseryRepo.Setup(r => r.ExistsByNameAsync(request.Name, null)).ReturnsAsync(false);
        nurseryRepo.Setup(r => r.PrepareCreate(It.IsAny<Nursery>()))
            .Callback<Nursery>(n => n.Id = 5);
        nurseryRepo.Setup(r => r.GetByIdWithDetailsAsync(5)).ReturnsAsync(CreateNurseryEntity(5, request.Name));

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.NurseryRepository).Returns(nurseryRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, CreateCacheMock());

        var result = await sut.CreateNurseryAsync(request);

        result.Id.Should().Be(5);
    }

    [Fact]
    public async Task CreateNurseryAsync_ShouldRollback_WhenDuplicateName_Abnormal()
    {
        var request = CreateValidCreateRequest("DupName");

        var nurseryRepo = new Mock<INurseryRepository>(MockBehavior.Strict);
        nurseryRepo.Setup(r => r.ExistsByNameAsync(request.Name, null)).ReturnsAsync(true);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.NurseryRepository).Returns(nurseryRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);

        var sut = CreateSut(uow, CreateCacheMock());

        var act = () => sut.CreateNurseryAsync(request);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Vựa với tên 'DupName' đã tồn tại");

        uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        uow.Verify(x => x.CommitTransactionAsync(), Times.Never);
    }

    // ----------------------------
    // UpdateNurseryAsync (3 normal, 2 boundary, 1 abnormal)
    // ----------------------------

    [Fact]
    public async Task UpdateNurseryAsync_ShouldUpdateAndCommit_AndInvalidateCache_Normal()
    {
        const int nurseryId = 10;
        var entity = CreateNurseryEntity(nurseryId, "Old");

        var request = new NurseryUpdateDto
        {
            Name = "New",
            Address = "NewAddr",
            Phone = "0987654321",
            IsActive = false
        };

        var nurseryRepo = new Mock<INurseryRepository>(MockBehavior.Strict);
        nurseryRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryId)).ReturnsAsync(entity);
        nurseryRepo.Setup(r => r.ExistsByNameAsync("New", nurseryId)).ReturnsAsync(false);
        nurseryRepo.Setup(r => r.PrepareUpdate(entity));

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.NurseryRepository).Returns(nurseryRepo.Object);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var cache = CreateCacheMock();
        var sut = CreateSut(uow, cache);

        var result = await sut.UpdateNurseryAsync(nurseryId, request);

        result.Id.Should().Be(nurseryId);
        result.Name.Should().Be("New");
        result.Phone.Should().Be("0987654321");
        result.IsActive.Should().BeFalse();

        uow.Verify(x => x.CommitTransactionAsync(), Times.Once);
        uow.Verify(x => x.SaveAsync(), Times.Once);
        cache.Verify(c => c.RemoveByPrefixAsync("nurseries_all_"), Times.Once);
    }

    [Fact]
    public async Task UpdateNurseryAsync_ShouldValidateManagerId_WhenProvided_Normal()
    {
        const int nurseryId = 11;
        var entity = CreateNurseryEntity(nurseryId, "Nursery");

        var request = new NurseryUpdateDto { ManagerId = 99 };

        var nurseryRepo = new Mock<INurseryRepository>(MockBehavior.Strict);
        nurseryRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryId)).ReturnsAsync(entity);
        nurseryRepo.Setup(r => r.PrepareUpdate(entity));

        var manager = new User { Id = 99, RoleId = (int)RoleEnum.Manager };

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(manager);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.NurseryRepository).Returns(nurseryRepo.Object);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, CreateCacheMock());

        var _ = await sut.UpdateNurseryAsync(nurseryId, request);

        userRepo.Verify(r => r.GetByIdAsync(99), Times.Once);
    }

    [Fact]
    public async Task UpdateNurseryAsync_ShouldNotCheckDuplicate_WhenNameNull_Normal()
    {
        const int nurseryId = 12;
        var entity = CreateNurseryEntity(nurseryId, "Nursery");

        var request = new NurseryUpdateDto { Address = "OnlyAddr" };

        var nurseryRepo = new Mock<INurseryRepository>(MockBehavior.Strict);
        nurseryRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryId)).ReturnsAsync(entity);
        nurseryRepo.Setup(r => r.PrepareUpdate(entity));

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.NurseryRepository).Returns(nurseryRepo.Object);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, CreateCacheMock());

        var _ = await sut.UpdateNurseryAsync(nurseryId, request);

        nurseryRepo.Verify(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task UpdateNurseryAsync_ShouldAllowNameLength200_Boundary()
    {
        const int nurseryId = 13;
        var entity = CreateNurseryEntity(nurseryId, "Old");

        var name200 = new string('b', 200);
        var request = new NurseryUpdateDto { Name = name200 };

        var nurseryRepo = new Mock<INurseryRepository>(MockBehavior.Strict);
        nurseryRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryId)).ReturnsAsync(entity);
        nurseryRepo.Setup(r => r.ExistsByNameAsync(name200, nurseryId)).ReturnsAsync(false);
        nurseryRepo.Setup(r => r.PrepareUpdate(entity));

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.NurseryRepository).Returns(nurseryRepo.Object);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, CreateCacheMock());

        var result = await sut.UpdateNurseryAsync(nurseryId, request);

        result.Name.Should().Be(name200);
    }

    [Fact]
    public async Task UpdateNurseryAsync_ShouldAllowManagerIdNull_Boundary()
    {
        const int nurseryId = 14;
        var entity = CreateNurseryEntity(nurseryId, "Old");

        var request = new NurseryUpdateDto { ManagerId = null, Address = "x" };

        var nurseryRepo = new Mock<INurseryRepository>(MockBehavior.Strict);
        nurseryRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryId)).ReturnsAsync(entity);
        nurseryRepo.Setup(r => r.PrepareUpdate(entity));

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.NurseryRepository).Returns(nurseryRepo.Object);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, CreateCacheMock());

        var _ = await sut.UpdateNurseryAsync(nurseryId, request);

        userRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateNurseryAsync_ShouldRollback_WhenManagerIsNotManager_Abnormal()
    {
        const int nurseryId = 15;
        var entity = CreateNurseryEntity(nurseryId, "Old");

        var request = new NurseryUpdateDto { ManagerId = 123 };

        var nurseryRepo = new Mock<INurseryRepository>(MockBehavior.Strict);
        nurseryRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryId)).ReturnsAsync(entity);

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(123)).ReturnsAsync(new User { Id = 123, RoleId = (int)RoleEnum.Customer });

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.NurseryRepository).Returns(nurseryRepo.Object);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);

        var sut = CreateSut(uow, CreateCacheMock());

        var act = () => sut.UpdateNurseryAsync(nurseryId, request);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Selected user is not a Manager");

        uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        uow.Verify(x => x.CommitTransactionAsync(), Times.Never);
    }
}

