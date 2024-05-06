public class GetUserResponseDTO
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<GetUserGroupMemberDTO> GroupMembers { get; set; }
}
public class GetUserGroupMemberDTO
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string MemberId { get; set; }

    public string InvitedById { get; set; }
    public bool IsAdmin { get; set; }

    public GroupMemberStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public GetUserGroupDTO? Group { get; set; }
}

public class GetUserGroupDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<GetUserGroupMemberDTO> GroupMembers { get; set; }
}