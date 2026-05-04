using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LootNet_API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIII : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Battle_Runs_RunId",
                table: "Battle");

            migrationBuilder.DropForeignKey(
                name: "FK_BattleEnemy_Battle_BattleId",
                table: "BattleEnemy");

            migrationBuilder.DropForeignKey(
                name: "FK_BattleEnemy_Equipments_EquipmentId",
                table: "BattleEnemy");

            migrationBuilder.DropForeignKey(
                name: "FK_ScenarioSlot_StageScenario_StageScenarioId",
                table: "ScenarioSlot");

            migrationBuilder.DropForeignKey(
                name: "FK_StageScenario_StageProfiles_StageProfileId",
                table: "StageScenario");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StageScenario",
                table: "StageScenario");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScenarioSlot",
                table: "ScenarioSlot");

            migrationBuilder.DropIndex(
                name: "IX_ScenarioSlot_StageScenarioId",
                table: "ScenarioSlot");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BattleEnemy",
                table: "BattleEnemy");

            migrationBuilder.DropIndex(
                name: "IX_BattleEnemy_EquipmentId",
                table: "BattleEnemy");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Battle",
                table: "Battle");

            migrationBuilder.DropColumn(
                name: "StageScenarioId",
                table: "ScenarioSlot");

            migrationBuilder.DropColumn(
                name: "EquipmentId",
                table: "BattleEnemy");

            migrationBuilder.RenameTable(
                name: "StageScenario",
                newName: "StageScenarios");

            migrationBuilder.RenameTable(
                name: "ScenarioSlot",
                newName: "ScenarioSlots");

            migrationBuilder.RenameTable(
                name: "BattleEnemy",
                newName: "BattleEnemies");

            migrationBuilder.RenameTable(
                name: "Battle",
                newName: "Battles");

            migrationBuilder.RenameIndex(
                name: "IX_StageScenario_StageProfileId",
                table: "StageScenarios",
                newName: "IX_StageScenarios_StageProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_BattleEnemy_BattleId",
                table: "BattleEnemies",
                newName: "IX_BattleEnemies_BattleId");

            migrationBuilder.RenameIndex(
                name: "IX_Battle_RunId",
                table: "Battles",
                newName: "IX_Battles_RunId");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Equipments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "BattleEnemyId",
                table: "Equipments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Class",
                table: "BattleEnemies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_StageScenarios",
                table: "StageScenarios",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScenarioSlots",
                table: "ScenarioSlots",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BattleEnemies",
                table: "BattleEnemies",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Battles",
                table: "Battles",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "EnemyClassProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Class = table.Column<int>(type: "integer", nullable: false),
                    AllowedColumns = table.Column<List<int>>(type: "integer[]", nullable: false),
                    GenerationProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnemyClassProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_BattleEnemyId",
                table: "Equipments",
                column: "BattleEnemyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioSlots_ScenarioId",
                table: "ScenarioSlots",
                column: "ScenarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_BattleEnemies_Battles_BattleId",
                table: "BattleEnemies",
                column: "BattleId",
                principalTable: "Battles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Battles_Runs_RunId",
                table: "Battles",
                column: "RunId",
                principalTable: "Runs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_BattleEnemies_BattleEnemyId",
                table: "Equipments",
                column: "BattleEnemyId",
                principalTable: "BattleEnemies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ScenarioSlots_StageScenarios_ScenarioId",
                table: "ScenarioSlots",
                column: "ScenarioId",
                principalTable: "StageScenarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StageScenarios_StageProfiles_StageProfileId",
                table: "StageScenarios",
                column: "StageProfileId",
                principalTable: "StageProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BattleEnemies_Battles_BattleId",
                table: "BattleEnemies");

            migrationBuilder.DropForeignKey(
                name: "FK_Battles_Runs_RunId",
                table: "Battles");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_BattleEnemies_BattleEnemyId",
                table: "Equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_ScenarioSlots_StageScenarios_ScenarioId",
                table: "ScenarioSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_StageScenarios_StageProfiles_StageProfileId",
                table: "StageScenarios");

            migrationBuilder.DropTable(
                name: "EnemyClassProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_BattleEnemyId",
                table: "Equipments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StageScenarios",
                table: "StageScenarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScenarioSlots",
                table: "ScenarioSlots");

            migrationBuilder.DropIndex(
                name: "IX_ScenarioSlots_ScenarioId",
                table: "ScenarioSlots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Battles",
                table: "Battles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BattleEnemies",
                table: "BattleEnemies");

            migrationBuilder.DropColumn(
                name: "BattleEnemyId",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "Class",
                table: "BattleEnemies");

            migrationBuilder.RenameTable(
                name: "StageScenarios",
                newName: "StageScenario");

            migrationBuilder.RenameTable(
                name: "ScenarioSlots",
                newName: "ScenarioSlot");

            migrationBuilder.RenameTable(
                name: "Battles",
                newName: "Battle");

            migrationBuilder.RenameTable(
                name: "BattleEnemies",
                newName: "BattleEnemy");

            migrationBuilder.RenameIndex(
                name: "IX_StageScenarios_StageProfileId",
                table: "StageScenario",
                newName: "IX_StageScenario_StageProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Battles_RunId",
                table: "Battle",
                newName: "IX_Battle_RunId");

            migrationBuilder.RenameIndex(
                name: "IX_BattleEnemies_BattleId",
                table: "BattleEnemy",
                newName: "IX_BattleEnemy_BattleId");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Equipments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StageScenarioId",
                table: "ScenarioSlot",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EquipmentId",
                table: "BattleEnemy",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_StageScenario",
                table: "StageScenario",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScenarioSlot",
                table: "ScenarioSlot",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Battle",
                table: "Battle",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BattleEnemy",
                table: "BattleEnemy",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioSlot_StageScenarioId",
                table: "ScenarioSlot",
                column: "StageScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_BattleEnemy_EquipmentId",
                table: "BattleEnemy",
                column: "EquipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Battle_Runs_RunId",
                table: "Battle",
                column: "RunId",
                principalTable: "Runs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BattleEnemy_Battle_BattleId",
                table: "BattleEnemy",
                column: "BattleId",
                principalTable: "Battle",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BattleEnemy_Equipments_EquipmentId",
                table: "BattleEnemy",
                column: "EquipmentId",
                principalTable: "Equipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ScenarioSlot_StageScenario_StageScenarioId",
                table: "ScenarioSlot",
                column: "StageScenarioId",
                principalTable: "StageScenario",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StageScenario_StageProfiles_StageProfileId",
                table: "StageScenario",
                column: "StageProfileId",
                principalTable: "StageProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
