using Microsoft.EntityFrameworkCore;
using Snip.Shared.Models;

namespace Snip.RedirectService.Data;

public class RedirectDbContext : DbContext
{
    public RedirectDbContext(DbContextOptions<RedirectDbContext> options) : base(options) { }

    public DbSet<Link> Links => Set<Link>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Link>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Slug).HasMaxLength(20).IsRequired();
            entity.Property(e => e.DestinationUrl).HasMaxLength(2048).IsRequired();
        });
    }
}