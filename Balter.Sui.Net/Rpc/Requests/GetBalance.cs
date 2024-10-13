using Balter.Rpc.Shared;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Balter.Sui.Net.Rpc.Requests;

public class GetBalanceRequest : IRequestParams
{
    public required string SuiAddress { get; set; }

    public string? CoinType { get; set; } = null;

    public object[] Params =>
        CoinType == null
            ? [SuiAddress]
            : [SuiAddress, CoinType];
}

public class GetBalanceResponse
{
    [JsonPropertyName("coinObjectCount")]
    public uint CoinObjectCount { get; set; }

    [JsonPropertyName("coinType")]
    public string CoinType { get; set; } = string.Empty;

    [JsonPropertyName("totalBalance")]
    public BigInteger TotalBalance { get; set; }
}
