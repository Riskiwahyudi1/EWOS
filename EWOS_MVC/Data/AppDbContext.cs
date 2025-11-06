using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EWOS_MVC.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserModel> Users { get; set; }
    public DbSet<MachineCategoriesModel> MachineCategories { get; set; }
    public DbSet<MachineModel> Machines { get; set; }
    public DbSet<RoleModel> Roles { get; set; }

    public DbSet<UserRoleModel> UserRoles { get; set; }

    //konfigurasi relasi  many to many
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRoleModel>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserRoleModel>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRoleModel>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);
    }

}
