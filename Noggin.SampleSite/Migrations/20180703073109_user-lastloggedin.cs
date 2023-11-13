using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Noggin.SampleSite.Migrations;

public partial class userlastloggedin : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "LastLoggedIn",
            table: "Users",
            nullable: false,
            defaultValue: new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "LastLoggedIn",
            table: "Users");
    }
}
