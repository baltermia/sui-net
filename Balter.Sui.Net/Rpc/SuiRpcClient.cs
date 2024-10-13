using Balter.Rpc.Shared;
using Balter.Sui.Net.Rpc.Requests;

namespace Balter.Sui.Net.Rpc;

public class SuiRpcClient : ISuiRpcClient
{
    private readonly IRpcClient _rpc;

    public SuiRpcClient(string rpcUrl)
    {
        _rpc = new JsonRpcClient(rpcUrl);
    }

    public Task<Response<GetBalanceResponse>?> GetBalanceAsync(GetBalanceRequest request)
    {
        const string method = "suix_getBalance";

        return _rpc.SendRequestAsync<GetBalanceResponse>(method, request);
    }

    public Task<Response<PaySuiResponse>?> PaySuiAsync(PaySuiRequest request)
    {
        const string method = "unsafe_paySui";
        return _rpc.SendRequestAsync<PaySuiResponse>(method, request);
    }

    public Task<Response<GetCoinsResponse>?> GetCoinsAsync(GetCoinsRequest request)
    {
        const string method = "suix_getCoins";
        return _rpc.SendRequestAsync<GetCoinsResponse>(method, request);
    }
}

