using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Library.Data.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Event = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Author = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Isbn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedMessages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedMessages", x => x.MessageId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookCopies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookCopies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookCopies_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookCopyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_BookCopies_BookCopyId",
                        column: x => x.BookCopyId,
                        principalTable: "BookCopies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Reservations_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Books",
                columns: new[] { "Id", "Author", "Category", "Isbn", "Title" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222201"), "Phạm Hữu Khang", "Công nghệ thông tin", "9786040000001", "Lập trình C# căn bản" },
                    { new Guid("22222222-2222-2222-2222-222222222202"), "Nguyễn Văn An", "Công nghệ thông tin", "9786040000002", "Kiến trúc phần mềm phân tán" },
                    { new Guid("22222222-2222-2222-2222-222222222203"), "Trần Thị Bình", "Công nghệ thông tin", "9786040000003", "Cơ sở dữ liệu" },
                    { new Guid("22222222-2222-2222-2222-222222222204"), "Lê Văn Cường", "Công nghệ thông tin", "9786040000004", "Mạng máy tính" },
                    { new Guid("22222222-2222-2222-2222-222222222205"), "Đỗ Minh Dũng", "Công nghệ thông tin", "9786040000005", "Trí tuệ nhân tạo" },
                    { new Guid("22222222-2222-2222-2222-222222222206"), "Vũ Thị Em", "Toán học", "9786040000006", "Toán rời rạc" },
                    { new Guid("22222222-2222-2222-2222-222222222207"), "Hoàng Văn Phúc", "Khoa học tự nhiên", "9786040000007", "Vật lý đại cương" },
                    { new Guid("22222222-2222-2222-2222-222222222208"), "Ngô Thị Giang", "Kinh tế", "9786040000008", "Kinh tế vĩ mô" },
                    { new Guid("22222222-2222-2222-2222-222222222209"), "Bùi Văn Hải", "Khoa học xã hội", "9786040000009", "Lịch sử Việt Nam" },
                    { new Guid("22222222-2222-2222-2222-222222222210"), "Phan Thị Lan", "Ngoại ngữ", "9786040000010", "Tiếng Anh giao tiếp" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "an@example.com", "Nguyễn Văn An" },
                    { new Guid("11111111-1111-1111-1111-111111111102"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "binh@example.com", "Trần Thị Bình" },
                    { new Guid("11111111-1111-1111-1111-111111111103"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cuong@example.com", "Lê Văn Cường" }
                });

            migrationBuilder.InsertData(
                table: "BookCopies",
                columns: new[] { "Id", "BookId", "Status" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333301"), new Guid("22222222-2222-2222-2222-222222222201"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333302"), new Guid("22222222-2222-2222-2222-222222222201"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333303"), new Guid("22222222-2222-2222-2222-222222222201"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333304"), new Guid("22222222-2222-2222-2222-222222222202"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333305"), new Guid("22222222-2222-2222-2222-222222222202"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333306"), new Guid("22222222-2222-2222-2222-222222222203"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333307"), new Guid("22222222-2222-2222-2222-222222222203"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333308"), new Guid("22222222-2222-2222-2222-222222222203"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333309"), new Guid("22222222-2222-2222-2222-222222222204"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333310"), new Guid("22222222-2222-2222-2222-222222222205"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333311"), new Guid("22222222-2222-2222-2222-222222222205"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333312"), new Guid("22222222-2222-2222-2222-222222222206"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333313"), new Guid("22222222-2222-2222-2222-222222222206"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333314"), new Guid("22222222-2222-2222-2222-222222222207"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333315"), new Guid("22222222-2222-2222-2222-222222222208"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333316"), new Guid("22222222-2222-2222-2222-222222222208"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333317"), new Guid("22222222-2222-2222-2222-222222222209"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333318"), new Guid("22222222-2222-2222-2222-222222222210"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333319"), new Guid("22222222-2222-2222-2222-222222222210"), "Available" },
                    { new Guid("33333333-3333-3333-3333-333333333320"), new Guid("22222222-2222-2222-2222-222222222210"), "Available" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ReservationId",
                table: "AuditLogs",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_BookCopies_BookId_Status",
                table: "BookCopies",
                columns: new[] { "BookId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Books_Title",
                table: "Books",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt",
                table: "OutboxMessages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BookCopyId",
                table: "Reservations",
                column: "BookCopyId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BookId",
                table: "Reservations",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Status",
                table: "Reservations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_UserId",
                table: "Reservations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "ProcessedMessages");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "BookCopies");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Books");
        }
    }
}
