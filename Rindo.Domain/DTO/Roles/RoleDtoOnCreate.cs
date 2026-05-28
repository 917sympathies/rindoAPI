namespace Rindo.Domain.DTO.Roles;

public class RoleDtoOnCreate
{
    public string Name { get; set; }
    public Guid ProjectId { get; set; }
    public string Color { get; set; }
}