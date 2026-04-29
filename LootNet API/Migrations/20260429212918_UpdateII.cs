using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LootNet_API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateII : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketInventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketInventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketInventoryItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RunInventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunInventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunInventoryItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BattleIndex = table.Column<int>(type: "integer", nullable: false),
                    PlayerCurrentHp = table.Column<int>(type: "integer", nullable: false),
                    PlayerMaxHp = table.Column<int>(type: "integer", nullable: false),
                    IsPlayerDisorganized = table.Column<bool>(type: "boolean", nullable: false),
                    PlayerSkipNextTurn = table.Column<bool>(type: "boolean", nullable: false),
                    PlayerPosition = table.Column<int>(type: "integer", nullable: false),
                    LeftHandItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    RightHandItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StageProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StageIndex = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    Falloff = table.Column<double>(type: "double precision", nullable: false),
                    Threshold = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Battle",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Battle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Battle_Runs_RunId",
                        column: x => x.RunId,
                        principalTable: "Runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StageScenario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StageProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnemyCount = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageScenario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageScenario_StageProfiles_StageProfileId",
                        column: x => x.StageProfileId,
                        principalTable: "StageProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BattleEnemy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BattleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    CurrentHp = table.Column<int>(type: "integer", nullable: false),
                    MaxHp = table.Column<int>(type: "integer", nullable: false),
                    IsDisorganized = table.Column<bool>(type: "boolean", nullable: false),
                    SkipNextTurn = table.Column<bool>(type: "boolean", nullable: false),
                    LeftHandItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    RightHandItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleEnemy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BattleEnemy_Battle_BattleId",
                        column: x => x.BattleId,
                        principalTable: "Battle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BattleEnemy_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioSlot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    ClassProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    StageScenarioId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioSlot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenarioSlot_StageScenario_StageScenarioId",
                        column: x => x.StageScenarioId,
                        principalTable: "StageScenario",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Battle_RunId",
                table: "Battle",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_BattleEnemy_BattleId",
                table: "BattleEnemy",
                column: "BattleId");

            migrationBuilder.CreateIndex(
                name: "IX_BattleEnemy_EquipmentId",
                table: "BattleEnemy",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_UserId_ItemId",
                table: "InventoryItems",
                columns: new[] { "UserId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketInventoryItems_UserId_ItemId",
                table: "MarketInventoryItems",
                columns: new[] { "UserId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunInventoryItems_UserId_ItemId",
                table: "RunInventoryItems",
                columns: new[] { "UserId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioSlot_StageScenarioId",
                table: "ScenarioSlot",
                column: "StageScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_StageScenario_StageProfileId",
                table: "StageScenario",
                column: "StageProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BattleEnemy");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "MarketInventoryItems");

            migrationBuilder.DropTable(
                name: "RunInventoryItems");

            migrationBuilder.DropTable(
                name: "ScenarioSlot");

            migrationBuilder.DropTable(
                name: "Battle");

            migrationBuilder.DropTable(
                name: "StageScenario");

            migrationBuilder.DropTable(
                name: "Runs");

            migrationBuilder.DropTable(
                name: "StageProfiles");
        }
    }
}
