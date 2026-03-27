using System.Text.Encodings.Web;
using Book_Store.Models;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Book_Store.TagHelpers
{
    [HtmlTargetElement("category-dropdown")]
    public class CategoryDropdownTagHelper : TagHelper
    {
        [HtmlAttributeName("items")]
        public IEnumerable<Category>? Items { get; set; }

        [HtmlAttributeName("selected-value")]
        public int? SelectedValue { get; set; }

        [HtmlAttributeName("name")]
        public string Name { get; set; } = "categoryId";

        [HtmlAttributeName("id")]
        public string? Id { get; set; }

        [HtmlAttributeName("class")]
        public string CssClass { get; set; } = "sb-select";

        [HtmlAttributeName("include-all-option")]
        public bool IncludeAllOption { get; set; } = true;

        [HtmlAttributeName("all-option-text")]
        public string AllOptionText { get; set; } = "-- Tất cả --";

        [HtmlAttributeName("all-option-value")]
        public string AllOptionValue { get; set; } = "";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "select";
            output.TagMode = TagMode.StartTagAndEndTag;

            output.Attributes.RemoveAll("items");
            output.Attributes.RemoveAll("selected-value");
            output.Attributes.RemoveAll("include-all-option");
            output.Attributes.RemoveAll("all-option-text");
            output.Attributes.RemoveAll("all-option-value");
            output.Attributes.RemoveAll("name");
            output.Attributes.RemoveAll("id");
            output.Attributes.RemoveAll("class");

            output.Attributes.SetAttribute("name", Name);
            output.Attributes.SetAttribute("id", string.IsNullOrWhiteSpace(Id) ? Name : Id);

            if (!string.IsNullOrWhiteSpace(CssClass))
            {
                output.Attributes.SetAttribute("class", CssClass);
            }

            output.Content.Clear();

            if (IncludeAllOption)
            {
                var selected = SelectedValue == null ? " selected=\"selected\"" : string.Empty;
                output.Content.AppendHtml($"<option value=\"{HtmlEncoder.Default.Encode(AllOptionValue)}\"{selected}>{HtmlEncoder.Default.Encode(AllOptionText)}</option>");
            }

            if (Items == null)
            {
                return;
            }

            foreach (var item in Items)
            {
                if (item == null)
                {
                    continue;
                }

                var selected = SelectedValue == item.CategoryID ? " selected=\"selected\"" : string.Empty;
                output.Content.AppendHtml($"<option value=\"{item.CategoryID}\"{selected}>{HtmlEncoder.Default.Encode(item.Name ?? string.Empty)}</option>");
            }
        }
    }
}
