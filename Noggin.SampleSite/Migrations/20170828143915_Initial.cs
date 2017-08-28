using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Noggin.SampleSite.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAuthAccount",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 64, nullable: false),
                    Provider = table.Column<string>(maxLength: 32, nullable: true),
                    UserId = table.Column<int>(nullable: true),
                    UserName = table.Column<string>(maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuthAccount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAuthAccount_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthAccount_UserId",
                table: "UserAuthAccount",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAuthAccount");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
