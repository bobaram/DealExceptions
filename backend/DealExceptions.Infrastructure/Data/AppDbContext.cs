using DealExceptions.Domain;
using DealExceptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DealExceptions.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DealException> DealExceptions => Set<DealException>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<StatusHistory> StatusHistories => Set<StatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DealException>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Priority).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.HasMany(x => x.Comments).WithOne(c => c.Exception).HasForeignKey(c => c.ExceptionId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.StatusHistories).WithOne(s => s.Exception).HasForeignKey(s => s.ExceptionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StatusHistory>(e =>
        {
            e.Property(x => x.FromStatus).HasConversion<string>();
            e.Property(x => x.ToStatus).HasConversion<string>();
        });
    }
}
