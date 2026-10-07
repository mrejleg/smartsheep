using Microsoft.AspNetCore.Mvc;
using Project.Base.Constants;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Core.Extentions;
using Project.Core.Toasts.Abstractions;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.Domain.Models;

namespace Web.Controllers.SmartSheep;

public class SourceVideoStatusLogController : BaseWebController
{
    private readonly ISmartSheepRepositories _repository;
    private const string ViewPath = "~/Views/SmartSheep/SourceVideoStatusLog/";

    public SourceVideoStatusLogController(INotyfService notyfService, AppSetting appSetting,
        ISmartSheepRepositories repository) : base(notyfService, appSetting)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetDataDxGrid([FromQuery] DataSourceLoadOptions loadOptions)
    {
        AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());
        return DataDxGrid(await _repository.SourceVideoStatusLogs.DxGridAsync(loadOptions));
    }

    public IActionResult Index()
    {
        AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());
        return View(ViewPath + "Index.cshtml");
    }

    public async Task<IActionResult> DetailsAsync(Guid id)
    {
        return Details(ViewPath, id, await _repository.SourceVideoStatusLogs.GetAsync(id));
    }
}
