using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LootNet_API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateI : Migration
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
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<int>(type: "integer", nullable: false),
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
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastDailyReward = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Currency = table.Column<decimal>(type: "numeric", nullable: false),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    BlockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BlockReason = table.Column<string>(type: "text", nullable: true)
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
                name: "Equipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
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
                        name: "FK_Equipments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
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

            migrationBuilder.CreateIndex(
                name: "IX_DistributionSegments_ItemElementSettingId",
                table: "DistributionSegments",
                column: "ItemElementSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionSegments_ItemParameterSettingId",
                table: "DistributionSegments",
                column: "ItemParameterSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_UserId",
                table: "Equipments",
                column: "UserId",
                unique: true);

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
                name: "IX_MarketListings_Category",
                table: "MarketListings",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_MarketListings_Price",
                table: "MarketListings",
                column: "Price");

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
                name: "DistributionSegments");

            migrationBuilder.DropTable(
                name: "Equipments");

            migrationBuilder.DropTable(
                name: "ItemElements");

            migrationBuilder.DropTable(
                name: "ItemTypeWeights");

            migrationBuilder.DropTable(
                name: "MarketListings");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "ItemElementSettings");

            migrationBuilder.DropTable(
                name: "ItemParameterSettings");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Armors");

            migrationBuilder.DropTable(
                name: "Weapons");

            migrationBuilder.DropTable(
                name: "ItemGenerationRules");

            migrationBuilder.DropTable(
                name: "GenerationProfiles");
        }
    }
}
