using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqlExcelBlazor.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialConnectionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Connections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastTested = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: true),
                    HasHeaders = table.Column<bool>(type: "INTEGER", nullable: true),
                    Delimiter = table.Column<string>(type: "TEXT", nullable: true),
                    Encoding = table.Column<string>(type: "TEXT", nullable: true),
                    ExcelConnection_FilePath = table.Column<string>(type: "TEXT", nullable: true),
                    ExcelConnection_HasHeaders = table.Column<bool>(type: "INTEGER", nullable: true),
                    WorksheetName = table.Column<string>(type: "TEXT", nullable: true),
                    Server = table.Column<string>(type: "TEXT", nullable: true),
                    Port = table.Column<int>(type: "INTEGER", nullable: true),
                    Database = table.Column<string>(type: "TEXT", nullable: true),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    Password = table.Column<string>(type: "TEXT", nullable: true),
                    ConnectionTimeout = table.Column<int>(type: "INTEGER", nullable: true),
                    Host = table.Column<string>(type: "TEXT", nullable: true),
                    PostgreSqlConnection_Port = table.Column<int>(type: "INTEGER", nullable: true),
                    PostgreSqlConnection_Database = table.Column<string>(type: "TEXT", nullable: true),
                    PostgreSqlConnection_Username = table.Column<string>(type: "TEXT", nullable: true),
                    PostgreSqlConnection_Password = table.Column<string>(type: "TEXT", nullable: true),
                    PostgreSqlConnection_ConnectionTimeout = table.Column<int>(type: "INTEGER", nullable: true),
                    SqlServerConnection_Server = table.Column<string>(type: "TEXT", nullable: true),
                    SqlServerConnection_Port = table.Column<int>(type: "INTEGER", nullable: true),
                    SqlServerConnection_Database = table.Column<string>(type: "TEXT", nullable: true),
                    IntegratedSecurity = table.Column<bool>(type: "INTEGER", nullable: true),
                    SqlServerConnection_Username = table.Column<string>(type: "TEXT", nullable: true),
                    SqlServerConnection_Password = table.Column<string>(type: "TEXT", nullable: true),
                    TrustServerCertificate = table.Column<bool>(type: "INTEGER", nullable: true),
                    SqlServerConnection_ConnectionTimeout = table.Column<int>(type: "INTEGER", nullable: true),
                    BaseUrl = table.Column<string>(type: "TEXT", nullable: true),
                    AuthType = table.Column<int>(type: "INTEGER", nullable: true),
                    AuthConfigJson = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultHeadersJson = table.Column<string>(type: "TEXT", nullable: true),
                    TimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Connections_IsActive",
                table: "Connections",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_Name",
                table: "Connections",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Connections_Type",
                table: "Connections",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Connections");
        }
    }
}
