using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using Noggin.NetCoreAuth.Config;

namespace Noggin.NetCoreAuth.Mvc.TagHelpers
{
    public class AuthLinkTagHelper : TagHelper
    {
        private readonly AuthConfigSection _config;

        public AuthLinkTagHelper(IOptions<AuthConfigSection> config)
        {
            _config = config.Value;
        }

        public string Provider { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {

            output.TagName = "a";
            output.Attributes.SetAttribute("href", $"/test/{Provider}");
        }
    }
}
