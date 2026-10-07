using Microsoft.AspNetCore.Mvc;
using Project.Base.Constants;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Project.Core.Toasts.Abstractions;
using Web.ApplicationCore.Interfaces.Auths;
using Web.ApplicationCore.Interfaces.Configs;
using Web.Domain.Models;
using Web.Domain.Models.Configs;

namespace Web.Controllers.Configs;

public class UserFarmController : BaseWebController
{
    private const string ViewPath = "~/Views/Configs/UserFarm/";
    private readonly IUserFarmService _service;
    private readonly IAuthService _auth;

    public UserFarmController(INotyfService notyfService, AppSetting settings, IUserFarmService service, IAuthService auth)
        : base(notyfService, settings)
    {
        _service = service;
        _auth = auth;
    }

    private async Task<bool> CanAsync(string access)
    {
        AuthorizePage(access);
        return await _auth.GetAccessMenuAsync("UserFarm", access);
    }

    public async Task<IActionResult> Index()
    {
        if (!await CanAsync("view")) return Forbid();
        return View(ViewPath + "Index.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> GetDataDxGrid([FromQuery] DataSourceLoadOptions options)
    {
        if (!await CanAsync("view")) return Forbid();
        return DataDxGrid(await _service.DxGridAsync(options));
    }

    public async Task<IActionResult> Create()
    {
        if (!await CanAsync("add")) return Forbid();
        await LoadOptionsAsync();
        return View(ViewPath + "Create.cshtml", new RestApiResult<UserFarm> { Data = new UserFarm() });
    }

    [HttpGet]
    public async Task<IActionResult> UsersAsync(Guid? id)
    {
        if (!await CanAsync("add") && !await CanAsync("edit")) return Forbid();
        await LoadOptionsAsync(id.HasValue ? (await _service.GetAsync(id.Value)).Data : null);
        if (!ModelState.IsValid) return StatusCode(502);
        return Json(((UserFarmOptions)ViewBag.Options).Users);
    }

    [HttpGet]
    public async Task<IActionResult> FarmsAsync(Guid? id)
    {
        if (!await CanAsync("add") && !await CanAsync("edit")) return Forbid();
        await LoadOptionsAsync(id.HasValue ? (await _service.GetAsync(id.Value)).Data : null);
        if (!ModelState.IsValid) return StatusCode(502);
        return Json(((UserFarmOptions)ViewBag.Options).Farms);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAsync(RestApiResult<UserFarm> model)
    {
        if (!await CanAsync("add")) return Forbid();
        if (!ModelState.IsValid || model?.Data == null)
        {
            _notyfService.Error(Messages.ModelStateInvalidMessage);
            await LoadOptionsAsync(model?.Data);
            return View(ViewPath + "Create.cshtml", model ?? new RestApiResult<UserFarm> { Data = new UserFarm() });
        }
        var result = await _service.PostAsync(model.Data);
        if (result.StatusCode != 200) await LoadOptionsAsync(model.Data);
        return Create(ViewPath, model, result);
    }

    public async Task<IActionResult> EditAsync(Guid id)
    {
        if (!await CanAsync("edit")) return Forbid();
        var result = await _service.GetAsync(id);
        if (result.StatusCode == 200 && result.Data != null) await LoadOptionsAsync(result.Data);
        return Edit(ViewPath, id, result);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAsync(RestApiResult<UserFarm> model)
    {
        if (!await CanAsync("edit")) return Forbid();
        if (!ModelState.IsValid || model?.Data == null)
        {
            _notyfService.Error(Messages.ModelStateInvalidMessage);
            await LoadOptionsAsync(model?.Data);
            return View(ViewPath + "Edit.cshtml", model ?? new RestApiResult<UserFarm> { Data = new UserFarm() });
        }
        var result = await _service.PutAsync(model.Data);
        if (result.StatusCode != 200) await LoadOptionsAsync(model.Data);
        return Edit(ViewPath, model, result);
    }

    public async Task<IActionResult> DetailsAsync(Guid id)
    {
        if (!await CanAsync("detail")) return Forbid();
        var result = await _service.GetAsync(id);
        return Details(ViewPath, id, result);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        if (!await CanAsync("delete")) return Forbid();
        return Delete(await _service.DeleteAsync(id));
    }

    private async Task LoadOptionsAsync(UserFarm? current = null)
    {
        var result = await _service.GetOptionsAsync();
        var options = result.Data ?? new UserFarmOptions();
        if (result.StatusCode != 200)
        {
            var message = result.Message ?? Messages.DataNotFoundMessage;
            ModelState.AddModelError(string.Empty, message);
            _notyfService.Error(message);
        }
        // Keep existing inactive selections visible while editing an assignment.
        if (current?.Id != Guid.Empty && current?.FkUserId != null && current.FkFarmId != null)
        {
            var saved = await _service.GetAsync(current.Id);
            if (saved.StatusCode == 200 && saved.Data != null)
            {
                var row = saved.Data;
                if (row.FkUserId == current.FkUserId && !options.Users.Any(x => x.Id == row.FkUserId))
                    options.Users.Add(new UserFarmUserOption { Id = row.FkUserId!.Value, Username = row.Username, Name = row.FullName + " — inactive", Email = row.Email });
                if (row.FkFarmId == current.FkFarmId && !options.Farms.Any(x => x.Id == row.FkFarmId))
                    options.Farms.Add(new UserFarmFarmOption { Id = row.FkFarmId!.Value, Code = row.FarmCode, Name = row.FarmName + " — inactive" });
            }
        }
        ViewBag.Options = options;
    }
}
