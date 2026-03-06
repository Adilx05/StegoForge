using StegoForge.Infrastructure;
using Xunit;

namespace StegoForge.Tests.Integration;

public sealed class BaselineIntegrationTests
{
    [Fact]
    public void InfrastructureMarker_CanBeConstructed()
    {
        var marker = new InfrastructureMarker();
        Assert.NotNull(marker);
    }
}
