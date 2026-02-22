using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class InventoryMapper
    {
        #region Entity to Response
        public static InventoryResponseDto ToResponse(this Inventory inventory)
        {
            if (inventory == null) return null!;
            return new InventoryResponseDto
            {
                Id = inventory.Id,
                InventoryCode = inventory.InventoryCode,
                Name = inventory.Name,
                Description = inventory.Description,
                BasePrice = inventory.BasePrice,
                StockQuantity = inventory.StockQuantity,
                Unit = inventory.Unit,
                Brand = inventory.Brand,
                Specifications = inventory.Specifications,
                ExpiryMonths = inventory.ExpiryMonths,
                IsActive = inventory.IsActive,
                CreatedAt = inventory.CreatedAt,
                UpdatedAt = inventory.UpdatedAt,
                Categories = inventory.Categories.Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ParentCategoryId = c.ParentCategoryId,
                    IsActive = c.IsActive
                }).ToList(),
                Tags = inventory.Tags.Select(t => new TagResponseDto
                {
                    Id = t.Id,
                    TagName = t.TagName
                }).ToList(),
                Images = inventory.InventoryImages.Select(i => new InventoryImageResponseDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    IsPrimary = i.IsPrimary
                }).ToList()
            };
        }

        public static InventoryListResponseDto ToListResponse(this Inventory inventory)
        {
            if (inventory == null) return null!;
            return new InventoryListResponseDto
            {
                Id = inventory.Id,
                InventoryCode = inventory.InventoryCode,
                Name = inventory.Name,
                BasePrice = inventory.BasePrice,
                StockQuantity = inventory.StockQuantity,
                Unit = inventory.Unit,
                Brand = inventory.Brand,
                IsActive = inventory.IsActive,
                PrimaryImageUrl = inventory.InventoryImages.FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl
                    ?? inventory.InventoryImages.FirstOrDefault()?.ImageUrl,
                CategoryNames = inventory.Categories.Select(c => c.Name).ToList(),
                TagNames = inventory.Tags.Select(t => t.TagName).ToList()
            };
        }

        public static List<InventoryResponseDto> ToResponseList(this IEnumerable<Inventory> inventories)
        {
            return inventories.Select(i => i.ToResponse()).ToList();
        }

        public static List<InventoryListResponseDto> ToListResponseList(this IEnumerable<Inventory> inventories)
        {
            return inventories.Select(i => i.ToListResponse()).ToList();
        }
        #endregion

        #region Request to Entity
        public static Inventory ToEntity(this InventoryRequestDto request)
        {
            if (request == null) return null!;

            return new Inventory
            {
                InventoryCode = request.InventoryCode,
                Name = request.Name,
                Description = request.Description,
                BasePrice = request.BasePrice,
                StockQuantity = request.StockQuantity ?? 0,
                Unit = request.Unit,
                Brand = request.Brand,
                Specifications = request.Specifications,
                ExpiryMonths = request.ExpiryMonths,
                IsActive = request.IsActive,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }
        #endregion

        #region Update Entity
        public static void ToUpdate(this InventoryUpdateDto request, Inventory inventory)
        {
            if (request == null || inventory == null) return;

            inventory.InventoryCode = request.InventoryCode ?? inventory.InventoryCode;
            inventory.Name = request.Name;
            inventory.Description = request.Description;
            inventory.BasePrice = request.BasePrice;
            inventory.StockQuantity = request.StockQuantity ?? inventory.StockQuantity;
            inventory.Unit = request.Unit;
            inventory.Brand = request.Brand;
            inventory.Specifications = request.Specifications;
            inventory.ExpiryMonths = request.ExpiryMonths;
            inventory.IsActive = request.IsActive ?? inventory.IsActive;
            inventory.UpdatedAt = DateTime.Now;
        }
        #endregion

        #region Helper
        public static string GenerateInventoryCode()
        {
            return $"INV{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(100, 999)}";
        }
        #endregion
    }
}
