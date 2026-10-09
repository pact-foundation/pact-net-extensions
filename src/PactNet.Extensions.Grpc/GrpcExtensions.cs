using PactNet.Models;


namespace PactNet.Extensions.Grpc;

/// <summary>
/// Grpc extensions for Pact V4
/// </summary>
public static class GrpcExtensions
{
    /// <summary>
    /// Add synchronous unary gRPC interactions to the pact
    /// </summary>
    /// <param name="pact">Pact details</param>
    /// <param name="port">Port for the mock server. If null, one will be assigned automatically</param>
    /// <param name="host">Host for the mock server</param>
    /// <param name="pluginVersion">Optional protobuf plugin version; null delegates version selection to the plugin framework</param>
    /// <returns>Pact builder</returns>
    public static IGrpcPactBuilderV4 WithGrpcInteractions(this IPactV4 pact, int? port = null, IPAddress host = IPAddress.Loopback, string pluginVersion = null)
    {
        var pluginBuilder = pact.WithSynchronousPluginInteractions("protobuf", pluginVersion, transport: "grpc", port, host);
        var builder = new GrpcPactBuilder(pluginBuilder);
        return builder;
    }
}
