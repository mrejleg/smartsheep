#:package Microsoft.Data.SqlClient@6.1.1
#:property PublishAot=false
using System.Text.Json;
using Microsoft.Data.SqlClient;

// Explicit local data update, never run by startup seed.
// dotnet run --file tools/UseDefaultUserFarmIcon.cs -- <API-config-path> [--apply]
using var config = JsonDocument.Parse(File.ReadAllText(args[0]));
await using var connection = new SqlConnection(config.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString());
await connection.OpenAsync();
await using var command = connection.CreateCommand();
if (args.Contains("--apply")) {
    command.CommandText = """
        IF (SELECT COUNT(*) FROM dbo.AppMenu WHERE Controller='UserLogin') <> 1
            THROW 51000, 'Expected one User Login reference menu.', 1;
        IF (SELECT COUNT(*) FROM dbo.AppMenu WHERE Controller='UserFarm') <> 1
            THROW 51000, 'Expected one User Farms menu.', 1;
        UPDATE target SET Icon=reference.Icon, IconActive=reference.IconActive,
            ModifiedBy='user-farm-default-icon', DateModified=GETDATE()
        FROM dbo.AppMenu target CROSS JOIN dbo.AppMenu reference
        WHERE target.Controller='UserFarm' AND reference.Controller='UserLogin'
            AND (ISNULL(target.Icon,'')<>ISNULL(reference.Icon,'') OR ISNULL(target.IconActive,'')<>ISNULL(reference.IconActive,''));
        """;
    Console.WriteLine($"Updated menus: {await command.ExecuteNonQueryAsync()}");
}
command.CommandText = "SELECT Name,Icon,IconActive FROM dbo.AppMenu WHERE Controller IN ('UserFarm','UserLogin') ORDER BY Controller";
await using var rows = await command.ExecuteReaderAsync();
while(await rows.ReadAsync()) Console.WriteLine($"{rows.GetString(0)}: Icon={(rows.IsDBNull(1) ? "default" : rows.GetString(1))}; IconActive={(rows.IsDBNull(2) ? "default" : rows.GetString(2))}");
