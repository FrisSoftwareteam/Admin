using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FirstReg.Data;

public class AppDB : IdentityDbContext<User, Role, int>
{
    public AppDB(DbContextOptions<AppDB> options) : base(options) { }

    public virtual DbSet<AccessRole> AccessRoles { get; set; }
    public virtual DbSet<AnnualReport> AnnualReports { get; set; }
    public virtual DbSet<Author> Authors { get; set; }
    public virtual DbSet<Contact> Contacts { get; set; }
    public virtual DbSet<Dividend> Dividends { get; set; }
    public virtual DbSet<ECertRequest> ECertRequests { get; set; }
    public virtual DbSet<ECertHolder> ECertHolders { get; set; }
    public virtual DbSet<ECertHolding> ECertHoldings { get; set; }
    public virtual DbSet<Faq> Faqs { get; set; }
    public virtual DbSet<FaqSection> FaqSections { get; set; }
    public virtual DbSet<LastId> LastIds { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }
    public virtual DbSet<Post> Posts { get; set; }
    public virtual DbSet<PostCategory> PostCategories { get; set; }
    public virtual DbSet<Register> Registers { get; set; }
    public virtual DbSet<RegisterHolding> RegisterHoldings { get; set; }
    public virtual DbSet<Shareholder> Shareholders { get; set; }
    public virtual DbSet<ShareHolding> ShareHoldings { get; set; }
    public virtual DbSet<StockBroker> StockBrokers { get; set; }
    public virtual DbSet<Ticket> Tickets { get; set; }
    public virtual DbSet<Message> Messages { get; set; }
    public virtual DbSet<Subscription> Subscriptions { get; set; }
    public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public virtual DbSet<FormResponse> FormResponses { get; set; }
    public virtual DbSet<ShareOffer> ShareOffers { get; set; }
    public virtual DbSet<ShareSubscription> ShareSubscriptions { get; set; }
    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<RegisterSHSumm> ProductViews { get; set; }
    public virtual DbSet<RegisterView> RegisterViews { get; set; }
    public virtual DbSet<ShareholderView> ShareholderViews { get; set; }
    public virtual DbSet<ShareholdingView> ShareholdingViews { get; set; }
    public virtual DbSet<RegisterIdModel> RegisterIdModels { get; set; }
    public virtual DbSet<AuditLogView> AuditLogView { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RegisterSHSumm>().ToView("vw_RegisterSHSumm").HasNoKey();
        modelBuilder.Entity<RegisterView>().ToView("vw_Registers").HasNoKey();
        modelBuilder.Entity<ShareholderView>().ToView("vw_Shareholders").HasNoKey();
        modelBuilder.Entity<ShareholdingView>().ToView("vw_Shareholdings").HasNoKey();
        modelBuilder.Entity<RegisterIdModel>().ToView("Registers").HasNoKey();
        modelBuilder.Entity<AuditLogView>().ToView("AuditLog").HasNoKey();

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.UserName).IsRequired().IsUnicode(false);
            entity.Property(e => e.NormalizedUserName).IsRequired().IsUnicode(false);
            entity.Property(e => e.Email).IsRequired().IsUnicode(false);
            entity.Property(e => e.NormalizedEmail).IsRequired().IsUnicode(false);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(256).IsUnicode(false);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(250).IsUnicode(false).HasDefaultValueSql("('')");

            entity.HasData(new User
            {
                Id = 1,
                Type = UserType.SystemAdmin,
                FullName = "Host Account",
                UserName = "host",
                NormalizedUserName = "HOST",
                Email = "support@clearwox.com",
                NormalizedEmail = "SUPPORT@CLEARWOX.COM",
                AccessFailedCount = 0,
                EmailConfirmed = true,
                LockoutEnabled = true,
                PhoneNumberConfirmed = true,
                TwoFactorEnabled = false,
                PasswordHash = "AQAAAAEAACcQAAAAECQvPNb43OCmnOTgI+0vIUlYKHiaeb86CAkOiC6cqgnw63KGA0akaDtIMS8AeZ/UEg==",
                SecurityStamp = "UNGTKDF5EN5BRYOMVKQ5DQ6ZGRTZLHYK",
                ConcurrencyStamp = "01577897-c74f-4339-8eus-5d7b9b15f684",
                PhoneNumber = "+234 1 279 9880"
            });

            entity.HasData(new User
            {
                Id = 2,
                Type = UserType.SystemAdmin,
                FullName = "Super Admin",
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                Email = "admin@firstreginstrars.com",
                NormalizedEmail = "ADMIN@FIRSTREGINSTRARS.COM",
                AccessFailedCount = 0,
                EmailConfirmed = true,
                LockoutEnabled = true,
                PhoneNumberConfirmed = true,
                TwoFactorEnabled = false,
                PasswordHash = "AQAAAAEAACcQAAAAECQvPNb43OCmnOTgI+0vIUlYKHiaeb86CAkOiC6cqgnw63KGA0akaDtIMS8AeZ/UEg==",
                SecurityStamp = "UNGTKDF5EN5BRYOMVKQ5DQ6ZGRTZLHYK",
                ConcurrencyStamp = "01577897-c74f-4339-b368-5d7b9b15f684",
                PhoneNumber = "+234 1 279 9880"
            });
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasData(new Role { Id = 1, Name = "admin", NormalizedName = "ADMIN", ConcurrencyStamp = "1bfa7a25-b1a2-42f0-bccf-a4a87f5e82is" });
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasData(new UserRole { UserId = 1, RoleId = 1 });
            entity.HasData(new UserRole { UserId = 2, RoleId = 1 });
        });

        modelBuilder.Entity<AccessRole>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.Role });
        });

        modelBuilder.Entity<Dividend>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<RegisterHolding>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<ECertRequest>(entity =>
        {
            entity.Property(e => e.Date).HasDefaultValueSql("GETDATE()");
            entity.HasOne(d => d.StockBroker).WithMany(p => p.ECertRequests).HasForeignKey(d => d.StockBrokerId)
                  .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ECertHolder>(entity =>
        {
            entity.Property(e => e.Date).HasDefaultValueSql("GETDATE()");
            entity.HasOne(d => d.ECertRequest).WithMany(p => p.ECertHolders).HasForeignKey(d => d.RequestId)
                  .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ECertHolding>(entity =>
        {
            entity.Property(e => e.Date).HasDefaultValueSql("GETDATE()");
            entity.HasOne(d => d.ECertHolder).WithMany(p => p.ECertHoldings).HasForeignKey(d => d.HolderId)
                  .OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.Register).WithMany(p => p.ECertHoldings).HasForeignKey(d => d.RegisterId)
                  .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Register>(entity =>
        {
            //entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
            //entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");
            entity.HasOne(d => d.User).WithOne(p => p.Register).HasForeignKey<Register>(d => d.UserId)
                  .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Shareholder>(entity =>
        {
            entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<StockBroker>(entity =>
        {
            entity.Property(e => e.Date).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
            entity.HasOne(d => d.User).WithOne(p => p.StockBroker).HasForeignKey<StockBroker>(d => d.Id)
                  .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasOne(d => d.Ticket).WithMany(p => p.Messages).HasForeignKey(d => d.TicketId)
                  .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<FormResponse>(entity =>
        {
            entity.HasIndex(d => d.UniqueKey);
        });

        modelBuilder.Entity<ShareOffer>(entity =>
        {
            entity.HasIndex(d => d.UniqueKey);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasOne(o => o.User).WithMany(c => c.Payments).HasForeignKey(o => o.UserId).IsRequired(false);
        });
    }
}