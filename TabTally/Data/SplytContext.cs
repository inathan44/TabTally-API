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

        // Set up optional relationship between Transaction and User
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.CreatedBy)
            .WithMany()
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Payer)
            .WithMany()
            .HasForeignKey(t => t.PayerId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);


        // Set up optional relationship between TransactionDetail and User
        modelBuilder.Entity<TransactionDetail>()
            .HasOne(td => td.Payer)
            .WithMany()
            .HasForeignKey(td => td.PayerId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        modelBuilder.Entity<TransactionDetail>()
            .HasOne(td => td.Recipient)
            .WithMany()
            .HasForeignKey(td => td.RecipientId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Set up cascading delete for GroupMember and Group
        modelBuilder.Entity<GroupMember>()
            .HasOne(gm => gm.InvitedBy)
            .WithMany()
            .HasForeignKey(gm => gm.InvitedById)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GroupMember>()
            .HasOne(gm => gm.Member)
            .WithMany()
            .HasForeignKey(gm => gm.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Group>()
            .HasOne(g => g.CreatedBy)
            .WithMany()
            .HasForeignKey(g => g.CreatedById)
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