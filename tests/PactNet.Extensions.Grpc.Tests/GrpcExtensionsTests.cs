using Xunit;

namespace PactNet.Extensions.Grpc.Tests;

public class GrpcExtensionsTests
{
    [Fact]
    public void WithGrpcInteractions_PluginVersionIsOptionalAndDefaultsToNull()
    {
        var parameters = typeof(GrpcExtensions).GetMethod(nameof(GrpcExtensions.WithGrpcInteractions)).GetParameters();
        var version = parameters[parameters.Length - 1];
        Assert.Equal("pluginVersion", version.Name);
        Assert.True(version.IsOptional);
        Assert.Null(version.DefaultValue);
    }
}