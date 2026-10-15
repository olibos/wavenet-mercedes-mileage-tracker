namespace MercedesMileageTracker;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed record VehicleMessage(
    string Vin,
    Guid MessageId,
    [property: JsonConverter(typeof(UnixMillisecondsConverter))] DateTimeOffset Timestamp,
    string ServiceId,
    string SendingBehaviour,
    VehicleData Data,
    string MessageType,
    string Version);

public sealed record VehicleData(IReadOnlyList<VehicleSignal> Signals);

public sealed record VehicleSignal(
    string Name,
    [property: JsonConverter(typeof(UnixMillisecondsConverter))] DateTimeOffset Timestamp,
    string Value,
    string Type)
{
    public double? AsDouble() =>
        Type == "double" && double.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
            ? d
            : null;
}

public sealed class UnixMillisecondsConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64());

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.ToUnixTimeMilliseconds());
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(VehicleMessage))]
internal partial class AppJsonContext : JsonSerializerContext;