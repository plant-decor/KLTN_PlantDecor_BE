using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public InvoiceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<InvoiceResponseDto> GetInvoiceByIdAsync(int invoiceId, int userId)
        {
            var invoice = await _unitOfWork.InvoiceRepository.GetByIdWithDetailsAsync(invoiceId);
            if (invoice == null)
                throw new NotFoundException($"Invoice {invoiceId} not found");

            if (invoice.Order?.UserId != userId)
                throw new ForbiddenException("You don't have access to this invoice");

            return MapToDto(invoice);
        }

        public async Task<List<InvoiceResponseDto>> GetInvoicesByOrderIdAsync(int orderId, int userId)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new NotFoundException($"Order {orderId} not found");

            if (order.UserId != userId)
                throw new ForbiddenException("You don't have access to this order");

            var invoices = await _unitOfWork.InvoiceRepository.GetByOrderIdAsync(orderId);
            return invoices.Select(MapToDto).ToList();
        }

        public async Task<InvoiceResponseDto> GenerateRemainingInvoiceAsync(int orderId)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId);
            if (order == null)
                throw new NotFoundException($"Order {orderId} not found");

            if (order.PaymentStrategy != (int)PaymentStrategiesEnum.Deposit)
                throw new BadRequestException("Order does not use deposit payment strategy");

            if (order.Status != (int)OrderStatusEnum.RemainingPaymentPending)
                throw new BadRequestException("Order is not in RemainingPaymentPending status");

            // Check if RemainingBalance invoice already exists
            var existing = await _unitOfWork.InvoiceRepository
                .GetPendingByOrderIdAndTypeAsync(orderId, (int)InvoiceTypeEnum.RemainingBalance);

            if (existing != null)
                return MapToDto(existing);

            // Collect all InvoiceDetails from all NurseryOrders
            var details = order.NurseryOrders
                .SelectMany(no => no.NurseryOrderDetails)
                .Select(d => new InvoiceDetail
                {
                    ItemName = d.ItemName,
                    UnitPrice = d.UnitPrice,
                    Quantity = d.Quantity,
                    Amount = d.Amount
                })
                .ToList();

            if (!details.Any())
                throw new BadRequestException("Cannot generate invoice details because NurseryOrderDetails are empty");

            // Create single RemainingBalance Invoice for the entire Order
            var invoice = new Invoice
            {
                OrderId = orderId,
                Type = (int)InvoiceTypeEnum.RemainingBalance,
                TotalAmount = order.RemainingAmount ?? 0,
                Status = (int)InvoiceStatusEnum.Pending,
                IssuedDate = DateTime.Now,
                InvoiceDetails = details
            };

            _unitOfWork.InvoiceRepository.PrepareCreate(invoice);
            await _unitOfWork.SaveAsync();

            return MapToDto(invoice);
        }

        private static InvoiceResponseDto MapToDto(Invoice invoice) => new()
        {
            Id = invoice.Id,
            OrderId = invoice.OrderId,
            IssuedDate = invoice.IssuedDate,
            TotalAmount = invoice.TotalAmount,
            Type = invoice.Type,
            TypeName = invoice.Type.HasValue ? ((InvoiceTypeEnum)invoice.Type.Value).ToString() : null,
            Status = invoice.Status,
            StatusName = invoice.Status.HasValue ? ((InvoiceStatusEnum)invoice.Status.Value).ToString() : null,
            Details = invoice.InvoiceDetails.Select(d => new InvoiceDetailResponseDto
            {
                Id = d.Id,
                ItemName = d.ItemName,
                UnitPrice = d.UnitPrice,
                Quantity = d.Quantity,
                Amount = d.Amount
            }).ToList()
        };
    }
}
