using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Book_Store.Models;
using Book_Store.Services.Momo;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Book_Store.Models.Momo;
namespace Book_Store.Controllers
{

    public class PaymentController : Controller
    {
         private IMomoService _momoService;
       public PaymentController(IMomoService momoService)
        {
            _momoService = momoService;
        }
        [HttpPost]
        public async Task<IActionResult> CreatePaymentMomo(OrderInfoModel model)
        {
            var response = await _momoService.CreatePaymentMomo(model);
            if (response?.PayUrl == null)
            {
                return BadRequest("Unable to create payment. Please try again.");
            }
            return Redirect(response.PayUrl);
        }
        [HttpGet]
        public IActionResult PaymentCallBack()
        {
            var response = _momoService.PaymentExecuteAsync(HttpContext. Request.Query);
          return View(response);
        }
    }
    
}