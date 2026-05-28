using Application.Common.Exceptions;
using Application.Common.Mapping;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Mapster;
using Rindo.Domain.DTO;

namespace Application.Services;

public class UserService(IUserRepository userRepository, IProjectRepository projectRepository) : IUserService
{
    public async Task<UserDto> GetUserById(Guid id)
    {
        var user = await userRepository.GetUserById(id);
        return user is null ? throw new NotFoundException("User with this id doesn't exists") : user.Adapt<UserDto>();
    }

    public async Task UpdateUser(UserDto userDto)
    {
        var user = await userRepository.GetUserById(userDto.Id);
        if (user is null) throw new NotFoundException(nameof(user), userDto.Id);
        user.FirstName = userDto.FirstName;
        user.LastName = userDto.LastName;
        user.Email = userDto.Email;
        await userRepository.UpdateUser(user);
    }

    public async Task<IEnumerable<UserDto>> GetUsersByProjectId(Guid projectId)
    {
        var project = await projectRepository.GetProjectById(projectId);
        if (project is null) throw new NotFoundException("Project with this id doesn't exists");
        
        var users = project.Users?.ToList() ?? [];
        var owner = await userRepository.GetUserById(project.OwnerId);
        users.Add(owner);
        return users.Select(user => user.Adapt<UserDto>()).ToArray();
    }
}