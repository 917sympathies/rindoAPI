using Rindo.Domain.DTO.Roles;

namespace Rindo.Domain.DTO.Projects;

public class ProjectDto
{
    public Guid Id { get; init;}
    public string Name { get; init; }
    public string Description { get; init; }
    public Guid OwnerId { get; init; }
    public UserDto[] Users { get; init; }
    public RoleDto[] Roles { get; init; }
    public DateTimeOffset Created { get; init; } 
    public DateTimeOffset? DeadlineDate { get; init; }
}