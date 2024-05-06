public class GetUserGroupsMemberDTO
{
    public string Id { get; set; }
    public string Username { get; set; }

    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GetUserGroupsGroupMemberDTO
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string MemberId { get; set; }

    public string InvitedById { get; set; }
    public bool IsAdmin { get; set; }

    public GroupMemberStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GetUserGroupsResponse
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }

    public string CreatedById { get; set; }

    public GetUserGroupsMemberDTO CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<GetUserGroupsGroupMemberDTO> GroupMembers { get; set; }
}