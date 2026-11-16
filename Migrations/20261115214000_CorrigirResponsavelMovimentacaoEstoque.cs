using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaCupCake.Migrations
{
    /// <inheritdoc />
    public partial class CorrigirResponsavelMovimentacaoEstoque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimentacoesEstoque_AspNetUsers_ResponsavelId",
                table: "MovimentacoesEstoque");

            migrationBuilder.DropIndex(
                name: "IX_MovimentacoesEstoque_ResponsavelId",
                table: "MovimentacoesEstoque");

            migrationBuilder.DropColumn(
                name: "ResponsavelId",
                table: "MovimentacoesEstoque");

            migrationBuilder.AlterColumn<string>(
                name: "UsuarioId",
                table: "MovimentacoesEstoque",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

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

            migrationBuilder.AlterColumn<string>(
                name: "UsuarioId",
                table: "MovimentacoesEstoque",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ResponsavelId",
                table: "MovimentacoesEstoque",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                "UPDATE `MovimentacoesEstoque` " +
                "SET `ResponsavelId` = `UsuarioId` " +
                "WHERE `UsuarioId` IS NOT NULL AND `UsuarioId` <> '';"
            );

            migrationBuilder.CreateIndex(
                name: "IX_MovimentacoesEstoque_ResponsavelId",
                table: "MovimentacoesEstoque",
                column: "ResponsavelId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimentacoesEstoque_AspNetUsers_ResponsavelId",
                table: "MovimentacoesEstoque",
                column: "ResponsavelId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
