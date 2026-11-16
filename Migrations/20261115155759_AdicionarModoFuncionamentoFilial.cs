using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaCupCake.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarModoFuncionamentoFilial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PodeControlarFuncionamento",
                table: "FuncionariosFiliais",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte>(
                name: "ModoFuncionamento",
                table: "Filiais",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PodeControlarFuncionamento",
                table: "FuncionariosFiliais");

            migrationBuilder.DropColumn(
                name: "ModoFuncionamento",
                table: "Filiais");
        }
    }
}
