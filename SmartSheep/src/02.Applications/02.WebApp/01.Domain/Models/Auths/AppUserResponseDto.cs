namespace Web.Domain.Models.Auths
{
    public class AppUserResponseDto
    {
        public string? ApplicationId { get; set; }
        public string? UserId { get; set; }
        public string? FullName { get; set; }
        public string? Username { get; set; }
        public string? Photo { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WaNumber { get; set; }
        public bool IsSuccessResponse { get; set; } = false;
        public string? Roles { get; set; }
        public List<string> ListRoles { get; set; } = new List<string>();
        public NavMenuDto Menus { get; set; } = new NavMenuDto();
        public List<UserFarmProfileDto> Farms { get; set; } = new();
    }

    public class UserFarmProfileDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
