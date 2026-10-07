using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Project.Base.Models.RestApis;
using Project.Core.HttpClients;
using Project.Core.Storages;
using Web.ApplicationCore.Interfaces.Auths;
using Web.Domain.Models;
using Web.Domain.Models.Notifications;
using Web.Infrastructure;
using System.Text;
using Newtonsoft.Json;
using System.Net;

namespace Web.ApplicationCore.Services.Auths
{
    public class AuthService : IAuthService
    {
        private const string NotificationCacheKey = "SmartSheep.SystemNotifications";
        private RestApiResult<List<SystemNotification>> _requestNotifications;
        private readonly HttpClient _client;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserService _userService;
        private readonly AppSetting _appSettings;

        public AuthService(
            IHttpContextAccessor httpContextAccessor,
            IUserService userService,
            AppSetting appSettings)
        {

            _httpContextAccessor = httpContextAccessor;
            _userService = userService;
            _appSettings = appSettings;

            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public bool IsAuthorizeSession()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
        }

        public Task<bool> GetAccessApplicationAsync()
        {
            return Task.FromResult(
                _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true);
        }

        public async Task<bool> GetAccessMenuAsync(string controllerName, string accessType)
        {
            var user = await _userService.GetUserProfileAsync();
            var result = false;
            var models = user?.Menus;
            if (models?.MenuParents == null || models.MenuChilds == null || models.MenuRoles == null)
            {
                return false;
            }

            var menu = models.MenuParents.FirstOrDefault(x => x.Controller == controllerName);
            if (menu == null)
            {
                menu = models.MenuChilds.FirstOrDefault(x => x.Controller == controllerName);
            }

            if (menu != null)
            {
                var menuRole = models.MenuRoles.FirstOrDefault(x => x.FkAppMenuId == menu.Id);
                if (menuRole != null)
                {
                    var accessTypes = (menuRole.AccessTypes ?? "").Split(",");
                    if (accessTypes.Any(x => string.Equals(x, accessType, StringComparison.OrdinalIgnoreCase)))
                    {
                        result = true;
                    }
                }
            }

            return result;
        }

        public async Task<string> LoadMenuAsync(string controllerName, AppSetting appSettings)
        {
            var htmlNavMenu = new StringBuilder(2048);
            var user = await _userService.GetUserProfileAsync();
            var models = user?.Menus;

            if (models?.MenuParents == null || models.MenuChilds == null)
            {
                return string.Empty;
            }

            try
            {
                foreach (var item in models.MenuParents.OrderBy(x => x.SequenceNumber))
                {
                    if (item.IsSection)
                    {
                        htmlNavMenu.Append("<li class='navigation-header'><span data-i18n='" + item.Name + "'>" + item.Name + "</span><svg xmlns='http://www.w3.org/2000/svg' width='14' height='14' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' class='feather feather-more-horizontal'><circle cx='12' cy='12' r='1'></circle><circle cx='19' cy='12' r='1'></circle><circle cx='5' cy='12' r='1'></circle></svg></li>");
                    }
                    else
                    {
                        var menuChilds = models.MenuChilds.Where(x => x.FkParentId == item.Id).ToList();
                        if (menuChilds.Count > 0)
                        {
                            var parent = models.MenuChilds.FirstOrDefault(x => string.Equals(x.Controller, controllerName, StringComparison.OrdinalIgnoreCase));
                            var classOpen = string.Empty;
                            var parentClassActive = string.Empty;

                            if (parent != null)
                            {
                                if (parent.FkParentId == item.Id)
                                {
                                    classOpen = "sidebar-group-active open";
                                    parentClassActive = "active";
                                }
                            }

                            htmlNavMenu.Append("<li class = 'nav-item has-sub " + classOpen + "'>");
                            htmlNavMenu.Append("    <a href = '#' class = 'd-flex align-items-center'>");
                            htmlNavMenu.Append("        " + item.Icon);
                            htmlNavMenu.Append("        <span class='menu-title text-truncate'> " + item.Name + "</span>");
                            htmlNavMenu.Append("    </a>");
                            htmlNavMenu.Append("    <ul class='menu-content'>");

                            foreach (var menuChild in menuChilds.OrderBy(x => x.SequenceNumber))
                            {
                                var childClassActive = string.Empty;

                                if (!string.IsNullOrEmpty(menuChild.Controller))
                                {
                                    if (string.Equals(menuChild.Controller, controllerName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        childClassActive = "active";
                                    }
                                }

                                htmlNavMenu.Append("        <li class = '" + childClassActive + "'>");
                                if ((menuChild.Controller ?? "").ToLower().Contains("https://"))
                                {
                                    var hrefLink = menuChild.Controller;
                                    htmlNavMenu.Append("            <a href = '" + hrefLink + "' target='_blank' class = 'd-flex align-items-center'>");
                                }
                                else
                                {
                                    htmlNavMenu.Append("            <a href = '" + appSettings.ApplicationUrl + "/" + menuChild.Controller + "' class = 'd-flex align-items-center'>");
                                }

                                var childIcon = childClassActive == "active" && !string.IsNullOrWhiteSpace(menuChild.IconActive)
                                    ? menuChild.IconActive : menuChild.Icon;
                                htmlNavMenu.Append(!string.IsNullOrWhiteSpace(childIcon) ? childIcon : "                <svg xmlns='http://www.w3.org/2000/svg' width='14' height='14' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' class='feather feather-circle'><circle cx='12' cy='12' r='10'></circle></svg>");
                                htmlNavMenu.Append("                <span class='menu-title text-truncate'> " + menuChild.Name + "</span>");
                                htmlNavMenu.Append("            </a>");
                                htmlNavMenu.Append("        </li>");
                            }

                            htmlNavMenu.Append("    </ul>");
                            htmlNavMenu.Append("</li>");
                        }
                        else
                        {
                            var parentClassActive = string.Empty;

                            if (!string.IsNullOrEmpty(item.Controller))
                            {
                                if (string.Equals(item.Controller, controllerName, StringComparison.OrdinalIgnoreCase))
                                {
                                    parentClassActive = "active";
                                }
                            }

                            htmlNavMenu.Append("<li class='nav-item " + parentClassActive + "'>");
                            if (!string.IsNullOrEmpty(item.Controller))
                            {
                                if ((item.Controller ?? "").ToLower().Contains("https://"))
                                {
                                    var hrefLink = item.Controller;
                                    htmlNavMenu.Append("    <a href = '" + hrefLink + "' target='_blank' class = 'd-flex align-items-center'>");
                                }
                                else
                                {
                                    htmlNavMenu.Append("            <a href = '" + appSettings.ApplicationUrl + "/" + item.Controller + "' class = 'd-flex align-items-center'>");
                                }
                            }
                            else
                            {
                                htmlNavMenu.Append("            <a href = '" + appSettings.ApplicationUrl + "/" + item.Controller + "' class = 'd-flex align-items-center'>");
                            }

                            htmlNavMenu.Append("        " + item.Icon);
                            htmlNavMenu.Append("        <span class='menu-title text-truncate'>" + item.Name + "</span>");
                            htmlNavMenu.Append("    </a>");
                            htmlNavMenu.Append("</li>");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FileStream objFilestream = new FileStream(string.Format("{0}\\{1}", Path.GetTempPath(), "ConsoleLog"), FileMode.Append, FileAccess.Write);
                StreamWriter objStreamWriter = new StreamWriter((Stream)objFilestream);
                objStreamWriter.WriteLine(ex.ToString());
                objStreamWriter.WriteLine("----------------");
                objStreamWriter.WriteLine(ex.Message);
                objStreamWriter.Close();
                objFilestream.Close();
            }

            return htmlNavMenu.ToString();
        }

        public async Task<RestApiResult<List<SystemNotification>>> SystemNotificationsAsync()
        {
            // Do not reuse a cross-request cache after farm assignments change.
            if (_requestNotifications != null) return _requestNotifications;
            var userName = HttpClientFactoryExtentions.GetUserName(_httpContextAccessor);
            var result = await HttpClientExtentions.SecuredGetAsync<List<SystemNotification>>(_client, "Auth/SystemNotification/" + userName);
            _requestNotifications = result;
            return result;
        }

        public void InvalidateSystemNotifications()
        {
            _requestNotifications = null;
            SessionDataExtentions.RemoveSession(NotificationCacheKey, _httpContextAccessor);
        }
    }
}
