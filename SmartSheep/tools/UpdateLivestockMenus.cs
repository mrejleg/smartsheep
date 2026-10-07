#:package Microsoft.Data.SqlClient@6.1.1
#:property PublishAot=false
using System.Text.Json;
using Microsoft.Data.SqlClient;

// Explicit, one-time menu update. Never called by application startup/seed.
// Usage: dotnet run --file tools/UpdateLivestockMenus.cs -- <API-config-path> [--apply]
using var config = JsonDocument.Parse(File.ReadAllText(args[0]));
await using var connection = new SqlConnection(config.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString());
await connection.OpenAsync();
await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
await using var command = connection.CreateCommand();
command.Transaction = transaction;
command.CommandText = """
SELECT Id, Name, Controller, FkParentId, SequenceNumber, Icon, IconActive, IsMobile, IsActive, AccessTypes, ModifiedBy, DateModified
FROM dbo.AppMenu WHERE Controller IN ('SucklingActivity','SucklingStatistic','SheepBreeding') OR Name = 'Livestocks'
ORDER BY SequenceNumber;
""";
await using (var reader = await command.ExecuteReaderAsync())
{
    var rows = new List<Dictionary<string,object?>>();
    while (await reader.ReadAsync())
    {
        var row = new Dictionary<string,object?>();
        for (var i=0; i<reader.FieldCount; i++) row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        rows.Add(row);
    }
    Console.WriteLine(JsonSerializer.Serialize(rows));
}
if (!args.Contains("--apply")) { await transaction.RollbackAsync(); return; }
string Svg(string body) => "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24\" height=\"24\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">" + body + "</svg>";
command.Parameters.AddWithValue("@activity", Svg("<polyline points=\"22 12 18 12 15 21 9 3 6 12 2 12\"/>"));
command.Parameters.AddWithValue("@statistics", Svg("<line x1=\"18\" y1=\"20\" x2=\"18\" y2=\"10\"/><line x1=\"12\" y1=\"20\" x2=\"12\" y2=\"4\"/><line x1=\"6\" y1=\"20\" x2=\"6\" y2=\"14\"/>"));
command.Parameters.AddWithValue("@breeding", Svg("<path d=\"M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z\"/>"));
command.CommandText = """
IF (SELECT COUNT(*) FROM dbo.AppMenu WHERE Name='Livestocks' AND Controller IS NULL AND IsActive=1) <> 1
    THROW 51000, 'Expected exactly one active Livestocks parent.', 1;
IF (SELECT COUNT(*) FROM dbo.AppMenu WHERE Controller IN ('SucklingActivity','SucklingStatistic')) <> 2
    THROW 51000, 'Expected the two suckling menus.', 1;
UPDATE dbo.AppMenu SET Icon=@activity, IconActive=@activity, ModifiedBy='livestock-menu-update', DateModified=GETDATE()
WHERE Controller='SucklingActivity' AND (ISNULL(Icon,'')<>@activity OR ISNULL(IconActive,'')<>@activity);
UPDATE dbo.AppMenu SET Icon=@statistics, IconActive=@statistics, ModifiedBy='livestock-menu-update', DateModified=GETDATE()
WHERE Controller='SucklingStatistic' AND (ISNULL(Icon,'')<>@statistics OR ISNULL(IconActive,'')<>@statistics);
IF NOT EXISTS (SELECT 1 FROM dbo.AppMenu WHERE Controller='SheepBreeding' OR Name='Sheep Breeding')
BEGIN
    DECLARE @id uniqueidentifier=NEWID();
    DECLARE @parent uniqueidentifier=(SELECT Id FROM dbo.AppMenu WHERE Name='Livestocks' AND Controller IS NULL AND IsActive=1);
    INSERT dbo.AppMenu (Id,Name,Controller,SequenceNumber,FkParentId,Icon,IconActive,IsSection,IsMobile,AccessTypes,IsActive,CreatedBy,ModifiedBy,DateCreated,DateModified)
    VALUES (@id,'Sheep Breeding','SheepBreeding','b.10.3',@parent,@breeding,@breeding,0,1,'view',1,'livestock-menu-update','livestock-menu-update',GETDATE(),GETDATE());
    -- Only new menu visibility; existing roles, client scopes and permissions are unchanged.
    INSERT dbo.AppMenuRole (Id,FkAppMenuId,FkAppRoleId,AccessTypes,IsActive,CreatedBy,ModifiedBy,DateCreated,DateModified)
    SELECT NEWID(),@id,r.FkAppRoleId,'view',1,'livestock-menu-update','livestock-menu-update',GETDATE(),GETDATE()
    FROM dbo.AppMenuRole r WHERE r.FkAppMenuId=@parent AND r.IsActive=1 AND r.FkAppRoleId IS NOT NULL
        AND EXISTS (SELECT 1 FROM STRING_SPLIT(r.AccessTypes,',') a WHERE LTRIM(RTRIM(a.value))='view')
    GROUP BY r.FkAppRoleId;
END
SELECT Name,Controller,IsMobile,Icon,IconActive,FkParentId FROM dbo.AppMenu
WHERE Controller IN ('SucklingActivity','SucklingStatistic','SheepBreeding') ORDER BY SequenceNumber;
""";
await using (var reader = await command.ExecuteReaderAsync())
    while (await reader.ReadAsync()) Console.WriteLine($"Updated: {reader.GetString(0)} | IsMobile={reader.GetBoolean(2)} | IconActiveMatches={reader.GetString(3)==reader.GetString(4)}");
await transaction.CommitAsync();
