using Api.ApplicationCore.Interfaces.Configs;
using Api.Controllers.FilterAttributes;
using Api.Domain.Entities.Configs;
using Api.Domain.Entities.Notifications;
using Api.Infrastructure.Data.DbContexts;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.Firebases;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.Interfaces.Jwt;
using Project.Core.Interfaces.Logging;
using Project.Core.RestApis;
using Project.Core.RestApis.Response;

namespace Api.Controllers.Configs
{
    public class ReminderTemplateController : BaseApiController
    {
        private readonly IReminderTemplateService _service;
        private readonly IAppLogger<ReminderTemplate> _loger;
        private readonly ApplicationDbContext _db;
        private readonly FirebasePushService _firebasePushService;
        private readonly IJwtService _jwtService;

        public ReminderTemplateController(
            IReminderTemplateService service,
            IAppLogger<ReminderTemplate> loger,
            ApplicationDbContext db,
            FirebasePushService firebasePushService,
            IJwtService jwtService)
        {
            _service = service;
            _loger = loger;
            _db = db;
            _firebasePushService = firebasePushService;
            _jwtService = jwtService;
        }

        [HttpPost("DxGrid")]
        [AuthorizeFilter(Scope = "ReminderTemplate.View")]
        public async Task<IActionResult> DxGridAsync([FromBody] DataSourceLoadOptions param)
        {
            var data = await _service.DxGridAsync(param);
            return new OK("success get all data", data).ReturnResponse();
        }

        [HttpGet("All")]
        [AuthorizeFilter(Scope = "ReminderTemplate.View")]
        public async Task<IActionResult> GetAllAsync()
        {
            var data = await _service.GetAll().AsNoTracking().ToListAsync();
            return new OK("Success get all data", data).ReturnResponse();
        }

        [HttpGet("{id}")]
        [AuthorizeFilter(Scope = "ReminderTemplate.View")]
        public async Task<IActionResult> GetAsync(Guid id)
        {
            var data = await _service.GetAsync(id);
            if (data == null)
            {
                _loger.LogError("Data not found : " + id);
                return new NotFound("Data not found : " + id, new { id }).ReturnResponse();
            }

            return new OK("Success get data by " + id, data).ReturnResponse();
        }

        [HttpPost("Param")]
        [AuthorizeFilter(Scope = "ReminderTemplate.View")]
        public async Task<IActionResult> GetAsync([FromBody] DataParameter param)
        {
            if (param.Start <= 0 || param.Length <= 0)
            {
                _loger.LogError("Bad request parameter. Start page > 0 and page size > 0");
                return new BadRequest("Start page > 0 and page size > 0", new { param.Start, param.Length }).ReturnResponse();
            }

            var data = await _service.GetAsync(param);
            return new OK("Success get data by paging", data).ReturnResponse();
        }

        [HttpPost]
        [AuthorizeFilter(Scope = "ReminderTemplate.Add")]
        public async Task<IActionResult> PostAsync([FromBody] ReminderTemplate data)
        {
            if (data == null)
            {
                _loger.LogError("Bad request parameter. Parameter is null");
                return new BadRequest("Parameter is null", data).ReturnResponse();
            }

            var id = await _service.PostAsync(data);
            if (id == null)
            {
                _loger.LogError("Duplicate data");

                var error = new Conflict("Duplicate data", data);
                return error.ReturnResponse();
            }

            var result = new OK("Success add data", new { id });
            return result.ReturnResponse();
        }

        [HttpPut]
        [AuthorizeFilter(Scope = "ReminderTemplate.Edit")]
        public async Task<IActionResult> PutAsync([FromBody] ReminderTemplate data)
        {
            if (data == null)
            {
                _loger.LogError("Bad request parameter. Parameter is null");

                var error = new BadRequest("Parameter is null", data);
                return error.ReturnResponse();
            }

            var id = await _service.PutAsync(data);
            if (id == null)
            {
                _loger.LogError("Duplicate data");

                var error = new Conflict("Duplicate data", data);
                return error.ReturnResponse();
            }

            var result = new OK("Success update data", new { id });
            return result.ReturnResponse();
        }

        [HttpPost("{id}/Test")]
        [AuthorizeFilter(Scope = "FirebaseNotification.Add")]
        public async Task<IActionResult> TestAsync(Guid id, CancellationToken cancellationToken)
        {
            var template = await _service.GetAll()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (template == null)
            {
                _loger.LogError("Reminder template not found: {0}", id);
                return new NotFound("Reminder template was not found.", new { id }).ReturnResponse();
            }

            var username = _jwtService.GetUsername();
            if (string.IsNullOrWhiteSpace(username))
            {
                return new Unauthorized("The authenticated username is unavailable.", new { id }).ReturnResponse();
            }

            var tokens = await _db.Set<UserDeviceToken>()
                .AsNoTracking()
                .Where(x => x.IsActive && x.UserLogin != null &&
                    x.UserLogin.IsActive && x.UserLogin.Username == username)
                .Select(x => x.DeviceToken)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (tokens.Count == 0)
            {
                return new NotFound(
                    "No active mobile device is registered for your account.",
                    new { id, username }).ReturnResponse();
            }

            try
            {
                var delivery = await _firebasePushService.SendAsync(
                    tokens,
                    template.Name,
                    template.ReminderText,
                    "reminder-test",
                    template.Id.ToString(),
                    cancellationToken);

                var response = new ReminderTestResultDto
                {
                    TemplateId = template.Id,
                    Username = username,
                    RegisteredDeviceCount = tokens.Count,
                    SuccessCount = delivery.SuccessCount,
                    FailureCount = delivery.FailureCount,
                    FailureCode = delivery.FirstErrorCode ?? string.Empty
                };

                if (delivery.SuccessCount == 0)
                {
                    if (string.Equals(
                        delivery.FirstErrorCode,
                        "ThirdPartyAuthError",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return new Conflict(
                            "The mobile device is registered, but the Firebase APNs credential for iOS is missing or invalid.",
                            response).ReturnResponse();
                    }

                    return new Conflict(
                        "Firebase could not deliver the test reminder to your registered device.",
                        response).ReturnResponse();
                }

                var message = delivery.FailureCount == 0
                    ? $"Test reminder sent to {delivery.SuccessCount} mobile device(s)."
                    : string.Equals(
                        delivery.FirstErrorCode,
                        "ThirdPartyAuthError",
                        StringComparison.OrdinalIgnoreCase)
                        ? $"Test reminder sent to {delivery.SuccessCount} mobile device(s); iOS delivery failed because the Firebase APNs credential is missing or invalid."
                        : $"Test reminder sent to {delivery.SuccessCount} mobile device(s); {delivery.FailureCount} delivery attempt(s) failed.";

                return new OK(message, response).ReturnResponse();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _loger.LogError("Firebase test reminder failed for template {0}: {1}", id, ex.Message);
                return new BadGateway(
                    "Firebase could not process the test reminder. Check the Firebase configuration and try again.",
                    new { id }).ReturnResponse();
            }
        }

        [HttpDelete("{id}")]
        [AuthorizeFilter(Scope = "ReminderTemplate.Delete")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var data = await _service.GetAsync(id);
            if (data == null)
            {
                _loger.LogError("Data not found : " + id.ToString());

                var error = new NotFound("Data not found : " + id.ToString(), new { id });
                return error.ReturnResponse();
            }

            var isDeleted = await _service.DeleteAsync(id);
            if (!isDeleted)
            {
                var error = new NotFound("Failed deleted data", new { id });
                return error.ReturnResponse();
            }

            var result = new OK("Success delete data ", new { id });
            return result.ReturnResponse();
        }
    }
}
