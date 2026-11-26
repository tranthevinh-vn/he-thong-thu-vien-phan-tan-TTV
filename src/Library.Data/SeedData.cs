using Library.Contracts.Enums;
using Library.Data.Entities;

namespace Library.Data;

public static class SeedData
{
    public static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static readonly User[] Users =
    {
        new() { Id = new Guid("11111111-1111-1111-1111-111111111101"), Name = "Nguyễn Văn An", Email = "an@example.com", CreatedAt = SeedTimestamp },
        new() { Id = new Guid("11111111-1111-1111-1111-111111111102"), Name = "Trần Thị Bình", Email = "binh@example.com", CreatedAt = SeedTimestamp },
        new() { Id = new Guid("11111111-1111-1111-1111-111111111103"), Name = "Lê Văn Cường", Email = "cuong@example.com", CreatedAt = SeedTimestamp }
    };

    public static readonly Book[] Books =
    {
        new() { Id = new Guid("22222222-2222-2222-2222-222222222201"), Title = "Lập trình C# căn bản", Author = "Phạm Hữu Khang", Isbn = "9786040000001", Category = "Công nghệ thông tin" },
        new() { Id = new Guid("22222222-2222-2222-2222-222222222202"), Title = "Kiến trúc phần mềm phân tán", Author = "Nguyễn Văn An", Isbn = "9786040000002", Category = "Công nghệ thông tin" },
        new() { Id = new Guid("22222222-2222-2222-2222-222222222203"), Title = "Cơ sở dữ liệu", Author = "Trần Thị Bình", Isbn = "9786040000003", Category = "Công nghệ thông tin" },
        new() { Id = new Guid("22222222-2222-2222-2222-222222222204"), Title = "Mạng máy tính", Author = "Lê Văn Cường", Isbn = "9786040000004", Category = "Công nghệ thông tin" },
        new() { Id = new Guid("22222222-2222-2222-2222-222222222205"), Title = "Trí tuệ nhân tạo", Author = "Đỗ Minh Dũng", Isbn = "9786040000005", Category = "Công nghệ thông tin" },
        new() { Id = new Guid("22222222-2222-2222-2222-222222222206"), Title = "Toán rời rạc", Author = "Vũ Thị Em", Isbn = "9786040000006", Category = "Toán học" },
        new() { Id = new Guid("22222222-2222-2222-2222-222222222207"), Title = "Vật lý đại cương", Author = "Hoàng Văn Phúc", Isbn = "9786040000007", Category = "Khoa học tự nhiên" },
        new() { Id = new Guid("22222222-2222-2222-2222-222222222208"), Title = "Kinh tế vĩ mô", Author = "Ngô Thị Giang", Isbn = "9786040000008", Category = "Kinh tế" },
        new() { Id = new Guid("22222222-2222-2222-2222-222222222209"), Title = "Lịch sử Việt Nam", Author = "Bùi Văn Hải", Isbn = "9786040000009", Category = "Khoa học xã hội" },
        new() { Id = new Guid("22222222-2222-2222-2222-222222222210"), Title = "Tiếng Anh giao tiếp", Author = "Phan Thị Lan", Isbn = "9786040000010", Category = "Ngoại ngữ" }
    };

    public static readonly BookCopy[] BookCopies = BuildCopies();

    private static BookCopy[] BuildCopies()
    {
        var copies = new List<BookCopy>();
        var index = 1;
        foreach (var book in Books)
        {
            var count = book.Id.ToString().EndsWith("01") || book.Id.ToString().EndsWith("03") || book.Id.ToString().EndsWith("10")
                ? 3
                : book.Id.ToString().EndsWith("04") || book.Id.ToString().EndsWith("07") || book.Id.ToString().EndsWith("09")
                    ? 1
                    : 2;

            for (var i = 0; i < count; i++)
            {
                copies.Add(new BookCopy
                {
                    Id = new Guid($"33333333-3333-3333-3333-3333333333{index:00}"),
                    BookId = book.Id,
                    Status = BookCopyStatus.Available
                });
                index++;
            }
        }

        return copies.ToArray();
    }
}
