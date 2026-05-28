using Rindo.Domain.DTO;
using Rindo.Domain.DTO.Auth;
using Rindo.Domain.DTO.Projects;
using Rindo.Domain.DTO.Roles;
using Rindo.Domain.DTO.Tasks;
using Rindo.Domain.DataObjects;

namespace Application.Common.Mapping;

public static class MapExtension
{
    public static UserDto MapToDto(this User user)
    {
        return new UserDto
        {
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Id = user.UserId
        };
    }

    public static StageDto MapToDto(this Stage stage)
    {
        return new StageDto
        {
            Id = stage.StageId,
            ProjectId = stage.ProjectId,
            CustomName = stage.CustomName,
            Type = stage.Type,
            Index = stage.Index,
            Tasks = stage.Tasks.Select(MapToDto).ToList(),
        };
    }

    public static UserShortInfoDto MapToShortDto(this User user)
    {
        return new UserShortInfoDto
        {
            Id = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
        };
    }

    public static User MapToModel(this SignUpDto dto)
    {
        return new User
        {
            Username = dto.Username,
            Password = dto.Password,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName
        };
    }

    public static RoleDto MapToDto(this Role role)
    {
        return new RoleDto
        {
            BitRolesRights = role.BitPermissions,
            Name = role.Name,
            Id = role.RoleId,
            //Users = role.Users,
        };
    }

    public static ProjectTaskDto MapToDto(this ProjectTask projectTask)
    {
        return new ProjectTaskDto
        {
            TaskId = projectTask.TaskId,
            ProjectId = projectTask.ProjectId,
            Name = projectTask.Name,
            TaskNumber = projectTask.TaskNumber,
            Description = projectTask.Description,
            Priority = projectTask.Priority,
            DeadlineDate = projectTask.DeadlineDate,
            Created = projectTask.Created,
            StageId = projectTask.StageId,
            Index = projectTask.Index,
            Assignee = projectTask.AssigneeId.HasValue ? new UserShortInfoDto
            {
                Id = projectTask.AssigneeId.Value,
            } : null, 
        };
    }

    public static ProjectTask MapFromDto(this ProjectTaskDto projectTaskDto)
    {
        return new ProjectTask
        {
            TaskId = projectTaskDto.TaskId,
            ProjectId = projectTaskDto.ProjectId,
            Name = projectTaskDto.Name,
            Description = projectTaskDto.Description,
            Priority = projectTaskDto.Priority,
            DeadlineDate = projectTaskDto.DeadlineDate,
            Index = projectTaskDto.Index,
            StageId = projectTaskDto.StageId,
            AssigneeId = projectTaskDto.Assignee?.Id,
            Created = projectTaskDto.Created,
        };
    }

    public static Stage MapToDto(this StageOnCreateDto dto)
    {
        return new Stage
        {
            ProjectId = dto.ProjectId,
            CustomName = dto.Name
        };
    }
}