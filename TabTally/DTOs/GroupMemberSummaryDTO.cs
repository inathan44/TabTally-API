public class GroupMemberSummaryDTO
{
    public int Id { get; set; }
    public string MemberId { get; set; }
    public UserSummaryDTO Member { get; set; }
    public string InvitedById { get; set; }
    public UserSummaryDTO InvitedBy { get; set; }

    public bool IsAdmin { get; set; }

    public GroupMemberStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}