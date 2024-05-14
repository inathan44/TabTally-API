// A safe way to return user information to the client without sensitive information (No email, user group information, etc.)

public class UserSummaryDTO
{
    public string Id { get; set; }
    public string Username { get; set; }

    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}