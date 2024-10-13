using Balter.Rpc.Shared;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Balter.Sui.Net.Rpc.Requests;

public class GetCoinsRequest : IRequestParams
{
    public required string Owner { get; set; }
    public string? CoinType { get; set; } // defaults to sui itself '0x2::sui::SUI'
    public string? Page { get; set; }
    public uint? Limit { get; set; }

    public object[] Params => new object?[] {
        Owner,
        CoinType,
        Page,
        Limit,
    }.OfType<object>().ToArray();
}

public class Coin
{
    [JsonPropertyName("coinType")]
    public string CoinType { get; set; } = string.Empty;

    [JsonPropertyName("coinObjectId")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("digest")]
    public string Digest { get; set; } = string.Empty;

    [JsonPropertyName("balance")]
    public BigInteger Balance { get; set; }

    [JsonPropertyName("previousTransaction")]
    public string PreviousTransaction { get; set; } = string.Empty;
}

public class GetCoinsResponse
{
    [JsonPropertyName("data")]
    public Coin[] Coins { get; set; } = [];

    [JsonPropertyName("nextCursor")]
    public string NextCursor { get; set; } = string.Empty;

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }
}

