using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LootNet_API.Migrations
{
    /// <inheritdoc />
    public partial class Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    TargetUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Data = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Armors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArmorType = table.Column<int>(type: "integer", nullable: false),
                    CutResistance = table.Column<double>(type: "double precision", nullable: false),
                    BluntResistance = table.Column<double>(type: "double precision", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Armors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Text = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "GenerationProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerationProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketListings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSold = table.Column<bool>(type: "boolean", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketListings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
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
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<int>(type: "integer", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    SellerPayout = table.Column<decimal>(type: "numeric", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Weapons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WeaponType = table.Column<int>(type: "integer", nullable: false),
                    Cut = table.Column<double>(type: "double precision", nullable: false),
                    Blunt = table.Column<double>(type: "double precision", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Weapons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemGenerationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    WeaponType = table.Column<int>(type: "integer", nullable: true),
                    ArmorType = table.Column<int>(type: "integer", nullable: true),
                    IsFallback = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemGenerationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemGenerationRules_GenerationProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "GenerationProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemTypeWeights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    WeaponType = table.Column<int>(type: "integer", nullable: true),
                    ArmorType = table.Column<int>(type: "integer", nullable: true),
                    Weight = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTypeWeights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemTypeWeights_GenerationProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "GenerationProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    EmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    EmailVerificationTokenHash = table.Column<string>(type: "text", nullable: true),
                    EmailVerificationTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PasswordResetTokenHash = table.Column<string>(type: "text", nullable: true),
                    PasswordResetTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastDailyReward = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Currency = table.Column<decimal>(type: "numeric", nullable: false),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    BlockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BlockReason = table.Column<string>(type: "text", nullable: true),
                    ProfileImagePath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_GenerationProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "GenerationProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Battles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Battles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Battles_Runs_RunId",
                        column: x => x.RunId,
                        principalTable: "Runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StageScenarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StageProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnemyCount = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageScenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageScenarios_StageProfiles_StageProfileId",
                        column: x => x.StageProfileId,
                        principalTable: "StageProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemElements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArmorId = table.Column<Guid>(type: "uuid", nullable: true),
                    WeaponId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemElementType = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemElements", x => x.Id);
                    table.CheckConstraint("CK_ItemElement_OnlyOneOwner", "(\"WeaponId\" IS NULL AND \"ArmorId\" IS NOT NULL)\r\n                  OR\r\n                  (\"WeaponId\" IS NOT NULL AND \"ArmorId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_ItemElements_Armors_ArmorId",
                        column: x => x.ArmorId,
                        principalTable: "Armors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemElements_Weapons_WeaponId",
                        column: x => x.WeaponId,
                        principalTable: "Weapons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemElementSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ElementType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemElementSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemElementSettings_ItemGenerationRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "ItemGenerationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemParameterSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Parameter = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemParameterSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemParameterSettings_ItemGenerationRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "ItemGenerationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "BattleEnemies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BattleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Class = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    CurrentHp = table.Column<int>(type: "integer", nullable: false),
                    MaxHp = table.Column<int>(type: "integer", nullable: false),
                    IsDisorganized = table.Column<bool>(type: "boolean", nullable: false),
                    SkipNextTurn = table.Column<bool>(type: "boolean", nullable: false),
                    LeftHandItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    RightHandItemId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleEnemies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BattleEnemies_Battles_BattleId",
                        column: x => x.BattleId,
                        principalTable: "Battles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    ClassProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenarioSlots_StageScenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "StageScenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DistributionSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemElementSettingId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemParameterSettingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Min = table.Column<double>(type: "double precision", nullable: false),
                    Max = table.Column<double>(type: "double precision", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributionSegments", x => x.Id);
                    table.CheckConstraint("CK_DistributionSegment_OnlyOneParent", "(\"ItemParameterSettingId\" IS NULL AND \"ItemElementSettingId\" IS NOT NULL)\r\n                  OR\r\n                  (\"ItemParameterSettingId\" IS NOT NULL AND \"ItemElementSettingId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_DistributionSegments_ItemElementSettings_ItemElementSetting~",
                        column: x => x.ItemElementSettingId,
                        principalTable: "ItemElementSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DistributionSegments_ItemParameterSettings_ItemParameterSet~",
                        column: x => x.ItemParameterSettingId,
                        principalTable: "ItemParameterSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Equipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    BattleEnemyId = table.Column<Guid>(type: "uuid", nullable: true),
                    HeadId = table.Column<Guid>(type: "uuid", nullable: true),
                    BodyId = table.Column<Guid>(type: "uuid", nullable: true),
                    GlovesId = table.Column<Guid>(type: "uuid", nullable: true),
                    LegsId = table.Column<Guid>(type: "uuid", nullable: true),
                    BootsId = table.Column<Guid>(type: "uuid", nullable: true),
                    WeaponSlot1Id = table.Column<Guid>(type: "uuid", nullable: true),
                    WeaponSlot2Id = table.Column<Guid>(type: "uuid", nullable: true),
                    WeaponSlot3Id = table.Column<Guid>(type: "uuid", nullable: true),
                    WeaponSlot4Id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipments_BattleEnemies_BattleEnemyId",
                        column: x => x.BattleEnemyId,
                        principalTable: "BattleEnemies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Equipments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BattleEnemies_BattleId",
                table: "BattleEnemies",
                column: "BattleId");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_RunId",
                table: "Battles",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionSegments_ItemElementSettingId",
                table: "DistributionSegments",
                column: "ItemElementSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionSegments_ItemParameterSettingId",
                table: "DistributionSegments",
                column: "ItemParameterSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_BattleEnemyId",
                table: "Equipments",
                column: "BattleEnemyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_UserId",
                table: "Equipments",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_UserId_ItemId",
                table: "InventoryItems",
                columns: new[] { "UserId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemElements_ArmorId",
                table: "ItemElements",
                column: "ArmorId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemElements_WeaponId",
                table: "ItemElements",
                column: "WeaponId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemElementSettings_RuleId",
                table: "ItemElementSettings",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemGenerationRules_ProfileId",
                table: "ItemGenerationRules",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemParameterSettings_RuleId",
                table: "ItemParameterSettings",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemTypeWeights_ProfileId",
                table: "ItemTypeWeights",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketInventoryItems_UserId_ItemId",
                table: "MarketInventoryItems",
                columns: new[] { "UserId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketListings_Category",
                table: "MarketListings",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_MarketListings_Price",
                table: "MarketListings",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_RunInventoryItems_UserId_ItemId",
                table: "RunInventoryItems",
                columns: new[] { "UserId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioSlots_ScenarioId",
                table: "ScenarioSlots",
                column: "ScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_StageScenarios_StageProfileId",
                table: "StageScenarios",
                column: "StageProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ProfileId",
                table: "Users",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminLogs");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "DistributionSegments");

            migrationBuilder.DropTable(
                name: "EnemyClassProfiles");

            migrationBuilder.DropTable(
                name: "Equipments");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "ItemElements");

            migrationBuilder.DropTable(
                name: "ItemTypeWeights");

            migrationBuilder.DropTable(
                name: "MarketInventoryItems");

            migrationBuilder.DropTable(
                name: "MarketListings");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RunInventoryItems");

            migrationBuilder.DropTable(
                name: "ScenarioSlots");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "ItemElementSettings");

            migrationBuilder.DropTable(
                name: "ItemParameterSettings");

            migrationBuilder.DropTable(
                name: "BattleEnemies");

            migrationBuilder.DropTable(
                name: "Armors");

            migrationBuilder.DropTable(
                name: "Weapons");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "StageScenarios");

            migrationBuilder.DropTable(
                name: "ItemGenerationRules");

            migrationBuilder.DropTable(
                name: "Battles");

            migrationBuilder.DropTable(
                name: "StageProfiles");

            migrationBuilder.DropTable(
                name: "GenerationProfiles");

            migrationBuilder.DropTable(
                name: "Runs");
        }
    }
}
