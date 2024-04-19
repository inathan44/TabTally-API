using Microsoft.EntityFrameworkCore;


public static class SeedUsers
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "inathan44",
                Email = "user1@example.com",
                FirstName = "test",
                LastName = "user",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow

            },
            new User
            {
                Id = 2,
                Username = "johndoe",
                Email = "getAWP",
                FirstName = "John",
                LastName = "Doe",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        modelBuilder.Entity<Group>().HasData(
            new Group
            {
                Id = 1,
                Name = "Whistler Wankers",
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
   );
    }
}