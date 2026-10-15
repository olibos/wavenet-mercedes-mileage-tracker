namespace MercedesMileageTracker;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Confluent.Kafka;

internal sealed class JsonValueDeserializer<T>(JsonTypeInfo<T> typeInfo) : IDeserializer<T?>
{
    public T? Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        => isNull ? default : JsonSerializer.Deserialize(data, typeInfo);
}