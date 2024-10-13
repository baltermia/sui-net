using Balter.Rpc.Shared;
using Balter.Sui.Net.Rpc.Requests;

namespace Balter.Sui.Net.Rpc;

public interface ISuiRpcClient
{
    public Task<Response<GetBalanceResponse>?> GetBalanceAsync(GetBalanceRequest request);
}
