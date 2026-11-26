using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Library.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LibraryDbContext>
{
    public LibraryDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=library_db;Username=library;Password=library_pass";

        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new LibraryDbContext(options);
    }
}
