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
    public async Task LoginAsync_ShouldThrowBadRequest_WhenPasswordIncorrect()
    {
        var user = CreateValidUser();

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        userRepo.Setup(r => r.VerifyPasswordAsync(user, "pw")).ReturnsAsync(false);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);

        var stampCache = new Mock<ISecurityStampCacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, stampCache);

        var act = () => sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "pw", DeviceId = "dev1" });

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Incorrect password");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowForbidden_WhenAccountDisabled()
    {
        var user = CreateValidUser();
        user.Status = (int)UserStatusEnum.Inactive;

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        userRepo.Setup(r => r.VerifyPasswordAsync(user, "pw")).ReturnsAsync(true);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);

        var stampCache = new Mock<ISecurityStampCacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, stampCache);

        var act = () => sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "pw", DeviceId = "dev1" });

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Account is disabled");
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
    public async Task LoginAsync_ShouldThrowException_WhenSaveFails()
    {
        var user = CreateValidUser();

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        userRepo.Setup(r => r.VerifyPasswordAsync(user, "pw")).ReturnsAsync(true);
        userRepo.Setup(r => r.GetOldRefreshTokenByDeviceIdAsync(user.Id, "dev1"))
            .ReturnsAsync(new List<RefreshToken>());

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(0);

        var stampCache = new Mock<ISecurityStampCacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, stampCache);

        var act = () => sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "pw", DeviceId = "dev1" });

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Failed to update RefreshToken");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_RevokeOldTokens_AndSetStampCache_WhenSuccess()
    {
        var user = CreateValidUser();

        var oldTokens = new List<RefreshToken>
        {
            new RefreshToken { UserId = user.Id, Token = "old1", IsRevoked = false, DeviceId = "dev1", CreatedDate = DateTime.UtcNow, ExpiryDate = DateTime.UtcNow.AddDays(1) },
            new RefreshToken { UserId = user.Id, Token = "old2", IsRevoked = false, DeviceId = "dev1", CreatedDate = DateTime.UtcNow, ExpiryDate = DateTime.UtcNow.AddDays(1) }
        };

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        userRepo.Setup(r => r.VerifyPasswordAsync(user, "pw")).ReturnsAsync(true);
        userRepo.Setup(r => r.GetOldRefreshTokenByDeviceIdAsync(user.Id, "dev1"))
            .ReturnsAsync(oldTokens);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var stampCache = new Mock<ISecurityStampCacheService>(MockBehavior.Strict);
        stampCache.Setup(s => s.SetSecurityStampAsync(user.Id, user.SecurityStamp!)).Returns(Task.CompletedTask);

        var sut = CreateSut(uow, stampCache);

        var result = await sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "pw", DeviceId = "dev1" });

        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();

        oldTokens.Should().OnlyContain(t => t.IsRevoked);
        user.RefreshTokens.Should().ContainSingle(t => t.DeviceId == "dev1" && t.IsRevoked == false);

        uow.Verify(x => x.SaveAsync(), Times.Once);
        stampCache.Verify(s => s.SetSecurityStampAsync(user.Id, user.SecurityStamp!), Times.Once);
    }
}