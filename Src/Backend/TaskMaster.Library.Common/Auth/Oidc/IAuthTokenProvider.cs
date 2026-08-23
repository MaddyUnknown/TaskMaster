
namespace TaskMaster.Library.Common.Auth.Oidc
{
    public interface IAuthTokenProvider
    {
        Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    }
}