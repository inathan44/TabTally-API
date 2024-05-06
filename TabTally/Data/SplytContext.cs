using Microsoft.EntityFrameworkCore;

public class SplytContext : DbContext
{
    public SplytContext(DbContextOptions<SplytContext> options) : base(options)
    {
    }

    public DbSet<Transaction> Transaction { get; set; }
    public DbSet<User> User { get; set; }
    public DbSet<Group> Group { get; set; }
    public DbSet<GroupMember> GroupMember { get; set; }
    public DbSet<TransactionDetail> TransactionDetail { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql($"Host={Environment.GetEnvironmentVariable("DB_HOST")};Database={Environment.GetEnvironmentVariable("DB_NAME")};Username={Environment.GetEnvironmentVariable("DB_USER")};Password={Environment.GetEnvironmentVariable("DB_PASS")};Include Error Detail=true");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GroupMember>().Property(e => e.Status).HasConversion<string>();

        // Configure the relationship between User/Group/GroupMembers
        // Might need to change the cascade delete behavior for if a user deletes their account
        modelBuilder.Entity<GroupMember>()
            .HasOne(gm => gm.Member)
            .WithMany(u => u.GroupMembers)
            .HasForeignKey(gm => gm.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GroupMember>()
            .HasOne(gm => gm.InvitedBy)
            .WithMany()
            .HasForeignKey(gm => gm.InvitedById)
            .OnDelete(DeleteBehavior.Cascade);



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