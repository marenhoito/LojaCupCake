using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaCupCake.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarFuncionamentoFilial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FuncionamentosFiliais",
                columns: table => new
                {
                    FuncionamentoFilialId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    DiaSemana = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    HoraAbertura = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    HoraFechamento = table.Column<TimeSpan>(type: "time(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuncionamentosFiliais", x => x.FuncionamentoFilialId);
                    table.ForeignKey(
                        name: "FK_FuncionamentosFiliais_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "FilialId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_FuncionamentosFiliais_FilialId_DiaSemana_HoraAbertura",
                table: "FuncionamentosFiliais",
                columns: new[] { "FilialId", "DiaSemana", "HoraAbertura" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FuncionamentosFiliais");
        }
    }
}
