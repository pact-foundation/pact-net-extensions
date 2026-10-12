using System;
using System.Collections.Generic;
using System.IO;
using PactNet.Models;

namespace PactNet.Extensions.Avro;

/// <summary>Avro binary contents over HTTP using the hominux Avro plugin.</summary>
public static class AvroExtensions
{
    /// <summary>Create HTTP interactions using the Avro content plugin.</summary>
    /// <param name="pact">Pact details.</param>
    /// <param name="port">Optional mock server port.</param>
    /// <param name="host">Mock server host.</param>
    /// <param name="pluginVersion">Optional native plugin version.</param>
    /// <returns>An HTTP plugin builder owned by PactNet.</returns>
    public static IHttpPluginPactBuilderV4 WithAvroInteractions(this IPactV4 pact,
        int? port = null, IPAddress host = IPAddress.Loopback, string pluginVersion = null)
        => pact.WithHttpPluginInteractions("avro", pluginVersion, port, host);

    /// <summary>Configure an Avro request body.</summary>
    /// <param name="request">HTTP plugin request builder.</param>
    /// <param name="schemaPath">Path to an Avro schema file.</param>
    /// <param name="recordName">Record name within that schema.</param>
    /// <param name="fields">Record field values or plugin matching expressions.</param>
    /// <returns>The request builder.</returns>
    public static IHttpPluginRequestBuilderV4 WithAvroBody(this IHttpPluginRequestBuilderV4 request,
        string schemaPath, string recordName, IDictionary<string, object> fields)
        => request.WithContent("avro/binary", Contents(schemaPath, recordName, fields));

    /// <summary>Configure an Avro response body, completing the interaction.</summary>
    /// <param name="response">HTTP plugin response builder.</param>
    /// <param name="schemaPath">Path to an Avro schema file.</param>
    /// <param name="recordName">Record name within that schema.</param>
    /// <param name="fields">Record field values or plugin matching expressions.</param>
    public static void WithAvroBody(this IHttpPluginResponseBuilderV4 response,
        string schemaPath, string recordName, IDictionary<string, object> fields)
        => response.WithContent("avro/binary", Contents(schemaPath, recordName, fields));

    private static Dictionary<string, object> Contents(string schemaPath, string recordName,
        IDictionary<string, object> fields)
    {
        if (string.IsNullOrWhiteSpace(schemaPath)) throw new ArgumentException("An Avro schema path is required.", nameof(schemaPath));
        if (string.IsNullOrWhiteSpace(recordName)) throw new ArgumentException("An Avro record name is required.", nameof(recordName));
        if (fields == null) throw new ArgumentNullException(nameof(fields));
        var contents = new Dictionary<string, object>
        {
            ["pact:avro"] = Path.GetFullPath(schemaPath),
            ["pact:record-name"] = recordName
        };
        foreach (var field in fields)
        {
            if (field.Key.StartsWith("pact:", StringComparison.Ordinal))
                throw new ArgumentException("Record fields must not override plugin configuration.", nameof(fields));
            contents.Add(field.Key, field.Value);
        }
        return contents;
    }
}