using RevolaAgent.Infrastructure.Persistence;
using Xunit;

namespace RevolaAgent.UnitTests;

public class DatabaseConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void MissingConnectionFailsClosed(string? connection)
    {
        Assert.Throws<InvalidOperationException>(() => DatabaseConfiguration.RequireConnectionString(connection));
    }

    [Fact]
    public void ConfiguredConnectionIsPreserved()
    {
        const string connection = "Host=localhost;Database=test";
        Assert.Equal(connection, DatabaseConfiguration.RequireConnectionString(connection));
    }
}
