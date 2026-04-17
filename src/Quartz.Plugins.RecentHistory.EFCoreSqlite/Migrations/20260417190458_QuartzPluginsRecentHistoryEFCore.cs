using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quartz.Plugins.RecentHistory.EFCoreSqlite.Migrations
{
    /// <inheritdoc />
    public partial class QuartzPluginsRecentHistoryEFCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_quartz_ExecutionHistoryStore",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fire_instance_id = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    scheduler_instance_id = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    scheduler_name = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    job_name = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    trigger_name = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    scheduled_fire_time_utc = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    actual_fire_time_utc = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    recovering = table.Column<int>(type: "INTEGER", nullable: false),
                    vetoed = table.Column<int>(type: "INTEGER", nullable: false),
                    finished_time_utc = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    exception_message = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_quartz_ExecutionHistoryStore", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tb_quartz_JobStats",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false),
                    total_jobs_executed = table.Column<int>(type: "INTEGER", nullable: false),
                    total_jobs_failed = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_quartz_JobStats", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_quartz_ExecutionHistoryStore_fire_instance_id",
                table: "tb_quartz_ExecutionHistoryStore",
                column: "fire_instance_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_quartz_ExecutionHistoryStore_scheduler_name_job_name",
                table: "tb_quartz_ExecutionHistoryStore",
                columns: new[] { "scheduler_name", "job_name" });

            migrationBuilder.CreateIndex(
                name: "IX_tb_quartz_ExecutionHistoryStore_scheduler_name_trigger_name",
                table: "tb_quartz_ExecutionHistoryStore",
                columns: new[] { "scheduler_name", "trigger_name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_quartz_ExecutionHistoryStore");

            migrationBuilder.DropTable(
                name: "tb_quartz_JobStats");
        }
    }
}
