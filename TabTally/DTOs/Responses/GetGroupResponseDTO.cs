public class GetGroupResponseDTO
{
    public int Id { get; set; }
    public string Name { get; set; }

    public string? Description { get; set; }
    public string CreatedById { get; set; }

    public UserSummaryDTO CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<GetGroupGroupMemberDTO> GroupMembers { get; set; }

    public ICollection<TransactionSummaryDTO> Transactions { get; set; }

}

public class GetGroupGroupMemberDTO
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string MemberId { get; set; }
    public UserSummaryDTO Member { get; set; }
    public string InvitedById { get; set; }
    public bool IsAdmin { get; set; }
    public GroupMemberStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

}

