using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Library.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedMessageConsumer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProcessedMessages",
                table: "ProcessedMessages");

            migrationBuilder.AddColumn<string>(
                name: "Consumer",
                table: "ProcessedMessages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProcessedMessages",
                table: "ProcessedMessages",
                columns: new[] { "Consumer", "MessageId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProcessedMessages",
                table: "ProcessedMessages");

            migrationBuilder.DropColumn(
                name: "Consumer",
                table: "ProcessedMessages");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProcessedMessages",
                table: "ProcessedMessages",
                column: "MessageId");
        }
    }
}
