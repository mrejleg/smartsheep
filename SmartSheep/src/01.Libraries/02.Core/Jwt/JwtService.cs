using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Project.Base.Models.Jwts;
using Project.Core.Extentions;
using Project.Core.Interfaces.Jwt;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Project.Core.Jwt
{
    public class JwtService : IJwtService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public JwtService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public JwtResponse GetJwt(JwtRequest jwtRequest)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtRequest.JwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claimJwts = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.UniqueName, jwtRequest.ClientId),
                new Claim(JwtRegisteredClaimNames.Sid, jwtRequest.ClientId),
                new Claim("ClientId", jwtRequest.ClientId.ToBase64Encode()),
                new Claim("ClientSecret", jwtRequest.ClientSecret.ToBase64Encode())
            };

            if (!string.IsNullOrEmpty(jwtRequest.ApplicationId))
            {
                claimJwts.Add(new Claim("ApplicationId", jwtRequest.ApplicationId.ToBase64Encode()));
            }

            if (!string.IsNullOrEmpty(jwtRequest.Username))
            {
                claimJwts.Add(new Claim("Username", jwtRequest.Username.ToBase64Encode()));
            }

            if (!string.IsNullOrEmpty(jwtRequest.Email))
            {
                claimJwts.Add(new Claim("Email", jwtRequest.Email.ToBase64Encode()));
            }

            if (!string.IsNullOrEmpty(jwtRequest.FullName))
            {
                claimJwts.Add(new Claim("FullName", jwtRequest.FullName.ToBase64Encode()));
            }

            if (!string.IsNullOrEmpty(jwtRequest.PositionId))
            {
                claimJwts.Add(new Claim("PositionId", jwtRequest.PositionId.ToBase64Encode()));
            }

            if (!string.IsNullOrEmpty(jwtRequest.PositionName))
            {
                claimJwts.Add(new Claim("PositionName", jwtRequest.PositionName.ToBase64Encode()));
            }

            if (!string.IsNullOrEmpty(jwtRequest.Roles))
            {
                claimJwts.Add(new Claim("Roles", jwtRequest.Roles.ToBase64Encode()));
            }

            if (!string.IsNullOrEmpty(jwtRequest.CostCenterCode))
            {
                claimJwts.Add(new Claim("CostCenterCode", jwtRequest.CostCenterCode.ToBase64Encode()));
            }

            if (!string.IsNullOrEmpty(jwtRequest.EaCode))
            {
                claimJwts.Add(new Claim("EaCode", jwtRequest.EaCode.ToBase64Encode()));
            }

            var token = new JwtSecurityToken(
                issuer: jwtRequest.JwtIssuer, //Owner Api => Application Name or URL API
                audience: jwtRequest.ClientId, //Client Token => Client Name or URL Website
                claims: claimJwts,
                notBefore: DateTime.Now,
                expires: jwtRequest.JwtExpireIn,
                signingCredentials: credentials);

            var encodeToken = new JwtSecurityTokenHandler().WriteToken(token);

            var refreshTokenClaimEncode = (jwtRequest.ClientId + jwtRequest.Username + jwtRequest.Roles).ToBase64Encode();
            var claimJwtRefreshToken = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.UniqueName, jwtRequest.ClientId),
                new Claim(JwtRegisteredClaimNames.Sid, jwtRequest.ClientId),
                new Claim("RefreshTokenClaim", refreshTokenClaimEncode),
            };

            var refreshToken = new JwtSecurityToken(
                issuer: jwtRequest.JwtIssuer, //Owner Api => Application Name or URL API
                audience: jwtRequest.ClientId, //Client Token => Client Name or URL Website
                claims: claimJwtRefreshToken,
                notBefore: DateTime.Now,
                expires: DateTime.Now.AddYears(10).AddMinutes(5),
                signingCredentials: credentials);

            var encodeRefreshToken = new JwtSecurityTokenHandler().WriteToken(refreshToken);

            var jwtResponse = new JwtResponse
            {
                Token = encodeToken,
                RefreshToken = encodeRefreshToken,
                ValidFrom = token.ValidFrom.ToLocalTime(),
                ValidTo = token.ValidTo.ToLocalTime()
            };

            return jwtResponse;
        }

        public JwtResponse GetJwtLongExpires(JwtRequest jwtRequest)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtRequest.JwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claimJwts = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.UniqueName, jwtRequest.ClientId),
                new Claim(JwtRegisteredClaimNames.Sid, jwtRequest.ClientId),
                new Claim("ClientId", jwtRequest.ClientId.ToBase64Encode()),
                new Claim("ClientSecret", jwtRequest.ClientSecret.ToBase64Encode())
            };

            if (!string.IsNullOrEmpty(jwtRequest.ApplicationId))
            {
                claimJwts.Add(new Claim("ApplicationId", jwtRequest.ApplicationId.ToBase64Encode()));
            }

            if (!string.IsNullOrEmpty(jwtRequest.Username))
            {
                claimJwts.Add(new Claim("Username", jwtRequest.Username.ToBase64Encode()));
            }

            if (!string.IsNullOrEmpty(jwtRequest.Email))
            {
                claimJwts.Add(new Claim("Email", jwtRequest.Email.ToBase64Encode()));
            }

            if (!string.IsNullOrEmpty(jwtRequest.FullName))
            {
                claimJwts.Add(new Claim("FullName", jwtRequest.FullName.ToBase64Encode()));
            }

            if (!string.IsNullOrEmpty(jwtRequest.PositionId))
            {
                claimJwts.Add(new Claim("PositionId", jwtRequest.PositionId.ToBase64Encode()));
            }

            if (!string.IsNullOrEmpty(jwtRequest.PositionName))
            {
                claimJwts.Add(new Claim("PositionName", jwtRequest.PositionName.ToBase64Encode()));
            }

            if (!string.IsNullOrEmpty(jwtRequest.Roles))
            {
                claimJwts.Add(new Claim("Roles", jwtRequest.Roles.ToBase64Encode()));
            }

            if (!string.IsNullOrEmpty(jwtRequest.CostCenterCode))
            {
                claimJwts.Add(new Claim("CostCenterCode", jwtRequest.CostCenterCode.ToBase64Encode()));
            }

            if (!string.IsNullOrEmpty(jwtRequest.EaCode))
            {
                claimJwts.Add(new Claim("EaCode", jwtRequest.EaCode.ToBase64Encode()));
            }

            var token = new JwtSecurityToken(
                issuer: jwtRequest.JwtIssuer, //Owner Api => Application Name or URL API
                audience: jwtRequest.ClientId, //Client Token => Client Name or URL Website
                claims: claimJwts,
                notBefore: DateTime.Now,
                expires: DateTime.Now.AddYears(10),
                signingCredentials: credentials);

            var encodeToken = new JwtSecurityTokenHandler().WriteToken(token);

            var refreshTokenClaimEncode = (jwtRequest.ClientId + jwtRequest.Username + jwtRequest.Roles).ToBase64Encode();
            var claimJwtRefreshToken = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.UniqueName, jwtRequest.ClientId),
                new Claim(JwtRegisteredClaimNames.Sid, jwtRequest.ClientId),
                new Claim("RefreshTokenClaim", refreshTokenClaimEncode),
            };

            var refreshToken = new JwtSecurityToken(
                issuer: jwtRequest.JwtIssuer, //Owner Api => Application Name or URL API
                audience: jwtRequest.ClientId, //Client Token => Client Name or URL Website
                claims: claimJwtRefreshToken,
                notBefore: DateTime.Now,
                expires: DateTime.Now.AddYears(10).AddMinutes(5),
                signingCredentials: credentials);

            var encodeRefreshToken = new JwtSecurityTokenHandler().WriteToken(refreshToken);

            var jwtResponse = new JwtResponse
            {
                Token = encodeToken,
                RefreshToken = encodeRefreshToken,
                ValidFrom = token.ValidFrom.ToLocalTime(),
                ValidTo = token.ValidTo.ToLocalTime()
            };

            return jwtResponse;
        }

        public string GetClaimData(string claimName)
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(claimName)?.Value?.ToBase64Decode();
        }

        public string GetClientId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("ClientId")?.Value?.ToBase64Decode();
        }

        public string GetClientSecret()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("ClientSecret")?.Value?.ToBase64Decode();
        }

        public string GetApplicationId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("ApplicationId")?.Value?.ToBase64Decode();
        }

        public string GetUsername()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("Username")?.Value?.ToBase64Decode();
        }

        public string GetEmail()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("Email")?.Value?.ToBase64Decode();
        }

        public string GetFullName()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("FullName")?.Value?.ToBase64Decode();
        }

        public string GetRoles()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("Roles")?.Value?.ToBase64Decode();
        }

        public string GetCostCenterCode()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("CostCenterCode")?.Value?.ToBase64Decode();
        }

        public string GetEaCode()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("EaCode")?.Value?.ToBase64Decode();
        }
    }
}
