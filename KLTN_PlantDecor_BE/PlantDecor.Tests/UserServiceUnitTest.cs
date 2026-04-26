using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Services;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.Tests;

public class UserServiceUnitTest
{
    private static UserService CreateSut(Mock<IUnitOfWork> uow)
    {
        var cloudinary = new Mock<ICloudinaryService>(MockBehavior.Strict);
        var stampCache = new Mock<ISecurityStampCacheService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<UserService>>(MockBehavior.Loose);
        return new UserService(uow.Object, cloudinary.Object, stampCache.Object, logger.Object);
    }

    private static UserUpdateDto CreateValidUpdate(string? phone = "0123456789")
        => new()
        {
            UserName = "u",
            FullName = "Full Name",
            PhoneNumber = phone,
            Address = "addr"
        };

    private static User CreateUser(int id, string? phone = "0123456789")
        => new()
        {
            Id = id,
            Email = "a@b.com",
            Username = "old",
            PhoneNumber = phone,
            PasswordHash = "hash",
            Status = (int)UserStatusEnum.Active,
            RoleId = (int)RoleEnum.Customer,
            UserProfile = new UserProfile { UserId = id, FullName = "Old Name" }
        };

    [Fact]
    public async Task UpdateUserInfoAsync_ShouldUpdateAndCommit_WhenValid_Normal()
    {
        const int userId = 1;
        var dto = CreateValidUpdate(phone: "0123456789");
        var user = CreateUser(userId, phone: "0123456789");

        var userRepo = new Mock<PlantDecor.DataAccessLayer.Interfaces.IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        userRepo.Setup(r => r.UpdateAsync(user)).ReturnsAsync(1);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);

        var sut = CreateSut(uow);

        var result = await sut.UpdateUserInfoAsync(userId, dto);

        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.FullName.Should().Be("Full Name");

        uow.Verify(x => x.BeginTransactionAsync(), Times.Once);
        uow.Verify(x => x.CommitTransactionAsync(), Times.Once);
        uow.Verify(x => x.RollbackTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateUserInfoAsync_ShouldUpdatePhone_WhenPhoneChangedAndNotUsed_Normal()
    {
        const int userId = 2;
        var dto = CreateValidUpdate(phone: "0987654321");
        var user = CreateUser(userId, phone: "0123456789");

        var userRepo = new Mock<PlantDecor.DataAccessLayer.Interfaces.IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        userRepo.Setup(r => r.GetByPhoneAsync("0987654321")).ReturnsAsync((User?)null);
        userRepo.Setup(r => r.UpdateAsync(user)).ReturnsAsync(1);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);

        var sut = CreateSut(uow);

        var result = await sut.UpdateUserInfoAsync(userId, dto);

        result.PhoneNumber.Should().Be("0987654321");
        userRepo.Verify(r => r.GetByPhoneAsync("0987654321"), Times.Once);
    }

    [Fact]
    public async Task UpdateUserInfoAsync_ShouldUpdateProfileFields_Normal()
    {
        const int userId = 3;
        var dto = CreateValidUpdate(phone: null);
        dto.Address = "new addr";
        var user = CreateUser(userId, phone: null);

        var userRepo = new Mock<PlantDecor.DataAccessLayer.Interfaces.IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        userRepo.Setup(r => r.UpdateAsync(user)).ReturnsAsync(1);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);

        var sut = CreateSut(uow);

        var _ = await sut.UpdateUserInfoAsync(userId, dto);

        user.UserProfile.Should().NotBeNull();
        user.UserProfile!.Address.Should().Be("new addr");
    }

    [Fact]
    public async Task UpdateUserInfoAsync_ShouldAllowPhoneNull_Boundary()
    {
        const int userId = 4;
        var dto = CreateValidUpdate(phone: null);
        var user = CreateUser(userId, phone: "0123456789");

        var userRepo = new Mock<PlantDecor.DataAccessLayer.Interfaces.IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        userRepo.Setup(r => r.UpdateAsync(user)).ReturnsAsync(1);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);

        var sut = CreateSut(uow);

        var result = await sut.UpdateUserInfoAsync(userId, dto);

        result.Id.Should().Be(userId);
        userRepo.Verify(r => r.GetByPhoneAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserInfoAsync_ShouldNotCheckPhone_WhenSameAsCurrent_Boundary()
    {
        const int userId = 5;
        var dto = CreateValidUpdate(phone: "0123456789");
        var user = CreateUser(userId, phone: "0123456789");

        var userRepo = new Mock<PlantDecor.DataAccessLayer.Interfaces.IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        userRepo.Setup(r => r.UpdateAsync(user)).ReturnsAsync(1);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);

        var sut = CreateSut(uow);

        var _ = await sut.UpdateUserInfoAsync(userId, dto);

        userRepo.Verify(r => r.GetByPhoneAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserInfoAsync_ShouldThrowBadRequest_WhenPhoneAlreadyInUse_Abnormal()
    {
        const int userId = 6;
        var dto = CreateValidUpdate(phone: "0999999999");
        var user = CreateUser(userId, phone: "0123456789");

        var otherUser = CreateUser(99, phone: "0999999999");

        var userRepo = new Mock<PlantDecor.DataAccessLayer.Interfaces.IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        userRepo.Setup(r => r.GetByPhoneAsync("0999999999")).ReturnsAsync(otherUser);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);

        var sut = CreateSut(uow);

        var act = () => sut.UpdateUserInfoAsync(userId, dto);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Phone number '0999999999' is already in use");

        uow.Verify(x => x.RollbackTransactionAsync(), Times.AtLeastOnce);
        uow.Verify(x => x.CommitTransactionAsync(), Times.Never);
    }
}

