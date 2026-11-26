using Library.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Author).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Isbn).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Category).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Title);

        builder.HasData(SeedData.Books);
    }
}
