namespace Web.Domain.Models.Configs;

public class UserFarmOptions
{
    public List<UserFarmUserOption> Users { get; set; } = new();
    public List<UserFarmFarmOption> Farms { get; set; } = new();
}

public class UserFarmUserOption
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UserFarmFarmOption
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
