using System;
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
            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.AddColumn<Guid>(
                name: "ProfileId",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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
                name: "ParamDistribution",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParamDistribution", x => x.Id);
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
                    IsFallback = table.Column<bool>(type: "boolean", nullable: false),
                    GenerationProfileId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemGenerationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemGenerationRules_GenerationProfiles_GenerationProfileId",
                        column: x => x.GenerationProfileId,
                        principalTable: "GenerationProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ItemTypeWeight",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    WeaponType = table.Column<int>(type: "integer", nullable: true),
                    ArmorType = table.Column<int>(type: "integer", nullable: true),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    GenerationProfileId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTypeWeight", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemTypeWeight_GenerationProfiles_GenerationProfileId",
                        column: x => x.GenerationProfileId,
                        principalTable: "GenerationProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DistributionSegment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Min = table.Column<double>(type: "double precision", nullable: false),
                    Max = table.Column<double>(type: "double precision", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    ParamDistributionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributionSegment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistributionSegment_ParamDistribution_ParamDistributionId",
                        column: x => x.ParamDistributionId,
                        principalTable: "ParamDistribution",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ItemElement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemElementType = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    ArmorId = table.Column<Guid>(type: "uuid", nullable: true),
                    WeaponId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemElement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemElement_Armors_ArmorId",
                        column: x => x.ArmorId,
                        principalTable: "Armors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemElement_Weapons_WeaponId",
                        column: x => x.WeaponId,
                        principalTable: "Weapons",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ItemElementSetting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ElementType = table.Column<int>(type: "integer", nullable: false),
                    DistributionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemGenerationRuleId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemElementSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemElementSetting_ItemGenerationRules_ItemGenerationRuleId",
                        column: x => x.ItemGenerationRuleId,
                        principalTable: "ItemGenerationRules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemElementSetting_ParamDistribution_DistributionId",
                        column: x => x.DistributionId,
                        principalTable: "ParamDistribution",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemParameterSetting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Parameter = table.Column<int>(type: "integer", nullable: false),
                    DistributionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemGenerationRuleId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemParameterSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemParameterSetting_ItemGenerationRules_ItemGenerationRule~",
                        column: x => x.ItemGenerationRuleId,
                        principalTable: "ItemGenerationRules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemParameterSetting_ParamDistribution_DistributionId",
                        column: x => x.DistributionId,
                        principalTable: "ParamDistribution",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ProfileId",
                table: "Users",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionSegment_ParamDistributionId",
                table: "DistributionSegment",
                column: "ParamDistributionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemElement_ArmorId",
                table: "ItemElement",
                column: "ArmorId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemElement_WeaponId",
                table: "ItemElement",
                column: "WeaponId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemElementSetting_DistributionId",
                table: "ItemElementSetting",
                column: "DistributionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemElementSetting_ItemGenerationRuleId",
                table: "ItemElementSetting",
                column: "ItemGenerationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemGenerationRules_GenerationProfileId",
                table: "ItemGenerationRules",
                column: "GenerationProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemParameterSetting_DistributionId",
                table: "ItemParameterSetting",
                column: "DistributionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemParameterSetting_ItemGenerationRuleId",
                table: "ItemParameterSetting",
                column: "ItemGenerationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemTypeWeight_GenerationProfileId",
                table: "ItemTypeWeight",
                column: "GenerationProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_GenerationProfiles_ProfileId",
                table: "Users",
                column: "ProfileId",
                principalTable: "GenerationProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_GenerationProfiles_ProfileId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "DistributionSegment");

            migrationBuilder.DropTable(
                name: "ItemElement");

            migrationBuilder.DropTable(
                name: "ItemElementSetting");

            migrationBuilder.DropTable(
                name: "ItemParameterSetting");

            migrationBuilder.DropTable(
                name: "ItemTypeWeight");

            migrationBuilder.DropTable(
                name: "Armors");

            migrationBuilder.DropTable(
                name: "Weapons");

            migrationBuilder.DropTable(
                name: "ItemGenerationRules");

            migrationBuilder.DropTable(
                name: "ParamDistribution");

            migrationBuilder.DropTable(
                name: "GenerationProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Users_ProfileId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rarity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });
        }
    }
}
