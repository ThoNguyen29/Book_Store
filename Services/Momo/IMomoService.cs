using Microsoft.AspNetCore.Http;
using Book_Store.Models.Momo;
using Book_Store.Models;

namespace Book_Store.Services.Momo
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponseModel> CreatePaymentMomo(OrderInfoModel model);
        MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection);
    }
}