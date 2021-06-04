using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class DonationAccounting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorDonations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AmountString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DonorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DonorLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remaining = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpenditureDesc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImportedRecord = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorDonations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GanjoorExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorExpenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DonationExpenditure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GanjoorDonationId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GanjoorExpenseId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonationExpenditure", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonationExpenditure_GanjoorDonations_GanjoorDonationId",
                        column: x => x.GanjoorDonationId,
                        principalTable: "GanjoorDonations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DonationExpenditure_GanjoorExpenses_GanjoorExpenseId",
                        column: x => x.GanjoorExpenseId,
                        principalTable: "GanjoorExpenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonationExpenditure_GanjoorDonationId",
                table: "DonationExpenditure",
                column: "GanjoorDonationId");

            migrationBuilder.CreateIndex(
                name: "IX_DonationExpenditure_GanjoorExpenseId",
                table: "DonationExpenditure",
                column: "GanjoorExpenseId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonationExpenditure");

            migrationBuilder.DropTable(
                name: "GanjoorDonations");

            migrationBuilder.DropTable(
                name: "GanjoorExpenses");
        }
    }
}
