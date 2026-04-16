using Microsoft.AspNetCore.Http;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IPaymentService
    {
        Task<CreatePaymentUrlResponseDto> CreatePaymentUrlAsync(int userId, CreatePaymentRequestDto request, HttpContext httpContext);
        Task<CreatePaymentUrlResponseDto> ContinuePaymentByInvoiceAsync(int userId, int invoiceId, HttpContext httpContext);
        Task<CreatePaymentUrlResponseDto> RetryPaymentAsync(int userId, int paymentId, HttpContext httpContext);
        Task<PaymentResponse> ProcessVnpayCallbackAsync(IQueryCollection queryParams);
        Task<VnpayIpnResponseDto> ProcessVnpayIpnAsync(IQueryCollection queryParams);
        Task<VnpayIpnResponseDto> ProcessVnpaySecondIpnAsync(IQueryCollection queryParams);
    }
}
