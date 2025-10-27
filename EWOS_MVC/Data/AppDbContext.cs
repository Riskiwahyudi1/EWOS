using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EWOS_MVC.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserModel> User { get; set; }
    public DbSet<MachineCategoriesModel> MachineCategories { get; set; }
    public DbSet<MachineModel> Machines { get; set; }

}
