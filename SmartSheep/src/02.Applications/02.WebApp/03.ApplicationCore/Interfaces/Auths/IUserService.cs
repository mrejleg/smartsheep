using Web.Domain.Models.Auths;

namespace Web.ApplicationCore.Interfaces.Auths
{
    public interface IUserService
    {
        Task<AppUserResponseDto> GetUserProfileAsync();
    }
}
