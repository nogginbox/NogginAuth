using Microsoft.Extensions.Options;
using Noggin.NetCoreAuth.Config;
using Noggin.NetCoreAuth.Exceptions;
using Noggin.NetCoreAuth.Providers;
using NSubstitute;
using System.Collections.Generic;
using Xunit;

namespace Noggin.NetCoreAuth.Tests.Providers;


public class ProviderFactoryTests
{
    [Fact]
    public void ConfiguringNoneExistentProviderThrowsException()
    {
        // Arrange
        var options = Substitute.For<IOptions<AuthConfigSection>>();
        options.Value.Returns(
            new AuthConfigSection
            {
                Providers = new List<ProviderConfig>
                {
                    // Configuration for none existent provider
                    new ProviderConfig
                    {
                        Name = "made-up",
                        CallbackTemplate = "cbt1",
                        RedirectTemplate = "rt1"
                    }
                }
            }
        );

        // Act / Assert
        Assert.Throws<NogginNetCoreConfigException>(() => new ProviderFactory(options));
    }

    [Fact]
    public void GettingNoneConfiguredProviderThrowsException()
    {
        // Arrange
        var options = Substitute.For<IOptions<AuthConfigSection>>();
        options.Value.Returns(new AuthConfigSection());
        var factory = new ProviderFactory(options);

        // Act / Assert
        Assert.Throws<NogginNetCoreConfigException>(() => factory.Get("github"));
    }

    [Fact]
    public void GettingNoneExistentProviderThrowsException()
    {
        // Arrange
        var options = Substitute.For<IOptions<AuthConfigSection>>();
        options.Value.Returns(new AuthConfigSection());
        var factory = new ProviderFactory(options);

        // Act / Assert
        Assert.Throws<NogginNetCoreConfigException>(() => factory.Get("made-up"));
    }

    [Fact]
    public void GettingConfiguredProviderReturnsProvider()
    {
        // Arrange
        var options = Substitute.For<IOptions<AuthConfigSection>>();
        options.Value.Returns(
            new AuthConfigSection
            {
                DefaultCallbackTemplate = "default-{provider}-cb",
                DefaultRedirectTemplate = "default-{provider}-r",
                Providers = new List<ProviderConfig>
                {
                    new ProviderConfig
                    {
                        Name = "github",
                        CallbackTemplate = "specific-github-cb",
                        RedirectTemplate = "specific-github-r",
                        Api = new ApiConfig
                        {
                            PrivateKey = "ssshhhhhhh",
                            PublicKey = "hello"
                        }
                    }
                }
            }
        );
        var factory = new ProviderFactory(options);

        // Act 
        var provider = factory.Get("github");

        // Assert
        Assert.Equal("github", provider.Name);
        Assert.Equal("specific-github-cb", provider.CallbackTemplate);
        Assert.Equal("specific-github-r", provider.RedirectTemplate);
    }

    [Fact]
    public void GettingConfiguredProviderWithOnlyDefaultCallbacksReturnsProvider()
    {
        // Arrange
        var options = Substitute.For<IOptions<AuthConfigSection>>();
        options.Value.Returns(
            new AuthConfigSection
            {
                DefaultCallbackTemplate = "default-{provider}-cb",
                DefaultRedirectTemplate = "default-{provider}-r",
                Providers = new List<ProviderConfig>
                {
                    new ProviderConfig
                    {
                        Name = "facebook",
                        Api = new ApiConfig
                        {
                            PrivateKey = "ssshhhhhhh",
                            PublicKey = "hello"
                        }
                    }
                }
            }
        );
        var factory = new ProviderFactory(options);

        // Act 
        var provider = factory.Get("facebook");

        // Assert
        Assert.Equal("facebook", provider.Name);
        Assert.Equal("default-facebook-cb", provider.CallbackTemplate);
        Assert.Equal("default-facebook-r", provider.RedirectTemplate);
    }
}
