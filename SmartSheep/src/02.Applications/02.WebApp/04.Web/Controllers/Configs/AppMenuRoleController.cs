using Microsoft.AspNetCore.Mvc;
using Project.Base.Constants;
using Project.Core.Extentions;
using Project.Core.Toasts.Abstractions;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.Domain.Models;
using Web.Domain.Models.Configs;

namespace Web.Controllers.Configs
{
    public class AppMenuRoleController : BaseWebController
    {
        private readonly IConfigRepositories _configRepositories;
        private string viewPath;

        public AppMenuRoleController(INotyfService notyfService, AppSetting appSetting, IConfigRepositories configRepositories) : base(notyfService, appSetting)
        {
            _configRepositories = configRepositories;
            viewPath = "~/Views/Configs/AppMenuRole/";
        }

        public async Task<ActionResult> IndexAsync(Guid? fkRoleId)
        {
            AuthorizePage(EnumExtentions.GetEnumDescriptionByValue(AccessTypes.View));

            var result = new AppMenuRoleDto
            {
                Menus = new List<AppMenu>(),
                MenuRoles = new List<AppMenuRole>()
            };

            if (fkRoleId.HasValue)
            {
                var menusTask = _configRepositories.AppMenus.GetAllAsync();
                var menuRolesTask = _configRepositories.AppMenuRoles.GetAllAsync(fkRoleId.Value);
                await Task.WhenAll(menusTask, menuRolesTask);

                var menus = await menusTask;
                if (menus.Data.Count > 0)
                {
                    result.Menus = menus.Data.ToList();
                }

                var menuRoles = await menuRolesTask;
                if (menuRoles.Data.Count > 0)
                {
                    result.MenuRoles = menuRoles.Data.ToList();
                }

                result.FkAppRoleId = fkRoleId;
            }

            return View(viewPath + "Index.cshtml", result);
        }

        [HttpPost]
        public IActionResult Search(Guid? fkRoleId)
        {
            AuthorizePage(EnumExtentions.GetEnumDescriptionByValue(AccessTypes.View));
            return RedirectToAction("Index", new { fkRoleId });
        }

        [HttpPost]
        public async Task<IActionResult> SaveAsync(Guid? fkRoleId)
        {
            AuthorizePage(EnumExtentions.GetEnumDescriptionByValue(AccessTypes.Save));

            if (fkRoleId.HasValue)
            {
                var menusTask = _configRepositories.AppMenus.GetAllAsync();
                var accessTypesTask = _configRepositories.AppMenus.BindingSelect2AccessTypes();
                await Task.WhenAll(menusTask, accessTypesTask);
                var menus = await menusTask;
                var accessTypeResults = await accessTypesTask;
                var replacements = new List<AppMenuRole>();
                foreach (var menu in menus.Data)
                {
                    var accessTypes = new List<string>();
                    foreach (var accessType in accessTypeResults.Data)
                    {
                        var form = Request.Form["chk_" + menu.Id + "_" + accessType.Id];
                        if (!string.IsNullOrEmpty(form))
                        {
                            accessTypes.Add(accessType.Id.ToString().ToLower());
                        }
                    }

                    if (accessTypes.Count > 0)
                    {
                        replacements.Add(new AppMenuRole
                        {
                            FkAppMenuId = menu.Id,
                            FkAppRoleId = fkRoleId.Value,
                            AccessTypes = string.Join(",", accessTypes),
                            IsActive = true
                        });
                    }
                }

                var saveResult = await _configRepositories.AppMenuRoles.ReplaceAsync(fkRoleId.Value, replacements);
                if (saveResult.StatusCode != StatusCodes.Status200OK)
                {
                    _notyfService.Error(saveResult.Message ?? "Failed to save menu permissions.");
                    return RedirectToAction("Index", new { fkRoleId });
                }

                _notyfService.Success(Messages.SuccessSaveMessage);
                return RedirectToAction("Index", new { fkRoleId });
            }

            _notyfService.Error(Messages.RequiredDataMessage);
            return RedirectToAction("Index", new { fkRoleId });
        }
    }
}
