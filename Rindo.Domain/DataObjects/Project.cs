using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rindo.Domain.DataObjects;

[Table("projects", Schema = "dbo")]
public class Project
{
    [Key]
    public Guid ProjectId { get; init;}
    public required string Name { get; set; } 
    public string? Description { get; set; }
    public Guid OwnerId { get; init; }
    public Guid ChatId { get; init; }
    // do we need these props in DO?
    public ICollection<Stage> Stages { get; set; } 
    public ICollection<User> Users { get; set; } 
    public ICollection<Role> Roles { get; set; } 
    public ICollection<Invitation> Invitations { get; set; } 
    public DateTimeOffset Created { get; set; }
    public Guid CreatedBy { get; init; }
    public DateTimeOffset Modified { get; init; }
    public Guid ModifiedBy { get; init; }
    public DateTimeOffset? DeadlineDate { get; init; }
}