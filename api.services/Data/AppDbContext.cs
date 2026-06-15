using api.models.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.services.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketFunction> TicketFunctions => Set<TicketFunction>();
    public DbSet<AccessToFuncPerRole> AccessToFuncPerRoles => Set<AccessToFuncPerRole>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(u => u.Username).HasMaxLength(50);
            entity.Property(u => u.Fullname).HasMaxLength(50);
            entity.Property(u => u.Password).HasMaxLength(60);
            entity.Property(u => u.Email).HasMaxLength(50);
            entity.Property(u => u.Active).HasDefaultValue(1);
            entity.Property(u => u.EmailConfirmed).HasDefaultValue(0);
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("datetime('now')");

            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasMany(u => u.UserRoles)
                  .WithOne(ur => ur.User)
                  .HasForeignKey(ur => ur.Id_User)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");

            entity.Property(r => r.Role_Name);

            entity.HasMany(r => r.UserRoles)
                  .WithOne(ur => ur.Role)
                  .HasForeignKey(ur => ur.Id_Roles)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserRole>(entity =>
        {
            entity.ToTable("User_Roles");

            entity.HasKey(ur => new { ur.Id_User, ur.Id_Roles });

            entity.Property(ur => ur.Id_User);
            entity.Property(ur => ur.Id_Roles);
        });

        builder.Entity<Area>(entity =>
        {
            entity.ToTable("Areas");
            entity.Property(a => a.Area_Name);
            entity.Property(a => a.Description);
        });

        builder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Tickets");

            entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Description).IsRequired().HasMaxLength(4000);
            entity.Property(t => t.Status).IsRequired().HasMaxLength(50).HasDefaultValue("open");
            entity.Property(t => t.Priority).IsRequired().HasMaxLength(50).HasDefaultValue("medium");
            entity.Property(t => t.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(t => t.IsDeleted).HasDefaultValue(0);

            entity.HasIndex(t => t.CreatedById);
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.AssignedToId);
            entity.HasIndex(t => t.IsDeleted);

            entity.HasOne(t => t.CreatedBy)
                  .WithMany()
                  .HasForeignKey(t => t.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.AssignedTo)
                  .WithMany()
                  .HasForeignKey(t => t.AssignedToId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<TicketFunction>(entity =>
        {
            entity.ToTable("Ticket_Functions");
            entity.Property(f => f.Function);
            entity.Property(f => f.Fun_Description);
        });

        builder.Entity<AccessToFuncPerRole>(entity =>
        {
            entity.ToTable("Access_To_Func_Per_Role");

            entity.HasKey(a => new { a.Id_Role, a.Id_Function });

            entity.Property(a => a.Id_Role);
            entity.Property(a => a.Id_Function);

            entity.HasOne(a => a.Role)
                  .WithMany(r => r.AccessToFuncPerRoles)
                  .HasForeignKey(a => a.Id_Role)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Function)
                  .WithMany()
                  .HasForeignKey(a => a.Id_Function)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
