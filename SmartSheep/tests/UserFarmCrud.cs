#:project ../src/02.Applications/01.WebApi/04.Api/04.Api.csproj
#:package Microsoft.EntityFrameworkCore.InMemory@10.0.5
#:property PublishAot=false

using Api.Controllers.Configs;
using Api.Domain.Entities.Auths;
using Api.Domain.Entities.Configs;
using Api.Domain.Entities.MasterDatas;
using Api.Domain.Models.Auths;
using Api.Infrastructure.Data.DbContexts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models.Jwts;
using Project.Core.Interfaces.Jwt;
using System.Text.Json;

// Isolated test database: never opens the application's database or Firebase.
await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new HttpContextAccessor());
var user = new UserLogin { Id = Guid.NewGuid(), Username = "test-user", FullName = "Test User", Email = "test@example.invalid", Password = "never-return-this-hash", IsActive = true };
var farm = new Farm { Id = Guid.NewGuid(), Code = "TEST-A", Name = "Test Farm A", IsActive = true };
var farm2 = new Farm { Id = Guid.NewGuid(), Code = "TEST-B", Name = "Test Farm B", IsActive = true };
var role = new AppRole { Id = Guid.NewGuid(), Name = "TEST", IsActive = true };
var menu = new AppMenu { Id = Guid.NewGuid(), Controller = "UserFarm", Name = "User Farms", SequenceNumber = "test.1", AccessTypes = "view,add,edit,delete,detail", IsActive = true };
var permission = new AppMenuRole { Id = Guid.NewGuid(), FkAppMenuId = menu.Id, FkAppRoleId = role.Id, AccessTypes = menu.AccessTypes, IsActive = true };
db.AddRange(user, farm, farm2, role, menu, permission,
    new UserLoginRole { Id = Guid.NewGuid(), FkUserId = user.Id, FkRoleId = role.Id, IsActive = true });
await db.SaveChangesAsync();
var controller = new UserFarmController(db, new TestJwt());
void Check(bool value, string label) { if (!value) throw new Exception(label); Console.WriteLine("PASS: " + label); }
int? Status(IActionResult result) => (result as ObjectResult)?.StatusCode;

var empty = await controller.PostAsync(new UserFarmDto { FkUserId = Guid.Empty, FkFarmId = farm.Id });
Check(Status(empty) == 400, "Empty user IDs are rejected");
Check(Status(await controller.PostAsync(new UserFarmDto { FkUserId = user.Id, FkFarmId = Guid.NewGuid() })) == 400, "Unknown farm is rejected");
var input = new UserFarmDto { FkUserId = user.Id, FkFarmId = farm.Id };
Check(Status(await controller.PostAsync(input)) == 200, "Create assignment");
var row = await db.UserFarm.SingleAsync();
Check(row.IsActive && row.DateCreated.HasValue, "Default active and server audit fields are saved");
Check(Status(await controller.PostAsync(input)) == 409, "Duplicate user/farm pair is rejected");
Check(Status(await controller.PostAsync(new UserFarmDto { FkUserId = user.Id, FkFarmId = farm2.Id })) == 200, "A user can have multiple farms");
var fetched = await controller.GetAsync(row.Id);
var json = JsonSerializer.Serialize(((ObjectResult)fetched).Value);
Check(json.Contains("Test Farm A") && !json.Contains("never-return-this-hash") && !json.Contains("Password"), "Details project safe user and farm fields only");
var options = JsonSerializer.Serialize(((ObjectResult)await controller.OptionsAsync()).Value);
Check(options.Contains("Test User") && !options.Contains("Password") && !options.Contains("never-return-this-hash"), "Dropdowns never expose password data");
Check(options.Contains("\"Username\":\"test-user\"") && options.Contains("\"Name\":\"Test User\"")
    && options.Contains("\"Email\":\"test@example.invalid\""), "User dropdown exposes separate Username, Name and Email fields");
Check(options.Contains("\"Code\":\"TEST-A\"") && options.Contains("\"Name\":\"Test Farm A\""),
    "Farm dropdown exposes separate Code and Name fields");
Check(Status(await controller.PutAsync(new UserFarmDto { Id = row.Id, FkUserId = user.Id, FkFarmId = farm2.Id })) == 409, "Duplicate edit is rejected");
Check(Status(await controller.PutAsync(new UserFarmDto { Id = row.Id, FkUserId = user.Id, FkFarmId = farm.Id, IsActive = false })) == 200 && !row.IsActive, "Edit assignment status");
Check(Status(await controller.PutAsync(new UserFarmDto { Id = Guid.NewGuid(), FkUserId = user.Id, FkFarmId = farm.Id })) == 404, "Missing edit returns not found");
permission.AccessTypes = "view";
await db.SaveChangesAsync();
Check(await controller.PostAsync(input) is ForbidResult && await controller.DeleteAsync(row.Id) is ForbidResult, "Read-only role cannot add or delete assignments");
permission.AccessTypes = "view,add,edit,delete,detail";
await db.SaveChangesAsync();
Check(Status(await controller.DeleteAsync(row.Id)) == 200, "Delete assignment");
Check(await db.UserLogin.CountAsync() == 1 && await db.Farm.CountAsync() == 2, "Deleting assignment preserves users and farms");
Check(Status(await controller.GetAsync(row.Id)) == 404 && Status(await controller.DeleteAsync(row.Id)) == 404, "Missing details/delete return not found");
var mapping = db.Model.FindEntityType(typeof(UserFarm))!;
Check(mapping.GetIndexes().Any(x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(new[] { "FkUserId", "FkFarmId" })), "Database model enforces unique pair");
Check(mapping.GetForeignKeys().All(x => x.DeleteBehavior == DeleteBehavior.Restrict), "Foreign keys restrict parent deletion");

sealed class TestJwt : IJwtService
{
    public string GetUsername() => "test-user";
    public string GetClaimData(string name) => "";
    public string GetApplicationId() => "";
    public string GetClientId() => "test";
    public string GetEmail() => "";
    public string GetFullName() => "";
    public string GetRoles() => "TEST";
    public string GetCostCenterCode() => "";
    public string GetEaCode() => "";
    public JwtResponse GetJwt(JwtRequest request) => throw new NotSupportedException();
    public JwtResponse GetJwtLongExpires(JwtRequest request) => throw new NotSupportedException();
}
