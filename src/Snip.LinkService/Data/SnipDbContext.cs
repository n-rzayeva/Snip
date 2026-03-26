using Microsoft.EntityFrameworkCore;
using Snip.Shared.Models;

namespace Snip.LinkService.Data;

public class SnipDbContext : DbContext
{
    public SnipDbContext(DbContextOptions<SnipDbContext> options) : base(options) { }

    public DbSet<Link> Links => Set<Link>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Link>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.Slug).HasMaxLength(20).IsRequired();
            entity.Property(e => e.DestinationUrl).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
        });
    }
}