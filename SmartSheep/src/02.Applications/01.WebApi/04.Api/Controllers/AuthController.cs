using Api.ApplicationCore.Interfaces.Auths;
using Api.Domain.Models.Auths;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Base.Models.Jwts;
using Project.Core.RestApis;
using Project.Core.RestApis.Response;
using Project.Core.Extentions;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthApiService _authService;

        public AuthController(IAuthApiService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("Token")]
        public async Task<ActionResult> Token([FromBody] TokenJwtRequest param)
        {
            if (param == null)
            {
                var error = new BadRequest("Parameter is null", new { param });
                return error.ReturnResponse();
            }
            else if (string.IsNullOrEmpty(param.ClientId) || string.IsNullOrEmpty(param.ClientSecret))
            {
                var error = new BadRequest("Client Id and Client Secret is required", new { param });
                return error.ReturnResponse();
            }

            var jwtResponse = await _authService.GenerateJwtAsync(param);

            if (!string.IsNullOrEmpty(jwtResponse.Token))
            {
                var result = new OK("Success get token", jwtResponse);
                return result.ReturnResponse();
            }

            var unauthorize = new Unauthorized("Unauthorized", new { param });
            return unauthorize.ReturnResponse();
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<ActionResult> Login([FromBody] LocalLoginRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var profile = await _authService.AuthenticateAsync(request);
            if (profile == null) return Unauthorized(new { Message = "The username or password is incorrect." });
            return Ok(profile);
        }

        [HttpGet("Profile/{username}")]
        public async Task<ActionResult> Profile(string username)
        {
            var signedInUsername = User.FindFirst("Username")?.Value.ToBase64Decode();
            if (string.IsNullOrWhiteSpace(signedInUsername) || !string.Equals(username, signedInUsername, StringComparison.OrdinalIgnoreCase)) return Forbid();
            var profile = await _authService.GetLocalProfileAsync(username);
            return profile == null ? NotFound() : Ok(profile);
        }

        [HttpGet("SystemNotification/{username}")]
        public async Task<ActionResult> SystemNotificationAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                var error = new BadRequest("Username id required", new { username });
                return error.ReturnResponse();
            }

            var data = await _authService.SystemNotificationAsync(username);
            var result = new OK("Success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("ReadNotification/{id}")]
        public async Task<IActionResult> GetAsync(Guid id)
        {
            var data = await _authService.ReadNotificationAsync(id);

            if (data == null)
            {
                var error = new NotFound("Data not found : " + id.ToString(), id);
                return error.ReturnResponse();
            }

            var result = new OK("Success update data " + id.ToString(), data);
            return result.ReturnResponse();
        }
    }
}
