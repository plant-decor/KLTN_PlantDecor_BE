using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string ALL_CART_KEY = "cart_user";

        public CartService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<PaginatedResult<CartItemResponseDto>> GetCartByUserIdAsync(int userId, Pagination pagination)
        {
            // Lấy ra cache trước khi truy vấn database
            var cacheKey = $"{ALL_CART_KEY}_{userId}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cached = await _cacheService.GetDataAsync<PaginatedResult<CartItemResponseDto>>(cacheKey);
            if (cached != null)
                return cached;

            // Đảm bảo cart tồn tại để lấy ra cartId
            var cart = await GetOrCreateCartAsync(userId);
            // Lấy ra danh sách CartItem với phân trang
            var paginatedItems = await _unitOfWork.CartRepository.GetItemsByCartIdWithPaginationAsync(cart.Id, pagination);
            var result = new PaginatedResult<CartItemResponseDto>(
                paginatedItems.Items.ToResponseList(),
                paginatedItems.TotalCount,
                paginatedItems.PageNumber,
                paginatedItems.PageSize);

            // Lưu vào cache với thời gian hết hạn 30 phút
            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<CartItemResponseDto> AddItemAsync(int userId, CartItemRequestDto request)
        {
            // Validate request
            ValidateCartItemRequest(request);

            // Lấy giá từ backend và kiểm tra tồn kho
            var (price, availableQty) = await GetPriceAndStockAsync(request);
            // Nếu đã tồn tại item trong cart thì availableQty phải trừ đi quantity của item đó để tránh trường hợp cộng dồn số lượng mà vượt quá tồn kho
            if (request.Quantity > availableQty)
                throw new BadRequestException($"The remaining stock isn't enough for request. Remaining: {availableQty}");

            var cart = await GetOrCreateCartAsync(userId);

            // Kiểm tra xem sản phẩm đã tồn tại trong cart chưa (dựa trên loại sản phẩm và ID tương ứng)
            CartItem resultItem;
            var existingItem = FindExistingCartItem(cart, request);
            // Nếu đã tồn tại, cộng thêm quantity; nếu chưa thì tạo mới CartItem
            if (existingItem != null)
            {
                var newQty = (existingItem.Quantity ?? 0) + request.Quantity;
                if (newQty > availableQty)
                    throw new BadRequestException($"The remaining stock isn't enough for request. Remaining: {availableQty}");

                existingItem.Quantity = newQty;
                existingItem.Price = price;
                _unitOfWork.CartRepository.PrepareUpdate(cart);
                resultItem = existingItem;
            }
            else
            {
                var newItem = new CartItem
                {
                    CartId = cart.Id,
                    CommonPlantId = request.CommonPlantId,
                    NurseryPlantComboId = request.NurseryPlantComboId,
                    NurseryMaterialId = request.NurseryMaterialId,
                    Quantity = request.Quantity,
                    Price = price,
                    CreatedAt = DateTime.Now
                };
                cart.CartItems.Add(newItem);
                resultItem = newItem;
            }

            cart.UpdatedAt = DateTime.Now;
            await _unitOfWork.SaveAsync();

            // Xoá cache để đảm bảo dữ liệu mới nhất được trả về ở lần truy vấn tiếp theo
            await _cacheService.RemoveByPrefixAsync($"{ALL_CART_KEY}_{userId}");

            var saved = await _unitOfWork.CartRepository.GetCartItemByIdAsync(resultItem.Id);
            return saved!.ToResponse();
        }

        public async Task<CartItemResponseDto> UpdateItemQuantityAsync(int userId, int cartItemId, UpdateCartItemDto request)
        {
            var item = await _unitOfWork.CartRepository.GetCartItemByIdAsync(cartItemId);
            if (item == null)
                throw new NotFoundException($"CartItem with ID {cartItemId} not exists");

            if (item.Cart?.UserId != userId)
                throw new BadRequestException("CartItem doesn't belong to this user");

            if (request.Quantity < 0)
            {
                throw new BadRequestException("Quantity must be greater than 0");
            }

            // Kiểm tra tồn kho trước khi cập nhật
            var availableQty = GetAvailableQty(item);
            if (request.Quantity > availableQty)
                throw new BadRequestException($"Available quantity is not enough. Remaining: {availableQty}");

            item.Quantity = request.Quantity;
            item.Cart!.UpdatedAt = DateTime.Now;
            await _unitOfWork.SaveAsync();

            // Xoá cache để đảm bảo dữ liệu mới nhất được trả về ở lần truy vấn tiếp theo
            await _cacheService.RemoveByPrefixAsync($"{ALL_CART_KEY}_{userId}");

            var updated = await _unitOfWork.CartRepository.GetCartItemByIdAsync(cartItemId);
            return updated!.ToResponse();
        }

        public async Task<bool> RemoveItemAsync(int userId, int cartItemId)
        {
            var item = await _unitOfWork.CartRepository.GetCartItemByIdAsync(cartItemId);
            if (item == null)
                throw new NotFoundException($"CartItem with ID {cartItemId} not exists");

            if (item.Cart?.UserId != userId)
                throw new BadRequestException("CartItem doesn't belong to this user");

            var result = await _unitOfWork.CartRepository.RemoveCartItemAsync(item);
            await _cacheService.RemoveByPrefixAsync($"{ALL_CART_KEY}_{userId}");
            return result;
        }

        public async Task<bool> ClearCartAsync(int userId)
        {
            var cart = await _unitOfWork.CartRepository.GetByUserIdAsync(userId);
            if (cart == null)
                return true;

            await _unitOfWork.CartRepository.ClearCartItemsAsync(cart.Id);
            await _cacheService.RemoveByPrefixAsync($"{ALL_CART_KEY}_{userId}");
            return true;
        }

        #region Helpers

        private async Task<Cart> GetOrCreateCartAsync(int userId)
        {
            var cart = await _unitOfWork.CartRepository.GetByUserIdAsync(userId);
            if (cart != null) return cart;

            var newCart = new Cart
            {
                UserId = userId,
                UpdatedAt = DateTime.Now
            };
            await _unitOfWork.CartRepository.CreateAsync(newCart);

            return (await _unitOfWork.CartRepository.GetByUserIdAsync(userId))!;
        }

        // Đảm bảo CartItemRequestDto luôn có đúng 1 loại sản phẩm được chỉ định, tránh trường hợp người dùng gửi request sai hoặc thiếu thông tin
        private static void ValidateCartItemRequest(CartItemRequestDto request)
        {
            var setCount = new[] { request.CommonPlantId, request.NurseryPlantComboId, request.NurseryMaterialId }
                .Count(id => id.HasValue);

            if (setCount == 0)
                throw new BadRequestException("Must choose 1 of the following types (CommonPlantId, NurseryPlantComboId hoặc NurseryMaterialId)");

            if (setCount > 1)
                throw new BadRequestException("Only 1 type of product is selected!");

            if (request.Quantity < 0)
            {
                throw new BadRequestException("Quantity must be greater than 0");
            }
        }

        // Lấy giá từ entity gốc và kiểm tra tồn kho
        private async Task<(decimal price, int availableQty)> GetPriceAndStockAsync(CartItemRequestDto request)
        {
            if (request.CommonPlantId.HasValue)
            {
                var commonPlant = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(request.CommonPlantId.Value);
                if (commonPlant == null || !commonPlant.IsActive)
                    throw new NotFoundException($"CommonPlant {request.CommonPlantId.Value} not exists or has been discontinued");

                var price = commonPlant.Plant.BasePrice ?? 0;
                var availableQty = commonPlant.Quantity;
                return (price, availableQty);
            }

            if (request.NurseryMaterialId.HasValue)
            {
                var nurseryMaterial = await _unitOfWork.NurseryMaterialRepository.GetByIdWithDetailsAsync(request.NurseryMaterialId.Value);
                if (nurseryMaterial == null || !nurseryMaterial.IsActive)
                    throw new NotFoundException($"NurseryMaterial {request.NurseryMaterialId.Value} not exists or has been discontinued");

                var price = nurseryMaterial.Material.BasePrice ?? 0;
                var availableQty = nurseryMaterial.Quantity;
                return (price, availableQty);
            }

            // NurseryPlantComboId
            var combo = await _unitOfWork.NurseryPlantComboRepository.GetByIdAsync(request.NurseryPlantComboId!.Value);
            if (combo == null || !combo.IsActive)
                throw new NotFoundException($"NurseryPlantCombo {request.NurseryPlantComboId.Value} not exists or has been discontinued");

            return (combo.PlantCombo.ComboPrice ?? 0, combo.Quantity);
        }

        // Lấy số lượng tồn kho khả dụng từ CartItem đã được load entity
        private static int GetAvailableQty(CartItem item)
        {
            if (item.CommonPlant != null)
                return item.CommonPlant.Quantity;

            if (item.NurseryMaterial != null)
                return item.NurseryMaterial.Quantity;

            if (item.NurseryPlantCombo != null)
                return item.NurseryPlantCombo.Quantity;

            throw new BadRequestException("CartItem doesn't have a product to be linked with");
        }

        private static CartItem? FindExistingCartItem(Cart cart, CartItemRequestDto request)
        {
            return cart.CartItems.FirstOrDefault(i =>
                (request.CommonPlantId.HasValue && i.CommonPlantId == request.CommonPlantId) ||
                (request.NurseryPlantComboId.HasValue && i.NurseryPlantComboId == request.NurseryPlantComboId) ||
                (request.NurseryMaterialId.HasValue && i.NurseryMaterialId == request.NurseryMaterialId));
        }

        #endregion
    }
}
