using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Snip.LinkService.Data;

public class SnipDbContextFactory : IDesignTimeDbContextFactory<SnipDbContext>
{
    public SnipDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SnipDbContext>();
        optionsBuilder.UseNpgsql("Host=127.0.0.1;Port=5433;Database=snip;Username=snip;Password=snip");
        return new SnipDbContext(optionsBuilder.Options);
    }
}