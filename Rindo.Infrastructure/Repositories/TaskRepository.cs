using System.Linq.Expressions;
using Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Rindo.Domain.DTO;
using Rindo.Domain.DTO.Projects;
using Rindo.Domain.DataObjects;

namespace Rindo.Infrastructure.Repositories;

public class TaskRepository(PostgresDbContext context) : RepositoryBase<ProjectTask>(context), ITaskRepository
{
    private readonly PostgresDbContext _context = context;
    public Task<ProjectTask> CreateTask(ProjectTask projectTask) => CreateAsync(projectTask);

    public Task DeleteTask(ProjectTask projectTask) => Delete(projectTask);

    public async Task UpdateTask(UpdateTaskDto projectTask, Guid modifiedBy, DateTime modified)
    {
        await _context.Tasks
            .Where(x => x.TaskId == projectTask.TaskId)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(x => x.AssigneeId, projectTask.AssigneeId)
                .SetProperty(x => x.Priority, projectTask.Priority)
                .SetProperty(x => x.StageId, projectTask.StageId)
                .SetProperty(x => x.Description, projectTask.Description)
                .SetProperty(x => x.Name, projectTask.Name)
                .SetProperty(x => x.Index, projectTask.Index)
                .SetProperty(x => x.DeadlineDate, projectTask.DeadlineDate)
                .SetProperty(x => x.ModifiedBy, modifiedBy)
                .SetProperty(x => x.Modified, modified)
            );
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetNextTaskNumber(Guid projectId) => await _context.Tasks.CountAsync(x => x.ProjectId == projectId) + 1;

    public async Task UnassignTasksFromUser(Guid projectId, Guid userId)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.project_tasks SET assignee_id = NULL WHERE assignee_id = {userId}");
    }
    
    public async Task DeleteTasksByProjectId(Guid projectId)
    {
        // TODO: maybe we shouldn't delete records but mark them as 'Deleted' with IsDeleted flag
        await _context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.project_tasks WHERE project_id = {projectId}");
    }

    public async Task UpdateTaskStage(Guid taskId, Guid stageId)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.project_tasks SET stage_id = {stageId} WHERE task_id = {taskId}");
    }

    public async Task<IEnumerable<ProjectTask>> GetTasksByProjectId(Guid projectId) =>
        await FindByCondition(t => t.ProjectId == projectId).AsNoTracking().OrderBy(t => t.Created).ToListAsync();

    public async Task<IEnumerable<ProjectTask>> GetTasksInProjectAssignedToUser(Guid projectId, Guid userId) =>
        await FindByCondition(t => t.AssigneeId == userId && t.ProjectId == projectId).AsNoTracking().ToListAsync();
    
    public async Task<IEnumerable<ProjectTask>> GetTasksAssignedToUser(Guid userId) =>
        await FindByCondition(t => t.AssigneeId == userId).AsNoTracking().ToListAsync();

    public async Task<IEnumerable<ProjectTask>> GetTasksByStageId(Guid processId) =>
        await FindByCondition(t => t.StageId == processId).AsNoTracking().ToListAsync();

    public async Task<ProjectTask?> GetById(Guid id) =>
        await FindByCondition(t => t.TaskId == id)
            .Include(t => t.Comments)
            .AsNoTracking()
            .FirstOrDefaultAsync();

    public async Task UpdateProperty<TProperty>(ProjectTask projectTask, Expression<Func<ProjectTask, TProperty>> expression) =>
        await UpdatePropertyAsync<TProperty>(projectTask, expression);
}