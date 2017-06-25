using System.Threading.Tasks;
using RestSharp;

namespace Noggin.NetCoreAuth.Providers
{
    public static class RestClientExtensions
    {
        public static async Task<IRestResponse> ExecuteAsync(this IRestClient client, RestRequest request)
        {
            var taskCompletion = new TaskCompletionSource<IRestResponse>();
            client.ExecuteAsync(request, r => taskCompletion.SetResult(r));
            return await taskCompletion.Task;
        }

        public static async Task<IRestResponse<T>> ExecuteAsync<T>(this IRestClient client, RestRequest request) where T : new()
        {
            var taskCompletion = new TaskCompletionSource<IRestResponse<T>>();
            client.ExecuteAsync<T>(request, r => taskCompletion.SetResult(r));
            return await taskCompletion.Task;
        }
    }
}