using System.Globalization;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Book_Store.TagHelpers
{
    [HtmlTargetElement("money")]
    public class MoneyTagHelper : TagHelper
    {
        [HtmlAttributeName("value")]
        public object? Value { get; set; }

        [HtmlAttributeName("format")]
        public string Format { get; set; } = "N0";

        [HtmlAttributeName("currency")]
        public string Currency { get; set; } = "d";

        [HtmlAttributeName("show-currency")]
        public bool ShowCurrency { get; set; } = true;

        [HtmlAttributeName("null-text")]
        public string NullText { get; set; } = "-";

        [HtmlAttributeName("class")]
        public string? CssClass { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "span";
            output.TagMode = TagMode.StartTagAndEndTag;

            output.Attributes.RemoveAll("value");
            output.Attributes.RemoveAll("format");
            output.Attributes.RemoveAll("currency");
            output.Attributes.RemoveAll("show-currency");
            output.Attributes.RemoveAll("null-text");
            output.Attributes.RemoveAll("class");

            if (!string.IsNullOrWhiteSpace(CssClass))
            {
                output.Attributes.SetAttribute("class", CssClass);
            }

            if (!TryParseDecimal(Value, out var amount))
            {
                output.Content.SetContent(NullText);
                return;
            }

            var formatted = amount.ToString(Format, CultureInfo.CurrentCulture);
            output.Content.SetContent(ShowCurrency ? $"{formatted} {Currency}" : formatted);
        }

        private static bool TryParseDecimal(object? value, out decimal result)
        {
            result = 0;
            if (value is null)
            {
                return false;
            }

            switch (value)
            {
                case decimal decimalValue:
                    result = decimalValue;
                    return true;
                case double doubleValue:
                    result = Convert.ToDecimal(doubleValue);
                    return true;
                case float floatValue:
                    result = Convert.ToDecimal(floatValue);
                    return true;
                case int intValue:
                    result = intValue;
                    return true;
                case long longValue:
                    result = longValue;
                    return true;
                case short shortValue:
                    result = shortValue;
                    return true;
                case uint uintValue:
                    result = uintValue;
                    return true;
                case ulong ulongValue:
                    result = ulongValue;
                    return true;
                case string stringValue:
                    return decimal.TryParse(stringValue, out result);
                default:
                    return decimal.TryParse(Convert.ToString(value, CultureInfo.CurrentCulture), out result);
            }
        }
    }
}
