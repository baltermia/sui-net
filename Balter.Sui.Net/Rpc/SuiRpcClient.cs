using Balter.Rpc.Shared;
using Balter.Sui.Net.Rpc.Requests;

namespace Balter.Sui.Net.Rpc;

internal class SuiRpcClient : ISuiRpcClient
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
}

