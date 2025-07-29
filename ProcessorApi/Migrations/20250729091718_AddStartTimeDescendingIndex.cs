using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessorApi.Migrations
{
    /// <inheritdoc />
    public partial class AddStartTimeDescendingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "INDX_RequestLogs_StartTime_Desc",
                table: "RequestLogs",
                column: "StartTime",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "INDX_RequestLogs_StartTime_Desc",
                table: "RequestLogs");
        }
    }
}
