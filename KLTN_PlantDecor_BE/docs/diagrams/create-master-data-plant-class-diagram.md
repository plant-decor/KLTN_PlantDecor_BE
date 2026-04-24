# Class Diagram - Create master data plant

```mermaid
classDiagram
    class PlantsController {
        -IPlantService _plantService
        +CreatePlant(request: PlantRequestDto) Task<IActionResult>
    }

    class IPlantService {
        <<interface>>
        +CreatePlantAsync(request: PlantRequestDto) Task<PlantResponseDto>
    }

    class PlantService {
        -IUnitOfWork _unitOfWork
        -ICacheService _cacheService
        +CreatePlantAsync(request: PlantRequestDto) Task<PlantResponseDto>
        -ValidateEnumBackedFields(roomStyles: List<int>?, roomTypes: List<int>?) void
        -InvalidateCacheAsync() Task
    }

    class IUnitOfWork {
        <<interface>>
        +PlantRepository: IPlantRepository
        +BeginTransactionAsync() Task
        +SaveAsync() Task<int>
        +CommitTransactionAsync() Task
        +RollbackTransactionAsync() Task
    }

    class IPlantRepository {
        <<interface>>
        +ExistsByNameAsync(name: string, excludeId: int?) Task<bool>
        +PrepareCreate(entity: Plant) void
    }

    class IGenericRepository~T~ {
        <<interface>>
        +PrepareCreate(entity: T) void
        +SaveAsync() Task<int>
    }

    class PlantMapper {
        <<static>>
        +ToEntity(request: PlantRequestDto) Plant
        +ToResponse(plant: Plant) PlantResponseDto
    }

    class PlantRequestDto {
        +Name: string
        +BasePrice: decimal?
        +PlacementType: int
        +RoomStyle: List<int>?
        +RoomType: List<int>?
        +CareLevelType: int?
        +IsActive: bool
        +IsUniqueInstance: bool
    }

    class PlantResponseDto {
        +Id: int
        +Name: string
        +BasePrice: decimal?
        +PlacementType: int?
        +RoomStyle: List<int>?
        +RoomType: List<int>?
        +CareLevelType: int?
        +IsActive: bool?
        +CreatedAt: DateTime?
        +UpdatedAt: DateTime?
    }

    class Plant {
        +Id: int
        +Name: string
        +BasePrice: decimal?
        +PlacementType: int
        +RoomStyle: List<int>?
        +RoomType: List<int>?
        +CareLevelType: int?
        +IsActive: bool?
        +IsUniqueInstance: bool
        +CreatedAt: DateTime?
        +UpdatedAt: DateTime?
    }

    class RoomStyleEnum {
        <<enum>>
    }

    class RoomTypeEnum {
        <<enum>>
    }

    class ApiResponse~T~ {
        +Success: bool
        +StatusCode: int
        +Message: string
        +Payload: T
    }

    class BadRequestException

    PlantsController --> IPlantService : uses
    PlantService ..|> IPlantService : implements
    PlantService --> IUnitOfWork : uses
    PlantService --> ICacheService : invalidates cache
    IUnitOfWork --> IPlantRepository : exposes
    IPlantRepository --|> IGenericRepository~Plant~ : inherits

    PlantService ..> PlantRequestDto : input
    PlantService ..> PlantMapper : map request/entity/response
    PlantMapper ..> PlantRequestDto : map from
    PlantMapper ..> Plant : map to
    PlantMapper ..> PlantResponseDto : map to

    PlantService ..> RoomStyleEnum : validate values
    PlantService ..> RoomTypeEnum : validate values
    PlantService ..> BadRequestException : throw when invalid

    PlantsController ..> ApiResponse~PlantResponseDto~ : wraps response
```

## Sequence note for create flow

1. PlantsController.CreatePlant nhận PlantRequestDto.
2. PlantService.CreatePlantAsync mở transaction qua IUnitOfWork.
3. Service gọi IPlantRepository.ExistsByNameAsync để chống trùng tên.
4. Service validate RoomStyle và RoomType theo enum.
5. PlantMapper.ToEntity chuyển PlantRequestDto thành Plant.
6. Service gọi IPlantRepository.PrepareCreate rồi IUnitOfWork.SaveAsync.
7. Service commit transaction, invalidate cache, map PlantResponseDto và trả về.
