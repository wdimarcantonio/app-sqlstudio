using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqlExcelBlazor.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialWorkflowSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QueryViews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SqlQuery = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectionString = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastExecuted = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryViews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Schedule = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QueryParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    QueryViewId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DataType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DefaultValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueryParameters_QueryViews_QueryViewId",
                        column: x => x.QueryViewId,
                        principalTable: "QueryViews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowExecutionResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkflowId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    TotalSteps = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedSteps = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowExecutionResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowExecutionResults_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkflowId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Configuration = table.Column<string>(type: "TEXT", nullable: false),
                    OnSuccess = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    OnError = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MaxRetries = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowSteps_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StepResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkflowExecutionResultId = table.Column<int>(type: "INTEGER", nullable: false),
                    StepOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    StepName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    RecordsProcessed = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordsFailed = table.Column<int>(type: "INTEGER", nullable: false),
                    LogDetails = table.Column<string>(type: "TEXT", nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StepResults_WorkflowExecutionResults_WorkflowExecutionResultId",
                        column: x => x.WorkflowExecutionResultId,
                        principalTable: "WorkflowExecutionResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QueryParameters_QueryViewId",
                table: "QueryParameters",
                column: "QueryViewId");

            migrationBuilder.CreateIndex(
                name: "IX_QueryViews_Name",
                table: "QueryViews",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_StepResults_WorkflowExecutionResultId",
                table: "StepResults",
                column: "WorkflowExecutionResultId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionResults_StartTime",
                table: "WorkflowExecutionResults",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionResults_WorkflowId",
                table: "WorkflowExecutionResults",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_IsActive",
                table: "Workflows",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_Name",
                table: "Workflows",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSteps_WorkflowId_Order",
                table: "WorkflowSteps",
                columns: new[] { "WorkflowId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueryParameters");

            migrationBuilder.DropTable(
                name: "StepResults");

            migrationBuilder.DropTable(
                name: "WorkflowSteps");

            migrationBuilder.DropTable(
                name: "QueryViews");

            migrationBuilder.DropTable(
                name: "WorkflowExecutionResults");

            migrationBuilder.DropTable(
                name: "Workflows");
        }
    }
}
