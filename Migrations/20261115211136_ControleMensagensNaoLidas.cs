using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaCupCake.Migrations
{
    /// <inheritdoc />
    public partial class ControleMensagensNaoLidas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LidaPelaLojaEm",
                table: "ChatMensagens",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LidaPeloClienteEm",
                table: "ChatMensagens",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LidaPelaLojaEm",
                table: "ChatMensagens");

            migrationBuilder.DropColumn(
                name: "LidaPeloClienteEm",
                table: "ChatMensagens");
        }
    }
}
