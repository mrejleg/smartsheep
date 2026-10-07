using Microsoft.AspNetCore.Mvc;
using Project.Base.Constants;
using Project.Core.Extentions;
using System.Net;
using Project.Core.Toasts.Abstractions;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.Domain.Models;
using Web.Domain.Models.ApiManagements;
using Web.Domain.Models.Configs;

namespace Web.Controllers.ApiManagements
{
    public class AppScopeClientController : BaseWebController
    {
        private readonly IConfigRepositories _configRepositories;
        private string viewPath;

        public AppScopeClientController(INotyfService notyfService, AppSetting appSetting, IConfigRepositories configRepositories) : base(notyfService, appSetting)
        {
            _configRepositories = configRepositories;
            viewPath = "~/Views/ApiManagements/AppScopeClient/";
        }

        public async Task<ActionResult> IndexAsync(Guid? fkAppClientId)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var result = new AppScopeClientDto
            {
                ScopeClients = new List<ScopeClient>(),
                AppScopeClients = new List<AppScopeClient>()
            };

            if (fkAppClientId.HasValue)
            {
                var scopeClientsTask = _configRepositories.ScopeClients.GetAllAsync();
                var appScopeClientsTask = _configRepositories.AppScopeClients.GetAllAsync(fkAppClientId.Value);
                await Task.WhenAll(scopeClientsTask, appScopeClientsTask);

                var scopeClients = await scopeClientsTask;
                if (scopeClients.Data.Count > 0)
                {
                    result.ScopeClients = scopeClients.Data.ToList();
                }

                var appScopeClients = await appScopeClientsTask;
                if (appScopeClients.Data.Count > 0)
                {
                    result.AppScopeClients = appScopeClients.Data.ToList();
                }

                result.FkAppClientId = fkAppClientId;
            }

            return View(viewPath + "Index.cshtml", result);
        }

        [HttpPost]
        public IActionResult Search(Guid? fkAppClientId)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());
            return RedirectToAction("Index", new { fkAppClientId });
        }

        [HttpPost]
        public async Task<IActionResult> SaveAsync(Guid? fkAppClientId)
        {
            AuthorizePage(AccessTypes.Save.GetEnumDescriptionByValue());

            if (!fkAppClientId.HasValue)
            {
                _notyfService.Error(Messages.RequiredDataMessage);
                return RedirectToAction("Index");
            }

            var clientId = fkAppClientId.Value;
            var selectedScopeIds = Request.Form.Keys
                .Where(key => key.StartsWith("chk_", StringComparison.OrdinalIgnoreCase))
                .Select(key => key[4..])
                .Select(value => Guid.TryParse(value, out var scopeId) ? scopeId : (Guid?)null)
                .Where(scopeId => scopeId.HasValue)
                .Select(scopeId => scopeId!.Value)
                .ToHashSet();

            var replacements = selectedScopeIds.Select(scopeId => new AppScopeClient
                {
                    FkScopeClientId = scopeId,
                    FkAppClientId = clientId,
                    IsActive = true
                })
                .ToList();

            var saveResult = await _configRepositories.AppScopeClients.ReplaceAsync(clientId, replacements);
            if (saveResult.StatusCode != (int)HttpStatusCode.OK)
            {
                _notyfService.Error(saveResult.Message ?? "Failed to save application scopes.");
                return RedirectToAction("Index", new { fkAppClientId = clientId });
            }

            _notyfService.Success(Messages.SuccessSaveMessage);
            return RedirectToAction("Index", new { fkAppClientId = clientId });
        }
    }
}
