using Balter.Rpc.Shared;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Balter.Sui.Net.Rpc.Requests;

public class PaySuiRequest : IRequestParams
{
    public required string Signer { get; set; }
    public required string[] InputCoints { get; set; }
    public required string[] Recipients { get; set; }
    public required BigInteger[] Amounts { get; set; }
    public required BigInteger GasBudget { get; set; }

    public object[] Params => [
        Signer,
        InputCoints,
        Recipients,
        Amounts.Select(a => a.ToString()).ToArray(),
        GasBudget.ToString(),
    ];
}

public class PaySuiResponse
{
    [JsonPropertyName("txBytes")]
    public string TransactionBytes { get; set; } = string.Empty;
}

