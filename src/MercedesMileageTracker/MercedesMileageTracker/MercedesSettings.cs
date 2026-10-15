namespace MercedesMileageTracker;

using Confluent.Kafka;

public class MercedesSettings
{
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public required string Topic { get; set; }
    public required string GroupId { get; set; }
    public string BootstrapServers { get; set; } = "bootstrap.streaming.connect-business.net:443";
    public SaslOauthbearerMethod SaslOauthbearerMethod { get; set; } = SaslOauthbearerMethod.Oidc;
    public string Scope { get; set; }
        = "openid groups profile audience:server:client_id:95B37AC2-D501-4CFD-B853-7D299DD2D872";
    public string TokenUrl { get; set; } = "https://ssoalpha.dvb.corpinter.net/v1/token";
}