using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class GroupMember
{

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Required]
    public int Id { get; set; }

    [Required]
    public int GroupId { get; set; }

    [ForeignKey("GroupId")]
    public virtual Group? Group { get; set; }


    [Required]
    public string MemberId { get; set; }

    [ForeignKey("MemberId")]
    public virtual User? Member { get; set; }

    [Required]
    public string InvitedById { get; set; }

    [ForeignKey("InvitedById")]
    public virtual User? InvitedBy { get; set; }

    public bool IsAdmin { get; set; }

    [Required]
    public GroupMemberStatus Status { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    public GroupMember()
    {
        Status = GroupMemberStatus.Invited;
    }
}