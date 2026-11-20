using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Model;
using Noggin.NetCoreAuth.Providers;
using System.Threading.Tasks;

namespace Noggin.NetCoreAuth.Tests;

/// <summary>
/// A provider that doesn't call any API, for testing the parts of the login flow shared by all providers
/// </summary>
internal class FakeProvider : Provider
{
    public const string ProviderName = "fake";

    public FakeProvider()
        : base(new ProviderConfig { Name = ProviderName, Api = new ApiConfig { PublicKey = "public", PrivateKey = "private" } },
              "auth/{provider}", "auth/callback/{provider}", NullLogger.Instance)
    {
    }

    /// <summary>The auth code passed to RetrieveAccessToken, or null if it wasn't called</summary>
    public string? CodeUsed { get; set; }

    private protected override string CreateAuthoriseUrl(HttpRequest request, string state)
    {
        return $"https://provider.example/authorize?state={state}";
    }

    private protected override Task<string> RetrieveAccessToken(string authorizationCode, string callbackUrl)
    {
        CodeUsed = authorizationCode;
        return Task.FromResult("access-token");
    }

    private protected override Task<UserInformation> RetrieveUserInformationAsync(string accessToken)
    {
        return Task.FromResult(new UserInformation { Id = "user-id" });
    }
}
