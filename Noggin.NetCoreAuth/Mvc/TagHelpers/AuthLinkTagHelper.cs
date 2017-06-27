using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Noggin.NetCoreAuth.Providers;


namespace Noggin.NetCoreAuth.Mvc.TagHelpers
{
    public class AuthLinkTagHelper : TagHelper
    {
        private readonly IProviderFactory _providerFactory;
        private readonly IHtmlGenerator _generator;

        public AuthLinkTagHelper(IProviderFactory providerFactory, IHtmlGenerator generator)
        {
            _providerFactory = providerFactory;
            _generator = generator;
        }

        public string Provider { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var provider = _providerFactory.Get(Provider);

            var routeName = $"NogginAuth_Redirect_{Provider}";
            var tagBuilder = _generator.GenerateRouteLink(ViewContext, "Provider link", routeName, null, null, null, null, null);

            output.TagName = "a";
            //output.MergeAttributes(tagBuilder);
            output.Attributes.SetAttribute("href", tagBuilder.Attributes["href"]);
        }
    }
}
