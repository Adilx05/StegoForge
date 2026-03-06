using StegoForge.Application;
using Xunit;

namespace StegoForge.Tests.Unit;

public sealed class BaselineTests
{
    [Fact]
    public void ApplicationMarker_CanBeConstructed()
    {
        var marker = new ApplicationMarker();
        Assert.NotNull(marker);
    }
}
