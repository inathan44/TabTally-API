using Microsoft.EntityFrameworkCore;

public class SplytContext : DbContext
{
    public SplytContext(DbContextOptions<SplytContext> options) : base(options)
    {
    }

    public DbSet<Transaction> Transaction { get; set; }
    public DbSet<User> User { get; set; }
    public DbSet<Group> Group { get; set; }
    public DbSet<GroupMembers> GroupMembers { get; set; }
    public DbSet<TransactionDetails> TransactionDetails { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql($"Host={Environment.GetEnvironmentVariable("DB_HOST")};Database={Environment.GetEnvironmentVariable("DB_NAME")};Username={Environment.GetEnvironmentVariable("DB_USER")};Password={Environment.GetEnvironmentVariable("DB_PASS")};");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GroupMembers>().Property(e => e.Status).HasConversion<string>();

        // Configure the relationship between User/Group/GroupMembers
        modelBuilder.Entity<GroupMembers>()
            .HasOne(gm => gm.Member)
            .WithMany(u => u.GroupMembers)
            .HasForeignKey(gm => gm.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GroupMembers>()
            .HasOne(gm => gm.InvitedBy)
            .WithMany()
            .HasForeignKey(gm => gm.InvitedById)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public bool TestConnection()
    {
        try
        {
            this.Database.OpenConnection();
            this.Database.CloseConnection();
            return true;
        }
        catch
        {
            return false;
        }
    }

}