public class GroupSummaryDTO
{
    public int Id { get; set; }
    public string Name { get; set; }

    public string? Description { get; set; }
    public string CreatedById { get; set; }

    public UserSummaryDTO CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<GroupMemberSummaryDTO>? GroupMembers { get; set; }

    public ICollection<TransactionSummaryDTO>? Transactions { get; set; }

}