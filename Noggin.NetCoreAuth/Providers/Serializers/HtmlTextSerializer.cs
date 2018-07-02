using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Noggin.NetCoreAuth.Extensions;
using RestSharp;
using RestSharp.Deserializers;

namespace Noggin.NetCoreAuth.Providers.Serializers
{
    internal class TwitterHtmlTextSerializer : IDeserializer
    {
        public string RootElement { get; set; }
        public string Namespace { get; set; }
        public string DateFormat { get; set; }

        public T Deserialize<T>(IRestResponse response)
        {
            var bits = QueryHelpers.ParseQuery(response.Content);
            var thing = bits.ToObject<T, StringValues>();
            return thing;
        }

        public static IDeserializer CreateDefault()
        {
            return new TwitterHtmlTextSerializer();
        }
    }
}
