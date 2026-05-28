using Application.Common.Exceptions;
using Application.Common.Mapping;
using Application.Interfaces.Access;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.Transactions;
using Mapster;
using Rindo.Domain.DTO;
using Rindo.Domain.DTO.Projects;
using Rindo.Domain.DTO.Users;
using Rindo.Domain.Enums;
using Rindo.Domain.DataObjects;

namespace Application.Services;

public class ProjectService(
    IProjectRepository projectRepository,
    IUserRepository userRepository,
    IChatService chatService,
    IInvitationRepository invitationRepository,
    ITaskRepository taskRepository,
    IRoleRepository roleRepository,
    IDataAccessController dataAccessController,
    IDataTransactionService dataTransactionService) : IProjectService
{
    public async Task<Guid> CreateProject(ProjectOnCreateDto projectOnCreateDto)
    {
        await dataTransactionService.BeginTransactionAsync();
        var chatId = await chatService.Create(new Chat());
        var owner = await userRepository.GetUserById(dataAccessController.EmployeeId);
        var project = new Project
        {
            Name = projectOnCreateDto.Name,
            Description = projectOnCreateDto.Description,
            OwnerId = dataAccessController.EmployeeId,
            Created = DateTime.UtcNow,
            Stages = [
                new Stage { Index = (int)StageType.ToDo, Type = StageType.ToDo },
                new Stage { Index = (int)StageType.InProgress, Type = StageType.InProgress },
                new Stage { Index = (int)StageType.ReadyToTest, Type = StageType.ReadyToTest },
                new Stage { Index = (int)StageType.Testing, Type = StageType.Testing },
                new Stage { Index = (int)StageType.Closed, Type = StageType.Closed },
            ],
            ChatId = chatId,
            DeadlineDate = projectOnCreateDto.DeadlineDate,
            Users = [owner!]
        };
        var addedProject = await projectRepository.CreateProject(project);
        await dataTransactionService.CommitTransactionAsync();
        return addedProject.ProjectId;
    }

    public async Task<ProjectDto?> GetProjectById(Guid projectId)
    {
        var project = await projectRepository.GetProjectById(projectId);
        return project is null ? throw new NotFoundException(nameof(Project), projectId) : project.Adapt<ProjectDto>();
    }

    public async Task<ProjectDto> GetProjectSettings(Guid projectId)
    {
        var project = await projectRepository.GetProjectById(projectId);
        return project == null ? throw new NotFoundException(nameof(Project), projectId) : project.Adapt<ProjectDto>();
            // // weird logic
            // var projectOwner = await userRepository.GetUserById(project.OwnerId);
            // project.Users.Add(projectOwner!);
    }

    public async Task<IEnumerable<ProjectShortInfoDto>> GetProjectsWhereUserAttends(Guid userId)
    {
        return (await projectRepository.GetProjectsWhereUserAttends(userId)).Select(project => project.Adapt<ProjectShortInfoDto>());
    }

    public async Task<User> InviteUserToProject(Guid projectId, string username)
    {
        var senderId = dataAccessController.EmployeeId;
        var user = await userRepository.GetUserByUsername(username);
        if(user is null) throw new NotFoundException($"User with username = {username} was not found");
        var project = await projectRepository.GetProjectById(projectId);
        if(project is null) throw new NotFoundException(nameof(Project), projectId);
        var sender = await userRepository.GetUserById(senderId);
        if(sender is null) throw new NotFoundException(nameof(User), senderId);
                    
        var invitation = new Invitation { RecipientId = user.UserId, ProjectId = projectId, SenderId = sender.UserId };
        await invitationRepository.CreateInvitation(invitation);
        return user;
    }

    public async Task AddUserToProject(Guid projectId, Guid userId)
    {
        var project = await projectRepository.GetProjectByIdWithUsers(projectId);
        if(project is null) throw new NotFoundException(nameof(Project), projectId);
        var user = await userRepository.GetUserById(userId);
        if(user is null) throw new NotFoundException(nameof(User), userId);
        var invitation = (await invitationRepository.GetInvitationsByUserId(userId)).FirstOrDefault(x => x.ProjectId == projectId);
        if(invitation is null) throw new NotFoundException<Invitation>($"Invitation from project with id = {projectId} to user with id = ${userId} could not be found");
        
        await dataTransactionService.BeginTransactionAsync();
        await projectRepository.AddUserToProject(projectId, userId);
        await invitationRepository.DeleteInvitation(invitation);
        await dataTransactionService.CommitTransactionAsync();
    }

    public async Task RemoveUserFromProject(Guid projectId, string username)
    {
        var user = await userRepository.GetUserByUsername(username);
        if (user is null) throw new NotFoundException<User>($"User with username = {username} was not found");
        
        await dataTransactionService.BeginTransactionAsync();
        await taskRepository.UnassignTasksFromUser(projectId, user.UserId);
        await projectRepository.RemoveUserFromProject(projectId, user.UserId);
        await dataTransactionService.CommitTransactionAsync();
    }

    public Task<ProjectHeaderInfoDto?> GetProjectsInfoForHeader(Guid projectId)
    {
        return projectRepository.GetProjectHeaderInfo(projectId);
    }

    public Task UpdateProject(UpdateProjectDto updateProjectDto)
    {
        return projectRepository.UpdateProject(updateProjectDto);
    }

    public async Task DeleteProject(Guid projectId)
    {
        var project = await projectRepository.GetProjectById(projectId);
        if (project is null) throw new NotFoundException(nameof(Project), projectId);
        //await dataTransactionService.BeginTransactionAsync();
        
        // TODO: do this with cascade delete
        // await taskService.DeleteTasksByProjectId(projectId);
        // await chatService.DeleteById(projectId);
        // await roleService.RemoveRolesByProjectId(projectId);
        await projectRepository.DeleteProject(project);
        //await dataTransactionService.CommitAsync();
    }

    public async Task<UserProjectsTasks[]> GetProjectsWithUserTasks(Guid userId)
    {
        var user = await userRepository.GetUserById(userId);
        if (user is null) throw new NotFoundException(nameof(User), userId);
        var tasks = await taskRepository.GetTasksAssignedToUser(userId);
        var projects = await projectRepository.GetProjectsWhereUserAttends(userId);
        return projects.Select(p => new UserProjectsTasks
        {
            ProjectId = p.ProjectId,
            ProjectName = p.Name, 
            Tasks = tasks.Where(t => t.ProjectId == p.ProjectId).Select(x => x.MapToDto()).ToArray()
        }).ToArray();
    }
}