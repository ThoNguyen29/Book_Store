namespace Book_Store.Models.Momo
{
    public class MomoExecuteResponseModel
    {
        public string? OrderId { get; set; }
        public string? Amount { get; set; }
        public string? FullName { get; set; }
        public string? OrderInfo { get; set; }
        public string? ErrorCode { get; set; }
        public string? Message { get; set; }
        public bool SignatureValid { get; set; } = true;
        public int? LocalOrderId { get; set; }

        public bool IsSuccess => string.Equals(ErrorCode, "0", StringComparison.OrdinalIgnoreCase);
    }

}
