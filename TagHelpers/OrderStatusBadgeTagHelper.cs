using Book_Store.Models;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Book_Store.TagHelpers
{
    [HtmlTargetElement("order-status-badge")]
    public class OrderStatusBadgeTagHelper : TagHelper
    {
        [HtmlAttributeName("status")]
        public object? Status { get; set; }

        [HtmlAttributeName("class")]
        public string? CssClass { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "span";
            output.TagMode = TagMode.StartTagAndEndTag;

            output.Attributes.RemoveAll("status");
            output.Attributes.RemoveAll("class");

            var statusText = ResolveStatusKey(Status);
            var (badgeClass, label) = ResolveDisplay(statusText);

            var finalClass = string.IsNullOrWhiteSpace(CssClass)
                ? $"badge {badgeClass}"
                : $"badge {badgeClass} {CssClass}";

            output.Attributes.SetAttribute("class", finalClass);
            output.Content.SetContent(label);
        }

        private static string ResolveStatusKey(object? status)
        {
            if (status is null)
            {
                return string.Empty;
            }

            if (status is OrderStatus orderStatus)
            {
                return orderStatus.ToString();
            }

            return status.ToString() ?? string.Empty;
        }

        private static (string CssClass, string Label) ResolveDisplay(string status)
        {
            return status.ToLowerInvariant() switch
            {
                "pending" => ("bg-secondary", "Chờ xác nhận"),
                "confirmed" => ("bg-info text-dark", "Đã xác nhận"),
                "shipping" => ("bg-primary", "Đang giao"),
                "completed" => ("bg-success", "Hoàn thành"),
                "cancelled" => ("bg-danger", "Đã hủy"),
                _ => ("bg-secondary", string.IsNullOrWhiteSpace(status) ? "Không xác định" : status)
            };
        }
    }
}
