using Application.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace Rindo.Chat;

public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly ICommentService _commentService;
    private readonly IProjectService _projectService;
    private readonly IInvitationService _invitationService;

    public ChatHub(IMessageService messageService, ICommentService commentService, IProjectService projectService, IInvitationService invitationService)
    {
        _messageService = messageService;
        _commentService = commentService;
        _projectService = projectService;
        _invitationService = invitationService;
    }
    
    public async Task SendProjectChat(string userId, string message, string chatId)
    {
        var parsedUserId = Guid.Parse(userId);
        var parsedChatId = Guid.Parse(chatId);
        var (msg, username) = await _messageService.AddMessage(parsedUserId, parsedChatId, message);
        await Clients.All.SendAsync($"ReceiveProjectChat{chatId}", msg.MessageId, username, message, msg.ChatId, msg.Time);
    }
    
    public async Task SendTaskComment(string userId, string message, string taskId)
    {
        var parsedUserId = Guid.Parse(userId);
        var parsedTaskId = Guid.Parse(taskId);
        var comment = await _commentService.AddComment(parsedUserId, parsedTaskId, message);
        await Clients.All.SendAsync($"ReceiveTaskComment{taskId}", comment);
    }

    public async Task FetchDeleteProject(string projectId)
    {
        await Clients.All.SendAsync($"ReceiveDeleteProject", projectId);
    }

    public async Task FetchChangeProjectName(string projectId)
    {
        var parsedProjectId = Guid.Parse(projectId);
        var project = await _projectService.GetProjectById(parsedProjectId);
        if (project is null) return;
        await Clients.All.SendAsync("ReceiveChangeProjectName", projectId, project.Name);
    }

    public async Task SendAcceptInvite(string inviteId, string projectId, string userId)
    {
        var parsedInviteId = Guid.Parse(inviteId);
        var parsedProjectId = Guid.Parse(projectId);
        var parsedUserId = Guid.Parse(userId);
        var project = await _projectService.GetProjectById(parsedProjectId);
        if (project is null) return; 
        await _projectService.AddUserToProject(parsedProjectId, parsedUserId);
        await _invitationService.DeleteInvitation(parsedInviteId); 
        await Clients.All.SendAsync($"ReceiveAcceptInvite{userId}", project.Id, project.Name);
    }

    public async Task SendTaskAdd(string projectId)
    {
        await Clients.All.SendAsync($"ReceiveTaskAdd{projectId}", true);
    }
}