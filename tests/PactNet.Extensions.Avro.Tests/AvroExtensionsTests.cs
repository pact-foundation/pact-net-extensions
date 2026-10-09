using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using Xunit;

namespace PactNet.Extensions.Avro.Tests;

public class AvroExtensionsTests
{
    [Fact]
    public void Response_TranslatesSchemaRecordAndFieldsWithoutMutatingInput()
    {
        var response = new Mock<IHttpPluginResponseBuilderV4>();
        var fields = new Dictionary<string, object> { ["id"] = "matching(integer, 1)", ["username"] = "notEmpty('user')" };
        Dictionary<string, object> contents = null;
        response.Setup(value => value.WithContent("avro/binary", It.IsAny<Dictionary<string, object>>()))
            .Callback<string, Dictionary<string, object>>((_, value) => contents = value);
        response.Object.WithAvroBody("user.avsc", "User", fields);
        Assert.Equal(Path.GetFullPath("user.avsc"), contents["pact:avro"]);
        Assert.Equal("User", contents["pact:record-name"]);
        Assert.Equal(fields["id"], contents["id"]);
        Assert.Equal(fields["username"], contents["username"]);
        Assert.Equal(2, fields.Count);
        response.Verify(value => value.WithContent("avro/binary", It.IsAny<Dictionary<string, object>>()), Times.Once);
    }

    [Fact]
    public void Request_ReturnsUnderlyingBuilder()
    {
        var request = new Mock<IHttpPluginRequestBuilderV4>();
        request.Setup(value => value.WithContent("avro/binary", It.IsAny<Dictionary<string, object>>())).Returns(request.Object);
        Assert.Same(request.Object, request.Object.WithAvroBody("user.avsc", "User", new Dictionary<string, object>()));
    }

    [Theory]
    [InlineData("", "User")]
    [InlineData("user.avsc", " ")]
    public void InvalidConfiguration_IsRejected(string schemaPath, string recordName)
    {
        var response = new Mock<IHttpPluginResponseBuilderV4>();
        Assert.Throws<ArgumentException>(() => response.Object.WithAvroBody(schemaPath, recordName, new Dictionary<string, object>()));
        response.VerifyNoOtherCalls();
    }

    [Fact]
    public void ReservedFieldsAndNullContents_AreRejected()
    {
        var response = new Mock<IHttpPluginResponseBuilderV4>();
        Assert.Throws<ArgumentException>(() => response.Object.WithAvroBody("user.avsc", "User",
            new Dictionary<string, object> { ["pact:avro"] = "wrong.avsc" }));
        Assert.Throws<ArgumentNullException>(() => response.Object.WithAvroBody("user.avsc", "User", null));
        response.VerifyNoOtherCalls();
    }

    [Fact]
    public void AsyncMessage_MapsContentsAndReturnsTheVerifier()
    {
        var message = new Mock<IAsynchronousPluginMessageBuilderV4>();
        var verifier = new Mock<IConfiguredPluginMessageVerifier>();
        message.Setup(value => value.WithContent("avro/binary", It.Is<Dictionary<string, object>>(contents =>
            (string)contents["pact:record-name"] == "User" && (string)contents["username"] == "alice")))
            .Returns(verifier.Object);
        Assert.Same(verifier.Object, message.Object.WithAvroContent("user.avsc", "User",
            new Dictionary<string, object> { ["username"] = "alice" }));
    }

    [Fact]
    public void PluginVersion_DefaultsToFrameworkSelection()
    {
        var parameters = typeof(AvroExtensions).GetMethod(nameof(AvroExtensions.WithAvroInteractions)).GetParameters();
        Assert.Null(parameters[parameters.Length - 1].DefaultValue);
    }
}