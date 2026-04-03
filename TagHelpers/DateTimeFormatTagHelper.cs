using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Book_Store.TagHelpers
{
    [HtmlTargetElement("date-time")]
    public class DateTimeFormatTagHelper : TagHelper
    {
        [HtmlAttributeName("value")]
        public DateTime? Value { get; set; }

        [HtmlAttributeName("format")]
        public string Format { get; set; } = "dd/MM/yyyy HH:mm";

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
            output.Attributes.RemoveAll("null-text");
            output.Attributes.RemoveAll("class");

            if (!string.IsNullOrWhiteSpace(CssClass))
            {
                output.Attributes.SetAttribute("class", CssClass);
            }

            output.Content.SetContent(Value.HasValue ? Value.Value.ToString(Format) : NullText);
        }
    }
}
