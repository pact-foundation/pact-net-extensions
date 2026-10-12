using System;
using System.IO;
using Avro;
using Avro.Generic;
using Avro.IO;

namespace AvroSample.Tests;

internal static class AvroCodec
{
    internal static readonly string SchemaPath = Path.Combine(AppContext.BaseDirectory, "user.avsc");
    private static readonly RecordSchema Schema = (RecordSchema)global::Avro.Schema.Parse(File.ReadAllText(SchemaPath));

    internal static byte[] Encode(long id, string username)
    {
        var record = new GenericRecord(Schema);
        record.Add("id", id);
        record.Add("username", username);
        using var stream = new MemoryStream();
        new GenericWriter<GenericRecord>(Schema).Write(record, new BinaryEncoder(stream));
        return stream.ToArray();
    }

    internal static GenericRecord Decode(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return new GenericReader<GenericRecord>(Schema, Schema).Read(null, new BinaryDecoder(stream));
    }
}