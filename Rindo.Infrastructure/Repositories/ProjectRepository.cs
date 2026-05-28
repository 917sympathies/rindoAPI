using System.Linq.Expressions;
using Application.Interfaces.Access;
using Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Rindo.Domain.DTO.Projects;
using Rindo.Domain.DataObjects;
using Task = System.Threading.Tasks.Task;

namespace Rindo.Infrastructure.Repositories;

public class ProjectRepository(PostgresDbContext context, IDataAccessController dataAccessController) : RepositoryBase<Project>(context), IProjectRepository
{
    private readonly PostgresDbContext _context = context;
    public Task<Project> CreateProject(Project project) => CreateAsync(project);

    public Task DeleteProject(Project project) => Delete(project);

    public async Task UpdateProject(UpdateProjectDto updateProjectDto)
    {
        await _context.Projects
            .Where(x => x.ProjectId == updateProjectDto.ProjectId && dataAccessController.AccessibleProjectsIds.Contains(x.ProjectId))
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(x => x.Name, updateProjectDto.Name)
                .SetProperty(x => x.Description, updateProjectDto.Description)
            );
        await _context.SaveChangesAsync();
    }

    public async Task<Project?> GetProjectById(Guid projectId) => 
        await FindByCondition(p => p.ProjectId == projectId && dataAccessController.AccessibleProjectsIds.Contains(p.ProjectId))
            .Include(p => p.Users)
            .Include(p => p.Roles)
            .Include(p => p.Stages.OrderBy(s => s.Index))
            .ThenInclude(p => p.Tasks.OrderBy(t => t.Index))
            .FirstOrDefaultAsync();
    
    public async Task<ProjectHeaderInfoDto?> GetProjectHeaderInfo(Guid projectId) =>
        await _context.Projects
            .Where(x => x.ProjectId == projectId && dataAccessController.AccessibleProjectsIds.Contains(x.ProjectId))
            .Select(x => new ProjectHeaderInfoDto
            {
                Name = x.Name,
                OwnerId = x.OwnerId,
                ChatId = x.ChatId,
            })
            .FirstOrDefaultAsync();

    public async Task AddUserToProject(Guid projectId, Guid userId)
    {
        await _context.Database.ExecuteSqlAsync($"INSERT INTO dbo.projects_to_users VALUES({projectId}, {userId})");
    }
    
    public async Task RemoveUserFromProject(Guid projectId, Guid userId)
    {
        await _context.Database.ExecuteSqlAsync($"DELETE FROM dbo.projects_to_users WHERE projects_project_id = {projectId} AND users_user_id = {userId}");
    }

    public async Task<IEnumerable<Project>> GetProjectsByIds(Guid[] projectsIds)
    {
        var unionProjectsIds = projectsIds.Intersect(dataAccessController.AccessibleProjectsIds);
        return await FindByCondition(p => unionProjectsIds.Contains(p.ProjectId)).ToArrayAsync();
    }

    public async Task<IEnumerable<Project>> GetProjectsOwnedByUser(Guid userId) =>
        await FindByCondition(p => p.OwnerId == userId).ToListAsync();

    public async Task<Project?> GetProjectByIdWithUsers(Guid projectId) =>
        await FindByCondition(p => p.ProjectId == projectId && dataAccessController.AccessibleProjectsIds.Contains(p.ProjectId))
            .Include(x => x.Users)
            .FirstOrDefaultAsync();

    public async Task UpdateProperty<TProperty>(Project project, Expression<Func<Project, TProperty>> expression) =>
        await UpdatePropertyAsync(project, expression);
    
    public async Task UpdateCollection<TProperty>(Project project, Expression<Func<Project, IEnumerable<TProperty>>> expression) where TProperty: class =>
        await UpdateCollectionAsync(project, expression);

    public async Task<IEnumerable<Project>> GetProjectsWhereUserAttends(Guid userId) =>
        await FindByCondition(p => p.Users.Any(x => x.UserId == userId) || p.OwnerId == userId).ToListAsync();
}