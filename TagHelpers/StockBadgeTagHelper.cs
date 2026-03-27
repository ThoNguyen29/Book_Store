using System.Text;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Book_Store.TagHelpers
{
    [HtmlTargetElement("stock-badge")]
    public class StockBadgeTagHelper : TagHelper
    {
        [HtmlAttributeName("stock")]
        public int Stock { get; set; }

        [HtmlAttributeName("low-stock-threshold")]
        public int LowStockThreshold { get; set; } = 5;

        [HtmlAttributeName("in-stock-text")]
        public string InStockText { get; set; } = "Còn hàng";

        [HtmlAttributeName("incoming-text")]
        public string IncomingText { get; set; } = "Sắp về";

        [HtmlAttributeName("out-of-stock-text")]
        public string OutOfStockText { get; set; } = "Hết hàng";

        [HtmlAttributeName("show-quantity")]
        public bool ShowQuantity { get; set; }

        [HtmlAttributeName("class")]
        public string? CssClass { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "span";
            output.TagMode = TagMode.StartTagAndEndTag;

            output.Attributes.RemoveAll("stock");
            output.Attributes.RemoveAll("low-stock-threshold");
            output.Attributes.RemoveAll("in-stock-text");
            output.Attributes.RemoveAll("incoming-text");
            output.Attributes.RemoveAll("out-of-stock-text");
            output.Attributes.RemoveAll("show-quantity");
            output.Attributes.RemoveAll("class");

            var normalizedThreshold = LowStockThreshold < 1 ? 1 : LowStockThreshold;
            var (stateClass, label) = ResolveState(normalizedThreshold);

            var classBuilder = new StringBuilder("stock-badge ")
                .Append(stateClass);

            if (!string.IsNullOrWhiteSpace(CssClass))
            {
                classBuilder.Append(' ').Append(CssClass);
            }

            output.Attributes.SetAttribute("class", classBuilder.ToString());
            output.Content.SetContent(BuildLabel(label));
        }

        private (string StateClass, string Label) ResolveState(int threshold)
        {
            if (Stock > threshold)
            {
                return ("stock-badge--in", InStockText);
            }

            if (Stock > 0)
            {
                return ("stock-badge--incoming", IncomingText);
            }

            return ("stock-badge--out", OutOfStockText);
        }

        private string BuildLabel(string baseLabel)
        {
            var label = baseLabel;
            if (ShowQuantity && Stock > 0)
            {
                label += $" ({Stock})";
            }

            return label;
        }
    }
}
