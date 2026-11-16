using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaCupCake.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarResponsavelMovimentacaoEstoque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsuarioId",
                table: "MovimentacoesEstoque",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentacoesEstoque_UsuarioId",
                table: "MovimentacoesEstoque",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimentacoesEstoque_AspNetUsers_UsuarioId",
                table: "MovimentacoesEstoque",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimentacoesEstoque_AspNetUsers_UsuarioId",
                table: "MovimentacoesEstoque");

            migrationBuilder.DropIndex(
                name: "IX_MovimentacoesEstoque_UsuarioId",
                table: "MovimentacoesEstoque");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "MovimentacoesEstoque");
        }
    }
}
