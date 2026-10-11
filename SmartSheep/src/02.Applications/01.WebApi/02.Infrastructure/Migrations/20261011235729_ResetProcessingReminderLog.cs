using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ResetProcessingReminderLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Status Processing tidak dipakai lagi; reminder yang tersangkut dikembalikan ke Pending agar dikirim ulang
            migrationBuilder.Sql(
                "UPDATE [ReminderLog] SET [Status] = 'Pending' WHERE [Status] = 'Processing'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
