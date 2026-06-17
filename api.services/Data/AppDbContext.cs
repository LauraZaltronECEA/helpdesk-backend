using api.models.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.services.Data;

// Application database context using EF Core with SQLite.
// Manages all entities: Users, Roles, Tickets, Areas, and permission mappings.
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

        // ── User entity configuration ──
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

            // Unique indexes on login credentials
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();

            // One-to-many: User -> UserRoles
            entity.HasMany(u => u.UserRoles)
                  .WithOne(ur => ur.User)
                  .HasForeignKey(ur => ur.Id_User)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Role entity configuration ──
        builder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");

            entity.Property(r => r.Role_Name);

            // One-to-many: Role -> UserRoles
            entity.HasMany(r => r.UserRoles)
                  .WithOne(ur => ur.Role)
                  .HasForeignKey(ur => ur.Id_Roles)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── UserRole join table configuration ──
        builder.Entity<UserRole>(entity =>
        {
            entity.ToTable("User_Roles");

            // Composite primary key
            entity.HasKey(ur => new { ur.Id_User, ur.Id_Roles });

            entity.Property(ur => ur.Id_User);
            entity.Property(ur => ur.Id_Roles);
        });

        // ── Area entity configuration ──
        builder.Entity<Area>(entity =>
        {
            entity.ToTable("Areas");
            entity.Property(a => a.Area_Name);
            entity.Property(a => a.Description);
        });

        // ── Ticket entity configuration ──
        builder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Tickets");

            // Column constraints
            entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Description).IsRequired().HasMaxLength(4000);
            entity.Property(t => t.Status).IsRequired().HasMaxLength(50).HasDefaultValue("open");
            entity.Property(t => t.Priority).IsRequired().HasMaxLength(50).HasDefaultValue("medium");
            entity.Property(t => t.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(t => t.IsDeleted).HasDefaultValue(0);

            // Performance indexes
            entity.HasIndex(t => t.CreatedById);
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.AssignedToId);
            entity.HasIndex(t => t.IsDeleted);

            // Relationship: Ticket -> CreatedBy (User)
            entity.HasOne(t => t.CreatedBy)
                  .WithMany()
                  .HasForeignKey(t => t.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);  // Prevent cascade-delete

            // Relationship: Ticket -> AssignedTo (User, nullable)
            entity.HasOne(t => t.AssignedTo)
                  .WithMany()
                  .HasForeignKey(t => t.AssignedToId)
                  .OnDelete(DeleteBehavior.SetNull);  // Set null when assigned user is deleted
        });

        // ── TicketFunction entity configuration ──
        builder.Entity<TicketFunction>(entity =>
        {
            entity.ToTable("Ticket_Functions");
            entity.Property(f => f.Function);
            entity.Property(f => f.Fun_Description);
        });

        // ── AccessToFuncPerRole join table configuration ──
        builder.Entity<AccessToFuncPerRole>(entity =>
        {
            entity.ToTable("Access_To_Func_Per_Role");

            // Composite primary key
            entity.HasKey(a => new { a.Id_Role, a.Id_Function });

            entity.Property(a => a.Id_Role);
            entity.Property(a => a.Id_Function);

            // Relationship: AccessToFuncPerRole -> Role
            entity.HasOne(a => a.Role)
                  .WithMany(r => r.AccessToFuncPerRoles)
                  .HasForeignKey(a => a.Id_Role)
                  .OnDelete(DeleteBehavior.Cascade);

            // Relationship: AccessToFuncPerRole -> TicketFunction
            entity.HasOne(a => a.Function)
                  .WithMany()
                  .HasForeignKey(a => a.Id_Function)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
