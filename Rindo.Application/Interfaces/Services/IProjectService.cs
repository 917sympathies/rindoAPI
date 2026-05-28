using Rindo.Domain.DTO;
using Rindo.Domain.DTO.Projects;
using Rindo.Domain.DTO.Users;
using Rindo.Domain.DataObjects;

namespace Application.Interfaces.Services;

public interface IProjectService
{
    Task<IEnumerable<ProjectShortInfoDto>> GetProjectsWhereUserAttends(Guid userId);
    Task<ProjectDto?> GetProjectById(Guid projectId);
    Task<ProjectDto> GetProjectSettings(Guid projectId);
    Task<ProjectHeaderInfoDto?> GetProjectsInfoForHeader(Guid projectId);
    Task<User> InviteUserToProject(Guid projectId, string username);
    Task AddUserToProject(Guid projectId, Guid userId);
    Task<Guid> CreateProject(ProjectOnCreateDto projectOnCreateDto);
    Task RemoveUserFromProject(Guid projectId, string username);
    Task DeleteProject(Guid projectId);
    Task UpdateProject(UpdateProjectDto updateProjectDto);
    Task<UserProjectsTasks[]> GetProjectsWithUserTasks(Guid userId);
}