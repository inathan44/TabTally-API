public class InvitedMemberDTO
{
    public string Id { get; set; }
    public string Role { get; set; }
}

public class AddMembersDTO
{
    public List<InvitedMemberDTO> InvitedMembers { get; set; }
}