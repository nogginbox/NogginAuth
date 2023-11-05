using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Http;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Model;
using Noggin.NetCoreAuth.Providers.GitHub.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Noggin.NetCoreAuth.Providers.GitHub;

/// <summary>
/// GitHub Login Provider
/// </summary>
/// <remarks>
/// reference: https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps
/// </remarks>
internal class GitHubProvider : Provider
{
    private const string _githubApiUrl = "https://api.github.com/";
    private const string _githubOAuthUrl = "https://github.com/login/oauth";

    private readonly ApiConfig _apiDetails;


    internal GitHubProvider(ProviderConfig config, string defaultRedirectTemplate, string defaultCallbackTemplate) : base(config, defaultRedirectTemplate, defaultCallbackTemplate)
    {
        _apiDetails = config.Api;
    }

    internal override Task<(string url, string secret)> GenerateStartRequestUrl(HttpRequest request)
    {
        var callback = CreateCallbackUrl(request);
        var url = _githubOAuthUrl
			.AppendPathSegment("authorize")
            .SetQueryParam("scope", "user:email read:user")
			.SetQueryParam("client_id", _apiDetails.PublicKey)
			.SetQueryParam("redirect_uri", callback);

		// This implementation of method does not need to be async, so convert result to task
		return Task.FromResult((url.ToString(), string.Empty));
	}

    internal override async Task<UserInformation> AuthenticateUser(HttpRequest request, string state)
    {
		var code = GetCode(request.Query);
        var accessToken = await RetrieveAccessToken(code);
        var userInfo = await RetrieveUserInformationAsync(accessToken);

        return userInfo;
    }

	private static void CheckValid(AccessTokenResult tokenResult)
	{
		if (tokenResult.Error != null)
		{
			throw new Exception($"{tokenResult.Error} - {tokenResult.ErrorDescription}");
		}
	}

    private static string GetCode(IQueryCollection queryStringParameters)
    {
		if (queryStringParameters == null || !queryStringParameters.Any())
		{
			throw new ArgumentOutOfRangeException(nameof(queryStringParameters));
		}

		// Maybe we have an error?
		if(queryStringParameters.ContainsKey("error"))
		{
        var errorMessage = $"{queryStringParameters["error"]}: {queryStringParameters["error_description"]}";

			throw new NogginNetCoreAuthException(errorMessage);
		}

		if (!queryStringParameters.ContainsKey("code"))
		{
			throw new NogginNetCoreAuthException("No auth code returned by GitHub");
		}

		return queryStringParameters["code"].ToString();
	}

	private async Task<string> RetrieveAccessToken(string authorizationCode)
	{
		if (string.IsNullOrEmpty(authorizationCode))
		{
			throw new ArgumentNullException(nameof(authorizationCode));
		}

		var request = _githubOAuthUrl
			.AppendPathSegment("access_token")
            .WithHeader("User-Agent", "NogginAuth")
            .WithHeader("accept", "application/json");

		var form = new
		{
			client_id = _apiDetails.PublicKey,
			client_secret = _apiDetails.PrivateKey,
			code = authorizationCode
		};

        try
		{
			var tokenResponse = await request.PostUrlEncodedAsync(form);
			var data = await tokenResponse.GetJsonAsync<AccessTokenResult>();
			CheckValid(data);
			return data?.AccessToken ?? throw new Exception(data?.ErrorDescription );
		}
		catch(Exception ex)
		{
			throw new NogginNetCoreAuthException($"Failed to get access token from GitHub: {ex.Message}", ex);
		}
    }

    /// <summary>
    /// Gets user information from Github API
    /// </summary>
    /// <param name="authToken">Github token</param>
    /// <exception cref="NogginNetCoreAuthException"></exception>
    /// <remarks>Github API ref: https://docs.github.com/en/rest/users/users</remarks>
    protected static async Task<UserInformation> RetrieveUserInformationAsync(string authToken)
	{
		UserResult? user;

        var request = _githubApiUrl
			.AppendPathSegment("user")
			.WithOAuthBearerToken(authToken)
			//.WithHeader("Authorization", $"Bearer {authToken}")
			.WithHeader("Accept", "application/json")
			.WithHeader("User-Agent", "NogginAuth")
			.WithHeader("X-GitHub-Api-Version", "2022-11-28");

		try
		{
			user = await request.GetJsonAsync<UserResult>();
		}
		catch (Exception ex)
		{
			throw
				new NogginNetCoreAuthException($"Failed to get user from the GitHub: {ex.Message}", ex);
		}

		if (user == null || user?.Message != null)
		{
            var errorMessage = "GitHub: Failed to get user information";
            if (user?.Message != null) errorMessage += $" - {user.Message}";

            throw new NogginNetCoreAuthException(errorMessage);
		}

		var userInformation = new UserInformation
		{
			Id = user!.Id.ToString(),
			Name = user.Name,
			Email = user.Email,
			UserName = user.Login,
			Picture = user.AvatarUrl
		};

		return userInformation;
	}

}