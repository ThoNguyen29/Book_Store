using Microsoft.Extensions.Options;
using Book_Store.Models.Momo;
using Book_Store.Models;
using Newtonsoft.Json;
using RestSharp;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Book_Store.Services.Momo
{
    public class MomoService : IMomoService
    {
        private readonly IOptions<MomoOptionModel> _options;
        private readonly ILogger<MomoService> _logger;
        
        public MomoService(IOptions<MomoOptionModel> options, ILogger<MomoService> logger)
        {
            _options = options;
            _logger = logger;
        }

   public async Task<MomoCreatePaymentResponseModel> CreatePaymentMomo(OrderInfoModel model)
{
    model.OrderId ??= DateTime.UtcNow.Ticks.ToString();
    model.OrderInfo = "Khách hàng: " + model.FullName + ". Nội dung: " + model.OrderInfo;

    // xử lý amount - chuyển từ decimal sang long
    if (!decimal.TryParse(model.Amount, out decimal amountDecimal) || amountDecimal < 0)
    {
        throw new FormatException("Amount phải là số dương hợp lệ.");
    }
    long amountValue = Convert.ToInt64(amountDecimal);

    var rawData =
        $"partnerCode={_options.Value.PartnerCode}" +
        $"&accessKey={_options.Value.AccessKey}" +
        $"&requestId={model.OrderId}" +
        $"&amount={amountValue}" +
        $"&orderId={model.OrderId}" +
        $"&orderInfo={model.OrderInfo}" +
        $"&returnUrl={_options.Value.ReturnUrl}" +
        $"&notifyUrl={_options.Value.NotifyUrl}" +
        $"&extraData=";

    _logger.LogInformation($"[MOMO DEBUG] RawData for signature: {rawData}");

    var signature = ComputeHmacSha256(rawData, _options.Value.SecretKey ?? "");
    
    _logger.LogInformation($"[MOMO DEBUG] Computed Signature: {signature}");

    var client = new RestClient(_options.Value.MomoApiUrl ?? "");
    var request = new RestRequest("", Method.Post);

    request.AddHeader("Content-Type", "application/json; charset=UTF-8");

    var requestData = new
    {
        partnerCode = _options.Value.PartnerCode,
        accessKey = _options.Value.AccessKey,
        requestId = model.OrderId,
        amount = amountValue.ToString(),
        orderId = model.OrderId,
        orderInfo = model.OrderInfo,
        returnUrl = _options.Value.ReturnUrl,
        notifyUrl = _options.Value.NotifyUrl,
        requestType = _options.Value.RequestType,
        extraData = "",
        signature = signature
    };

    var jsonRequest = JsonConvert.SerializeObject(requestData);
    _logger.LogInformation($"[MOMO DEBUG] Request JSON: {jsonRequest}");

    request.AddParameter("application/json", jsonRequest, ParameterType.RequestBody);

    var response = await client.ExecuteAsync(request);

    _logger.LogInformation($"[MOMO DEBUG] Response Status: {response.StatusCode}");
    _logger.LogInformation($"[MOMO DEBUG] Response Content: {response.Content}");
    _logger.LogInformation($"[MOMO DEBUG] Response Success: {response.IsSuccessful}");

    if (!response.IsSuccessful)
    {
        _logger.LogError($"[MOMO ERROR] API Error: {response.ErrorException?.Message}");
        return new MomoCreatePaymentResponseModel();
    }

    var result = JsonConvert.DeserializeObject<MomoCreatePaymentResponseModel>(response.Content ?? "");
    _logger.LogInformation($"[MOMO DEBUG] Parsed Result - PayUrl: {result?.PayUrl}, ErrorCode: {result?.ErrorCode}");
    
    return result ?? new MomoCreatePaymentResponseModel();
}
        public MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection)
        {
            try
            {
                var partnerCode = collection.FirstOrDefault(s => s.Key == "partnerCode").Value.ToString();
                var accessKey = collection.FirstOrDefault(s => s.Key == "accessKey").Value.ToString();
                var requestId = collection.FirstOrDefault(s => s.Key == "requestId").Value.ToString();
                var amount = collection.FirstOrDefault(s => s.Key == "amount").Value.ToString();
                var orderId = collection.FirstOrDefault(s => s.Key == "orderId").Value.ToString();
                var orderInfo = collection.FirstOrDefault(s => s.Key == "orderInfo").Value.ToString();
                var requestType = collection.FirstOrDefault(s => s.Key == "requestType").Value.ToString();
                var orderType = collection.FirstOrDefault(s => s.Key == "orderType").Value.ToString();
                var transId = collection.FirstOrDefault(s => s.Key == "transId").Value.ToString();
                var responseTime = collection.FirstOrDefault(s => s.Key == "responseTime").Value.ToString();
                var errorCode = collection.FirstOrDefault(s => s.Key == "errorCode").Value.ToString();
                var message = collection.FirstOrDefault(s => s.Key == "message").Value.ToString();
                var localMessage = collection.FirstOrDefault(s => s.Key == "localMessage").Value.ToString();
                var payType = collection.FirstOrDefault(s => s.Key == "payType").Value.ToString();
                var extraData = collection.FirstOrDefault(s => s.Key == "extraData").Value.ToString();
                var signature = collection.FirstOrDefault(s => s.Key == "signature").Value.ToString();

                var result = new MomoExecuteResponseModel
                {
                    Amount = amount,
                    OrderId = orderId,
                    OrderInfo = orderInfo,
                    ErrorCode = errorCode,
                    Message = !string.IsNullOrWhiteSpace(localMessage) ? localMessage : message,
                    SignatureValid = true
                };

                var candidateRawData = BuildSignatureCandidates(
                    partnerCode,
                    accessKey,
                    requestId,
                    amount,
                    orderId,
                    orderInfo,
                    requestType,
                    orderType,
                    transId,
                    responseTime,
                    errorCode,
                    message,
                    localMessage,
                    payType,
                    extraData);

                var signatureMatched = candidateRawData.Any(raw =>
                    string.Equals(
                        signature,
                        ComputeHmacSha256(raw, _options.Value.SecretKey ?? ""),
                        StringComparison.OrdinalIgnoreCase));

                if (!signatureMatched)
                {
                    var computedSignatures = candidateRawData
                        .Select(raw => ComputeHmacSha256(raw, _options.Value.SecretKey ?? ""))
                        .ToList();

                    _logger.LogError(
                        "Signature mismatch. Received: {ReceivedSignature}, Computed candidates: {ComputedSignatures}",
                        signature,
                        string.Join(", ", computedSignatures));
                    result.SignatureValid = false;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"PaymentExecuteAsync Error: {ex.Message}");
                return new MomoExecuteResponseModel
                {
                    ErrorCode = "EXCEPTION",
                    Message = "Khong the xu ly ket qua thanh toan."
                };
            }
        }

        private static List<string> BuildSignatureCandidates(
            string partnerCode,
            string accessKey,
            string requestId,
            string amount,
            string orderId,
            string orderInfo,
            string requestType,
            string orderType,
            string transId,
            string responseTime,
            string errorCode,
            string message,
            string localMessage,
            string payType,
            string extraData)
        {
            var normalizedOrderType = !string.IsNullOrWhiteSpace(orderType) ? orderType : requestType;

            return new List<string>
            {
                $"partnerCode={partnerCode}" +
                $"&accessKey={accessKey}" +
                $"&requestId={requestId}" +
                $"&amount={amount}" +
                $"&orderId={orderId}" +
                $"&orderInfo={orderInfo}" +
                $"&transId={transId}" +
                $"&responseTime={responseTime}" +
                $"&errorCode={errorCode}" +
                $"&message={message}",

                $"partnerCode={partnerCode}" +
                $"&accessKey={accessKey}" +
                $"&requestId={requestId}" +
                $"&amount={amount}" +
                $"&orderId={orderId}" +
                $"&orderInfo={orderInfo}" +
                $"&orderType={normalizedOrderType}" +
                $"&transId={transId}" +
                $"&message={message}" +
                $"&localMessage={localMessage}" +
                $"&responseTime={responseTime}" +
                $"&errorCode={errorCode}" +
                $"&payType={payType}" +
                $"&extraData={extraData}",

                $"accessKey={accessKey}" +
                $"&amount={amount}" +
                $"&extraData={extraData}" +
                $"&message={message}" +
                $"&orderId={orderId}" +
                $"&orderInfo={orderInfo}" +
                $"&orderType={normalizedOrderType}" +
                $"&partnerCode={partnerCode}" +
                $"&payType={payType}" +
                $"&requestId={requestId}" +
                $"&responseTime={responseTime}" +
                $"&errorCode={errorCode}" +
                $"&transId={transId}"
            };
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            byte[] hashBytes;

            using (var hmac = new HMACSHA256(keyBytes))
            {
                hashBytes = hmac.ComputeHash(messageBytes);
            }

            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            return hashString;
        }
 }
}
