using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Services;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Interfaces;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.Tests;

public class AuthenticationServiceUnitTest
{
    private static AuthenticationService CreateSut(
        Mock<IUnitOfWork> uow,
        Mock<ISecurityStampCacheService> stampCache,
        IConfiguration? configuration = null)
    {
        var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var emailService = new Mock<IEmailService>(MockBehavior.Loose);
        var otpCacheService = new Mock<IOtpCacheService>(MockBehavior.Loose);

        configuration ??= new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = "unit-test-secret-key-should-be-long-enough",
                ["JwtSettings:Issuer"] = "unit-test-issuer",
                ["JwtSettings:Audience"] = "unit-test-audience",
                ["JwtSettings:ExpiryMinutes"] = "30"
            })
            .Build();

        return new AuthenticationService(
            httpClientFactory.Object,
            uow.Object,
            configuration,
            emailService.Object,
            stampCache.Object,
            otpCacheService.Object);
    }

    private static User CreateValidUser()
    {
        return new User
        {
            Id = 1,
            Email = "test@example.com",
            Username = "testuser",
            IsVerified = true,
            Status = (int)UserStatusEnum.Active,
            SecurityStamp = "stamp-123",
            Role = new Role { Id = 1, Name = "Customer" }
        };
    }

    private static UserRequest CreateValidRegisterRequest(string? phoneNumber = null, string? password = null)
    {
        password ??= "Aa1!aaaa";

        return new UserRequest
        {
            Email = "newuser@example.com",
            Username = "newuser",
            FullName = "New User",
            PhoneNumber = phoneNumber,
            Password = password,
            ConfirmPassword = password
        };
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowBadRequest_WhenEmailOrPasswordEmpty()
    {
        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);

        var stampCache = new Mock<ISecurityStampCacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, stampCache);

        var act1 = () => sut.LoginAsync(new LoginRequest { Email = "", Password = "x", DeviceId = "dev1" });
        var act2 = () => sut.LoginAsync(new LoginRequest { Email = "a@b.com", Password = "", DeviceId = "dev1" });

        await act1.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Email and password must not be empty");
        await act2.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Email and password must not be empty");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowBadRequest_WhenDeviceIdMissing()
    {
        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);

        var stampCache = new Mock<ISecurityStampCacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, stampCache);

        var act = () => sut.LoginAsync(new LoginRequest { Email = "a@b.com", Password = "pw", DeviceId = "" });

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("DeviceId is required for login");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowNotFound_WhenUserNotFound()
    {
        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByEmailAsync("a@b.com")).ReturnsAsync((User?)null);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);

        var stampCache = new Mock<ISecurityStampCacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, stampCache);

        var act = () => sut.LoginAsync(new LoginRequest { Email = "a@b.com", Password = "pw", DeviceId = "dev1" });

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("User not found");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorized_WhenEmailNotVerified()
    {
        var user = CreateValidUser();
        user.IsVerified = false;

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);

        var stampCache = new Mock<ISecurityStampCacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, stampCache);

        var act = () => sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "pw", DeviceId = "dev1" });

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Account email has not been verified");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowForbidden_WhenRoleMissing()
    {
        var user = CreateValidUser();
        user.Role = null;

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        userRepo.Setup(r => r.VerifyPasswordAsync(user, "pw")).ReturnsAsync(true);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);

        var stampCache = new Mock<ISecurityStampCacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, stampCache);

        var act = () => sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "pw", DeviceId = "dev1" });

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("User role not found");
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnUser_WhenValidRequestWithoutPhone_Normal()
    {
        var request = CreateValidRegisterRequest(phoneNumber: null);

        var createdUser = new User
        {
            Id = 10,
            Email = request.Email,
            Username = request.Username,
            PhoneNumber = null,
            RoleId = (int)RoleEnum.Customer,
            Status = (int)UserStatusEnum.Active,
            Role = new Role { Id = (int)RoleEnum.Customer, Name = "Customer" },
            UserProfile = new UserProfile { FullName = request.FullName }
        };

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByPhoneAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        userRepo.SetupSequence(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null)
            .ReturnsAsync(createdUser);
        userRepo.Setup(r => r.PrepareCreate(It.IsAny<User>()));

        var roleRepo = new Mock<IRoleRepository>(MockBehavior.Strict);
        roleRepo.Setup(r => r.GetByIdAsync((int)RoleEnum.Customer))
            .ReturnsAsync(new Role { Id = (int)RoleEnum.Customer, Name = "Customer" });

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.RoleRepository).Returns(roleRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var stampCache = new Mock<ISecurityStampCacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, stampCache);

        var result = await sut.RegisterAsync(request);

        result.Should().NotBeNull();
        result!.User.Should().NotBeNull();
        result.User!.Email.Should().Be(request.Email);
        result.User.Username.Should().Be(request.Username);

        uow.Verify(x => x.BeginTransactionAsync(), Times.Once);
        uow.Verify(x => x.CommitTransactionAsync(), Times.Once);
        uow.Verify(x => x.RollbackTransactionAsync(), Times.Never);
        userRepo.Verify(r => r.PrepareCreate(It.IsAny<User>()), Times.Once);
        uow.Verify(x => x.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldHashPassword_AndSetCustomerRole_Normal()
    {
        var request = CreateValidRegisterRequest(phoneNumber: null, password: "Ab1!abcd");

        User? preparedUser = null;

        var createdUser = new User
        {
            Id = 11,
            Email = request.Email,
            Username = request.Username,
            RoleId = (int)RoleEnum.Customer,
            Status = (int)UserStatusEnum.Active,
            Role = new Role { Id = (int)RoleEnum.Customer, Name = "Customer" },
            UserProfile = new UserProfile { FullName = request.FullName }
        };

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByPhoneAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        userRepo.SetupSequence(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null)
            .ReturnsAsync(createdUser);
        userRepo.Setup(r => r.PrepareCreate(It.IsAny<User>()))
            .Callback<User>(u => preparedUser = u);

        var roleRepo = new Mock<IRoleRepository>(MockBehavior.Strict);
        roleRepo.Setup(r => r.GetByIdAsync((int)RoleEnum.Customer))
            .ReturnsAsync(new Role { Id = (int)RoleEnum.Customer, Name = "Customer" });

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.RoleRepository).Returns(roleRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var stampCache = new Mock<ISecurityStampCacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, stampCache);

        var _ = await sut.RegisterAsync(request);

        preparedUser.Should().NotBeNull();
        preparedUser!.RoleId.Should().Be((int)RoleEnum.Customer);
        preparedUser.PasswordHash.Should().NotBeNullOrWhiteSpace();
        preparedUser.PasswordHash.Should().NotBe(request.Password);
        BCrypt.Net.BCrypt.Verify(request.Password, preparedUser.PasswordHash).Should().BeTrue();
        preparedUser.SecurityStamp.Should().NotBeNullOrWhiteSpace();

        uow.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldSucceed_WhenValidPhoneNumberProvided_Normal()
    {
        var request = CreateValidRegisterRequest(phoneNumber: "0123456789");

        var createdUser = new User
        {
            Id = 12,
            Email = request.Email,
            Username = request.Username,
            PhoneNumber = request.PhoneNumber,
            RoleId = (int)RoleEnum.Customer,
            Status = (int)UserStatusEnum.Active,
            Role = new Role { Id = (int)RoleEnum.Customer, Name = "Customer" },
            UserProfile = new UserProfile { FullName = request.FullName }
        };

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByPhoneAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        userRepo.SetupSequence(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null)
            .ReturnsAsync(createdUser);
        userRepo.Setup(r => r.PrepareCreate(It.IsAny<User>()));

        var roleRepo = new Mock<IRoleRepository>(MockBehavior.Strict);
        roleRepo.Setup(r => r.GetByIdAsync((int)RoleEnum.Customer))
            .ReturnsAsync(new Role { Id = (int)RoleEnum.Customer, Name = "Customer" });

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.RoleRepository).Returns(roleRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var stampCache = new Mock<ISecurityStampCacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, stampCache);

        var result = await sut.RegisterAsync(request);

        result.Should().NotBeNull();
        result!.User!.PhoneNumber.Should().Be(request.PhoneNumber);
        uow.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldSucceed_WhenPasswordIsExactly8Chars_Boundary()
    {
        var request = CreateValidRegisterRequest(phoneNumber: null, password: "Aa1!aaaa"); // length = 8

        var createdUser = new User
        {
            Id = 13,
            Email = request.Email,
            Username = request.Username,
            RoleId = (int)RoleEnum.Customer,
            Status = (int)UserStatusEnum.Active,
            Role = new Role { Id = (int)RoleEnum.Customer, Name = "Customer" },
            UserProfile = new UserProfile { FullName = request.FullName }
        };

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByPhoneAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        userRepo.SetupSequence(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null)
            .ReturnsAsync(createdUser);
        userRepo.Setup(r => r.PrepareCreate(It.IsAny<User>()));

        var roleRepo = new Mock<IRoleRepository>(MockBehavior.Strict);
        roleRepo.Setup(r => r.GetByIdAsync((int)RoleEnum.Customer))
            .ReturnsAsync(new Role { Id = (int)RoleEnum.Customer, Name = "Customer" });

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.RoleRepository).Returns(roleRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var stampCache = new Mock<ISecurityStampCacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, stampCache);

        var result = await sut.RegisterAsync(request);

        result.Should().NotBeNull();
        result!.User!.Email.Should().Be(request.Email);
        uow.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldSucceed_WhenPhoneIsPlus84Format_Boundary()
    {
        var request = CreateValidRegisterRequest(phoneNumber: "+84123456789");

        var createdUser = new User
        {
            Id = 14,
            Email = request.Email,
            Username = request.Username,
            PhoneNumber = request.PhoneNumber,
            RoleId = (int)RoleEnum.Customer,
            Status = (int)UserStatusEnum.Active,
            Role = new Role { Id = (int)RoleEnum.Customer, Name = "Customer" },
            UserProfile = new UserProfile { FullName = request.FullName }
        };

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByPhoneAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        userRepo.SetupSequence(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null)
            .ReturnsAsync(createdUser);
        userRepo.Setup(r => r.PrepareCreate(It.IsAny<User>()));

        var roleRepo = new Mock<IRoleRepository>(MockBehavior.Strict);
        roleRepo.Setup(r => r.GetByIdAsync((int)RoleEnum.Customer))
            .ReturnsAsync(new Role { Id = (int)RoleEnum.Customer, Name = "Customer" });

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.RoleRepository).Returns(roleRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var stampCache = new Mock<ISecurityStampCacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, stampCache);

        var result = await sut.RegisterAsync(request);

        result.Should().NotBeNull();
        result!.User!.PhoneNumber.Should().Be(request.PhoneNumber);
        uow.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldRollbackTransaction_WhenCreatedUserCannotBeRetrieved_Abnormal()
    {
        var request = CreateValidRegisterRequest(phoneNumber: null);

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByPhoneAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        userRepo.SetupSequence(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null)  // existing user check
            .ReturnsAsync((User?)null); // created user retrieval fails
        userRepo.Setup(r => r.PrepareCreate(It.IsAny<User>()));

        var roleRepo = new Mock<IRoleRepository>(MockBehavior.Strict);
        roleRepo.Setup(r => r.GetByIdAsync((int)RoleEnum.Customer))
            .ReturnsAsync(new Role { Id = (int)RoleEnum.Customer, Name = "Customer" });

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.RoleRepository).Returns(roleRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var stampCache = new Mock<ISecurityStampCacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, stampCache);

        var act = () => sut.RegisterAsync(request);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Failed to retrieve created user");

        uow.Verify(x => x.BeginTransactionAsync(), Times.Once);
        uow.Verify(x => x.CommitTransactionAsync(), Times.Never);
        uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }
}