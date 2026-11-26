using Library.Contracts.Enums;
using Library.Data;

namespace Library.Tests;

public class SeedDataTests
{
    [Fact]
    public void Seed_HasExpectedCounts()
    {
        Assert.Equal(3, SeedData.Users.Length);
        Assert.Equal(10, SeedData.Books.Length);
        Assert.Equal(20, SeedData.BookCopies.Length);
    }

    [Fact]
    public void EveryCopyReferencesExistingBook()
    {
        var bookIds = SeedData.Books.Select(b => b.Id).ToHashSet();

        Assert.All(SeedData.BookCopies, copy => Assert.Contains(copy.BookId, bookIds));
    }

    [Fact]
    public void CopyIdsAreUnique()
    {
        var distinct = SeedData.BookCopies.Select(c => c.Id).Distinct().Count();

        Assert.Equal(SeedData.BookCopies.Length, distinct);
    }

    [Fact]
    public void AllCopiesStartAvailable()
    {
        Assert.All(SeedData.BookCopies, copy => Assert.Equal(BookCopyStatus.Available, copy.Status));
    }
}
