using Api.Controllers.FilterAttributes;
using Api.Domain.Entities.Auths;
using Api.Domain.Models.Auths;
using Api.Infrastructure.Data.DbContexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Core.DevExpress;
using Project.Core.Interfaces.Jwt;
using Project.Core.RestApis;
using Project.Core.RestApis.Response;

namespace Api.Controllers.Configs;

public class UserFarmController : BaseApiController
{
    private readonly ApplicationDbContext _db;
    private readonly IJwtService _jwt;

    public UserFarmController(ApplicationDbContext db, IJwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    private IQueryable<UserFarmDto> Query() => _db.UserFarm.AsNoTracking()
        .Where(x => !_db.FarmScopeRequired || _db.AssignedFarmIds.Contains(x.FkFarmId)).Select(x => new UserFarmDto
    {
        Id = x.Id, FkUserId = x.FkUserId, FkFarmId = x.FkFarmId,
        Username = x.UserLogin!.Username, FullName = x.UserLogin.FullName, Email = x.UserLogin.Email,
        FarmCode = x.Farm!.Code, FarmName = x.Farm.Name, IsActive = x.IsActive,
        DateCreated = x.DateCreated, DateModified = x.DateModified,
        CreatedBy = x.CreatedBy, ModifiedBy = x.ModifiedBy
    });

    private async Task<bool> CanAsync(string access)
    {
        var username = _jwt.GetUsername();
        if (string.IsNullOrWhiteSpace(username)) return false;
        var roleIds = _db.UserLoginRole.Where(x => x.IsActive && x.UserLogin!.IsActive &&
            x.UserLogin.Username == username && x.AppRole!.IsActive).Select(x => x.FkRoleId);
        var permissions = await _db.AppMenuRole.Where(x => x.IsActive && x.AppMenu!.IsActive &&
            x.AppMenu.Controller == "UserFarm" && x.FkAppRoleId.HasValue && roleIds.Contains(x.FkAppRoleId.Value))
            .Select(x => x.AccessTypes).ToListAsync();
        return permissions.Any(x => (x ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Contains(access, StringComparer.OrdinalIgnoreCase));
    }

    [HttpPost("DxGrid"), AuthorizeFilter(Scope = "UserFarm.View")]
    public async Task<IActionResult> DxGridAsync([FromBody] DataSourceLoadOptions param)
    {
        if (!await CanAsync("view")) return Forbid();
        return new OK("Success get user farms", await DxDataGridExtentions.DxDataGridAsync(Query(), param)).ReturnResponse();
    }

    [HttpGet("{id:guid}"), AuthorizeFilter(Scope = "UserFarm.View")]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        if (!await CanAsync("view") && !await CanAsync("detail") && !await CanAsync("edit")) return Forbid();
        var data = await Query().FirstOrDefaultAsync(x => x.Id == id);
        return data == null ? new NotFound("User farm was not found.", new { id }).ReturnResponse()
            : new OK("Success get user farm", data).ReturnResponse();
    }

    [HttpGet("Options"), AuthorizeFilter(Scope = "UserFarm.View")]
    public async Task<IActionResult> OptionsAsync()
    {
        if (!await CanAsync("add") && !await CanAsync("edit")) return Forbid();
        // Never serialize UserLogin entities: they contain password hashes.
        var users = await _db.UserLogin.Where(x => x.IsActive).OrderBy(x => x.FullName)
            .Select(x => new { x.Id, x.Username, Name = x.FullName, x.Email }).ToListAsync();
        var farms = await _db.Farm.Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Code, x.Name }).ToListAsync();
        return new OK("Success get options", new { Users = users, Farms = farms }).ReturnResponse();
    }

    [HttpPost, AuthorizeFilter(Scope = "UserFarm.Add")]
    public async Task<IActionResult> PostAsync([FromBody] UserFarmDto data)
    {
        if (!await CanAsync("add")) return Forbid();
        return await SaveAsync(data, false);
    }

    [HttpPut, AuthorizeFilter(Scope = "UserFarm.Edit")]
    public async Task<IActionResult> PutAsync([FromBody] UserFarmDto data)
    {
        if (!await CanAsync("edit")) return Forbid();
        return await SaveAsync(data, true);
    }

    private async Task<IActionResult> SaveAsync(UserFarmDto data, bool editing)
    {
        var entity = editing ? await _db.UserFarm.AsTracking().FirstOrDefaultAsync(x => x.Id == data.Id &&
            (!_db.FarmScopeRequired || _db.AssignedFarmIds.Contains(x.FkFarmId))) : new UserFarm { Id = Guid.NewGuid() };
        if (entity == null) return new NotFound("User farm was not found.", new { data.Id }).ReturnResponse();
        if (!data.FkUserId.HasValue || data.FkUserId == Guid.Empty || !data.FkFarmId.HasValue || data.FkFarmId == Guid.Empty)
            return new BadRequest("User and farm are required.", new { data.Id }).ReturnResponse();
        var userExists = await _db.UserLogin.AnyAsync(x => x.Id == data.FkUserId && (x.IsActive || (editing && entity.FkUserId == x.Id)));
        var farmExists = await _db.Farm.AnyAsync(x => x.Id == data.FkFarmId && (x.IsActive || (editing && entity.FkFarmId == x.Id)));
        if (!userExists || !farmExists)
            return new BadRequest("Select an existing active user and farm.", new { data.Id }).ReturnResponse();
        if (await _db.UserFarm.AnyAsync(x => x.Id != entity.Id && x.FkUserId == data.FkUserId && x.FkFarmId == data.FkFarmId))
            return new Conflict("This user is already assigned to this farm. Edit the existing assignment instead.", new { data.Id }).ReturnResponse();
        entity.FkUserId = data.FkUserId.Value;
        entity.FkFarmId = data.FkFarmId.Value;
        entity.IsActive = data.IsActive;
        if (!editing) _db.UserFarm.Add(entity);
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
        {
            return new Conflict("This user is already assigned to this farm.", new { data.Id }).ReturnResponse();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number == 547)
        {
            return new Conflict("The selected user or farm no longer exists. Reload and try again.", new { data.Id }).ReturnResponse();
        }
        return new OK("Success save user farm", await Query().FirstOrDefaultAsync(x => x.Id == entity.Id)).ReturnResponse();
    }

    [HttpDelete("{id:guid}"), AuthorizeFilter(Scope = "UserFarm.Delete")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        if (!await CanAsync("delete")) return Forbid();
        var entity = await _db.UserFarm.AsTracking().FirstOrDefaultAsync(x => x.Id == id &&
            (!_db.FarmScopeRequired || _db.AssignedFarmIds.Contains(x.FkFarmId)));
        if (entity == null) return new NotFound("User farm was not found.", new { id }).ReturnResponse();
        _db.UserFarm.Remove(entity);
        await _db.SaveChangesAsync();
        return new OK("Success delete user farm", new { id }).ReturnResponse();
    }
}
