using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
using System.Text;

namespace PlantDecor.Tests;

public class RoomImageServiceUnitTest
{
    private static IFormFile CreateFile(string name = "room.jpg", string contentType = "image/jpeg")
    {
        var bytes = Encoding.UTF8.GetBytes("fake");
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static RoomImageService CreateSut(
        Mock<IUnitOfWork> uow,
        Mock<ICloudinaryService> cloudinary)
    {
        var logger = new Mock<ILogger<RoomImageService>>(MockBehavior.Loose);
        return new RoomImageService(uow.Object, cloudinary.Object, logger.Object);
    }

    [Fact]
    public async Task UploadRoomImagesAsync_ShouldUploadAndPersistImages_Normal()
    {
        const int userId = 1;
        var files = new List<IFormFile> { CreateFile(), CreateFile("room2.jpg") };
        var request = new UploadRoomImagesRequest
        {
            Images = files,
            ViewAngles = new List<RoomViewAngleEnum> { RoomViewAngleEnum.Front, RoomViewAngleEnum.Left }
        };

        var cloudinary = new Mock<ICloudinaryService>(MockBehavior.Strict);
        cloudinary.Setup(c => c.ValidateDocumentFile(It.IsAny<IFormFile>(), It.IsAny<int>()))
            .Returns((true, ""));
        cloudinary.SetupSequence(c => c.UploadFileAsync(It.IsAny<IFormFile>(), "RoomImages"))
            .ReturnsAsync(new FileUploadResponse { SecureUrl = "https://cdn.test/1.jpg" })
            .ReturnsAsync(new FileUploadResponse { SecureUrl = "https://cdn.test/2.jpg" });

        var roomImageRepo = new Mock<IRoomImageRepository>(MockBehavior.Strict);
        var created = new List<RoomImage>();
        roomImageRepo.Setup(r => r.PrepareCreate(It.IsAny<RoomImage>()))
            .Callback<RoomImage>(ri =>
            {
                ri.Id = created.Count + 1; // simulate db identity
                created.Add(ri);
            });

        var moderationRepo = new Mock<IRoomUploadModerationRepository>(MockBehavior.Strict);
        moderationRepo.Setup(r => r.PrepareCreate(It.IsAny<RoomUploadModeration>()));

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.RoomImageRepository).Returns(roomImageRepo.Object);
        uow.SetupGet(x => x.RoomUploadModerationRepository).Returns(moderationRepo.Object);
        uow.SetupSequence(x => x.SaveAsync())
            .ReturnsAsync(1) // save room images
            .ReturnsAsync(1) // moderation for image 1
            .ReturnsAsync(1); // moderation for image 2

        var sut = CreateSut(uow, cloudinary);

        var result = await sut.UploadRoomImagesAsync(request, userId);

        result.RoomImages.Should().HaveCount(2);
        created.Should().HaveCount(2);
        created.Select(x => x.ImageUrl).Should().BeEquivalentTo(new[] { "https://cdn.test/1.jpg", "https://cdn.test/2.jpg" });
        moderationRepo.Verify(r => r.PrepareCreate(It.IsAny<RoomUploadModeration>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UploadRoomImagesAsync_ShouldMapViewAnglesToImages_Normal()
    {
        const int userId = 2;
        var files = new List<IFormFile> { CreateFile() };
        var request = new UploadRoomImagesRequest
        {
            Images = files,
            ViewAngles = new List<RoomViewAngleEnum> { RoomViewAngleEnum.Back }
        };

        var cloudinary = new Mock<ICloudinaryService>(MockBehavior.Strict);
        cloudinary.Setup(c => c.ValidateDocumentFile(It.IsAny<IFormFile>(), It.IsAny<int>()))
            .Returns((true, ""));
        cloudinary.Setup(c => c.UploadFileAsync(It.IsAny<IFormFile>(), "RoomImages"))
            .ReturnsAsync(new FileUploadResponse { SecureUrl = "https://cdn.test/back.jpg" });

        RoomImage? created = null;
        var roomImageRepo = new Mock<IRoomImageRepository>(MockBehavior.Strict);
        roomImageRepo.Setup(r => r.PrepareCreate(It.IsAny<RoomImage>()))
            .Callback<RoomImage>(ri =>
            {
                ri.Id = 10;
                created = ri;
            });

        var moderationRepo = new Mock<IRoomUploadModerationRepository>(MockBehavior.Strict);
        moderationRepo.Setup(r => r.PrepareCreate(It.IsAny<RoomUploadModeration>()));

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.RoomImageRepository).Returns(roomImageRepo.Object);
        uow.SetupGet(x => x.RoomUploadModerationRepository).Returns(moderationRepo.Object);
        uow.SetupSequence(x => x.SaveAsync())
            .ReturnsAsync(1) // images
            .ReturnsAsync(1); // moderation

        var sut = CreateSut(uow, cloudinary);

        var _ = await sut.UploadRoomImagesAsync(request, userId);

        created.Should().NotBeNull();
        created!.ViewAngle.Should().Be((int)RoomViewAngleEnum.Back);
        created.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task UploadRoomImagesAsync_ShouldSaveApprovedModeration_WhenSuccess_Normal()
    {
        const int userId = 3;
        var request = new UploadRoomImagesRequest
        {
            Images = new List<IFormFile> { CreateFile() },
            ViewAngles = new List<RoomViewAngleEnum> { RoomViewAngleEnum.Right }
        };

        var cloudinary = new Mock<ICloudinaryService>(MockBehavior.Strict);
        cloudinary.Setup(c => c.ValidateDocumentFile(It.IsAny<IFormFile>(), It.IsAny<int>()))
            .Returns((true, ""));
        cloudinary.Setup(c => c.UploadFileAsync(It.IsAny<IFormFile>(), "RoomImages"))
            .ReturnsAsync(new FileUploadResponse { SecureUrl = "https://cdn.test/right.jpg" });

        var roomImageRepo = new Mock<IRoomImageRepository>(MockBehavior.Strict);
        roomImageRepo.Setup(r => r.PrepareCreate(It.IsAny<RoomImage>()))
            .Callback<RoomImage>(ri => ri.Id = 20);

        RoomUploadModeration? moderation = null;
        var moderationRepo = new Mock<IRoomUploadModerationRepository>(MockBehavior.Strict);
        moderationRepo.Setup(r => r.PrepareCreate(It.IsAny<RoomUploadModeration>()))
            .Callback<RoomUploadModeration>(m => moderation = m);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.RoomImageRepository).Returns(roomImageRepo.Object);
        uow.SetupGet(x => x.RoomUploadModerationRepository).Returns(moderationRepo.Object);
        uow.SetupSequence(x => x.SaveAsync()).ReturnsAsync(1).ReturnsAsync(1);

        var sut = CreateSut(uow, cloudinary);

        await sut.UploadRoomImagesAsync(request, userId);

        moderation.Should().NotBeNull();
        moderation!.RoomImageId.Should().Be(20);
        moderation.Status.Should().Be((int)RoomUploadModerationStatusEnum.Approved);
    }

    [Fact]
    public async Task UploadRoomImagesAsync_ShouldAllowMax4Images_Boundary()
    {
        const int userId = 4;
        var request = new UploadRoomImagesRequest
        {
            Images = new List<IFormFile> { CreateFile(), CreateFile("2.jpg"), CreateFile("3.jpg"), CreateFile("4.jpg") },
            ViewAngles = new List<RoomViewAngleEnum> { RoomViewAngleEnum.Front, RoomViewAngleEnum.Left, RoomViewAngleEnum.Right, RoomViewAngleEnum.Back }
        };

        var cloudinary = new Mock<ICloudinaryService>(MockBehavior.Strict);
        cloudinary.Setup(c => c.ValidateDocumentFile(It.IsAny<IFormFile>(), It.IsAny<int>()))
            .Returns((true, ""));
        cloudinary.Setup(c => c.UploadFileAsync(It.IsAny<IFormFile>(), "RoomImages"))
            .ReturnsAsync(new FileUploadResponse { SecureUrl = "https://cdn.test/x.jpg" });

        var roomImageRepo = new Mock<IRoomImageRepository>(MockBehavior.Strict);
        roomImageRepo.Setup(r => r.PrepareCreate(It.IsAny<RoomImage>()))
            .Callback<RoomImage>(ri => ri.Id = 1);

        var moderationRepo = new Mock<IRoomUploadModerationRepository>(MockBehavior.Strict);
        moderationRepo.Setup(r => r.PrepareCreate(It.IsAny<RoomUploadModeration>()));

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.RoomImageRepository).Returns(roomImageRepo.Object);
        uow.SetupGet(x => x.RoomUploadModerationRepository).Returns(moderationRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, cloudinary);

        var result = await sut.UploadRoomImagesAsync(request, userId);

        result.RoomImages.Should().HaveCount(4);
    }

    [Fact]
    public async Task UploadRoomImagesAsync_ShouldRejectDuplicateViewAngles_Boundary()
    {
        const int userId = 5;
        var request = new UploadRoomImagesRequest
        {
            Images = new List<IFormFile> { CreateFile(), CreateFile("2.jpg") },
            ViewAngles = new List<RoomViewAngleEnum> { RoomViewAngleEnum.Front, RoomViewAngleEnum.Front }
        };

        var cloudinary = new Mock<ICloudinaryService>(MockBehavior.Strict);

        RoomUploadModeration? moderation = null;
        var moderationRepo = new Mock<IRoomUploadModerationRepository>(MockBehavior.Strict);
        moderationRepo.Setup(r => r.PrepareCreate(It.IsAny<RoomUploadModeration>()))
            .Callback<RoomUploadModeration>(m => moderation = m);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.RoomUploadModerationRepository).Returns(moderationRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, cloudinary);

        var act = () => sut.UploadRoomImagesAsync(request, userId);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("ViewAngles must be unique within one upload");

        moderation.Should().NotBeNull();
        moderation!.Status.Should().Be((int)RoomUploadModerationStatusEnum.Rejected);
    }

    [Fact]
    public async Task UploadRoomImagesAsync_ShouldSaveRejectedModeration_WhenUploadThrows_Abnormal()
    {
        const int userId = 6;
        var request = new UploadRoomImagesRequest
        {
            Images = new List<IFormFile> { CreateFile() },
            ViewAngles = new List<RoomViewAngleEnum> { RoomViewAngleEnum.Front }
        };

        var cloudinary = new Mock<ICloudinaryService>(MockBehavior.Strict);
        cloudinary.Setup(c => c.ValidateDocumentFile(It.IsAny<IFormFile>(), It.IsAny<int>()))
            .Returns((true, ""));
        cloudinary.Setup(c => c.UploadFileAsync(It.IsAny<IFormFile>(), "RoomImages"))
            .ThrowsAsync(new Exception("upload failed"));

        var roomImageRepo = new Mock<IRoomImageRepository>(MockBehavior.Strict);
        roomImageRepo.Setup(r => r.PrepareCreate(It.IsAny<RoomImage>()));

        RoomUploadModeration? moderation = null;
        var moderationRepo = new Mock<IRoomUploadModerationRepository>(MockBehavior.Strict);
        moderationRepo.Setup(r => r.PrepareCreate(It.IsAny<RoomUploadModeration>()))
            .Callback<RoomUploadModeration>(m => moderation = m);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.RoomImageRepository).Returns(roomImageRepo.Object);
        uow.SetupGet(x => x.RoomUploadModerationRepository).Returns(moderationRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1); // for moderation save in helper

        var sut = CreateSut(uow, cloudinary);

        var act = () => sut.UploadRoomImagesAsync(request, userId);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("upload failed");

        moderation.Should().NotBeNull();
        moderation!.Status.Should().Be((int)RoomUploadModerationStatusEnum.Rejected);
        moderation.Reason.Should().Contain("upload failed");
    }
}

