using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Core.Toasts.Abstractions;
using System.Net;
using Web.ApplicationCore.Interfaces.Notification;
using Web.ApplicationCore.Interfaces.Auths;
using Web.Domain.Models;

namespace Web.Controllers
{
    public class NotificationController : BaseWebController
    {
        private readonly INotificationService _service;
        private readonly IAuthService _authService;

        public NotificationController(INotyfService notyfService, AppSetting appSetting, INotificationService service, IAuthService authService) : base(notyfService, appSetting)
        {
            _service = service;
            _authService = authService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> ReadNotificationAsync(Guid id)
        {
            var results = await _service.ReadNotificationAsync(id);
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (results.StatusCode == (int)HttpStatusCode.OK && results.Data != null)
            {
                _authService.InvalidateSystemNotifications();
                if (isAjax)
                {
                    return Json(new
                    {
                        isRead = results.Data.IsRead,
                        dateRead = results.Data.DateRead,
                        returnUrl = Url.IsLocalUrl(results.Data.ReturnLink) ? results.Data.ReturnLink : null
                    });
                }
                if (Url.IsLocalUrl(results.Data.ReturnLink))
                {
                    return LocalRedirect(results.Data.ReturnLink);
                }
            }

            if (isAjax)
            {
                return StatusCode(502, new { message = "Unable to mark notification as read. Please try again." });
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
