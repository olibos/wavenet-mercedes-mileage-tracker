using System.Data;
using System.Runtime.InteropServices;
using Confluent.Kafka;
using MercedesMileageTracker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.secrets.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var settings = configuration.GetSection("MercedesSettings").Get<MercedesSettings>() ?? throw new InvalidOperationException("Missing MercedesSettings section in configuration");

var caPath = new[]
{
    "/etc/ssl/certs/ca-certificates.crt", // Debian/Ubuntu/Arch
    "/etc/pki/tls/certs/ca-bundle.crt",   // Fedora/RHEL
    "/etc/ssl/ca-bundle.pem",             // openSUSE
}.FirstOrDefault(File.Exists) ?? throw new InvalidOperationException("No CA bundle found");

var config = new ConsumerConfig(new Dictionary<string, string>
{
    ["https.ca.location"] = caPath
})
{
    BootstrapServers = settings.BootstrapServers,
    GroupId = settings.GroupId,
    SaslOauthbearerMethod = settings.SaslOauthbearerMethod,
    SaslOauthbearerClientId = settings.ClientId,
    SaslOauthbearerClientSecret = settings.ClientSecret,
    SaslOauthbearerScope = settings.Scope,
    SaslOauthbearerTokenEndpointUrl = settings.TokenUrl,

    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnableAutoCommit = false,
    SecurityProtocol = SecurityProtocol.SaslSsl,
    SaslMechanism = SaslMechanism.OAuthBearer,
};

using var cts = new CancellationTokenSource();
using var sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, ctx =>
{
    ctx.Cancel = true;
    cts.Cancel();
});
using var sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx =>
{
    ctx.Cancel = true;
    cts.Cancel();
});
using var consumer = new ConsumerBuilder<string, VehicleMessage?>(config)
    .SetKeyDeserializer(Deserializers.Utf8)
    .SetValueDeserializer(new JsonValueDeserializer<VehicleMessage>(AppJsonContext.Default.VehicleMessage))
    .SetErrorHandler((_, e) => Console.Error.WriteLine($"[kafka error] {e.Code}: {e.Reason}"))
    .Build();

var knownSensors = new Dictionary<string, string>
{
    ["Driving.Odometer.Lifetime"] = "Odometer",
    ["Service.Interval.Distance"] = "NextServiceKm",
};

consumer.Subscribe(settings.Topic);

var connectionString = configuration.GetConnectionString("MileageTracker")
                       ?? throw new InvalidOperationException("Missing connection string MileageTracker");

const string upsertSql = 
    """
    MERGE dbo.VehicleSensorValue WITH (HOLDLOCK) AS target
    USING (
        SELECT @VIN AS VIN, SensorName, OriginalName, Value, EventDate
        FROM @Signals
    ) AS source
        ON target.VIN = source.VIN AND target.SensorName = source.SensorName
    WHEN MATCHED AND source.EventDate > target.EventDate THEN
        UPDATE SET
            OriginalName = source.OriginalName,
            Value        = source.Value,
            Source       = @Source,
            EventDate    = source.EventDate
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (VIN, SensorName, OriginalName, Value, Source, EventDate)
        VALUES (source.VIN, source.SensorName, source.OriginalName, source.Value, @Source, source.EventDate);
    """;
while (!cts.IsCancellationRequested)
{
    var content = consumer.Consume(cts.Token);
    var message = content.Message.Value;
    if (message is { Data.Signals.Count: > 0 })
    {
        var table = new DataTable();
        table.Columns.Add("SensorName", typeof(string));
        table.Columns.Add("OriginalName", typeof(string));
        table.Columns.Add("Value", typeof(string));
        table.Columns.Add("EventDate", typeof(DateTime));

        foreach (var group in message.Data.Signals.GroupBy(s => knownSensors.GetValueOrDefault(s.Name, s.Name)))
        {
            var latest = group.MaxBy(s => s.Timestamp)!;
            table.Rows.Add(group.Key, latest.Name, latest.Value, latest.Timestamp.UtcDateTime);
        }
        await using var connection = new SqlConnection(connectionString);
        await using var command = new SqlCommand(upsertSql, connection);

        command.Parameters.Add("@VIN", SqlDbType.Char, 17).Value = message.Vin;
        command.Parameters.Add("@Source", SqlDbType.VarChar, 30).Value = "Mercedes";
        var signals = command.Parameters.Add("@Signals", SqlDbType.Structured);
        signals.TypeName = "dbo.VehicleSensorValueType";
        signals.Value = table;

        await connection.OpenAsync(cts.Token);
        await command.ExecuteNonQueryAsync(cts.Token);
    }

    consumer.Commit(content);
}