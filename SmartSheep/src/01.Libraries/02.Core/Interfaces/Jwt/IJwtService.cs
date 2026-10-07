using Project.Base.Models.Jwts;

namespace Project.Core.Interfaces.Jwt
{
    public interface IJwtService
    {
        JwtResponse GetJwt(JwtRequest jwtRequest);
        JwtResponse GetJwtLongExpires(JwtRequest jwtRequest);
        string GetClaimData(string claimName);
        string GetApplicationId();
        string GetClientId();
        string GetUsername();
        string GetEmail();
        string GetFullName();
        string GetRoles();
        string GetCostCenterCode();
        string GetEaCode();
    }
}
