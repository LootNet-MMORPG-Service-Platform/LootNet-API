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
            migrationBuilder.DropForeignKey(
                name: "FK_DistributionSegment_ParamDistribution_ParamDistributionId",
                table: "DistributionSegment");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemElementSetting_ParamDistribution_DistributionId",
                table: "ItemElementSetting");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemParameterSetting_ParamDistribution_DistributionId",
                table: "ItemParameterSetting");

            migrationBuilder.DropTable(
                name: "ParamDistribution");

            migrationBuilder.DropIndex(
                name: "IX_ItemParameterSetting_DistributionId",
                table: "ItemParameterSetting");

            migrationBuilder.DropIndex(
                name: "IX_ItemElementSetting_DistributionId",
                table: "ItemElementSetting");

            migrationBuilder.DropColumn(
                name: "DistributionId",
                table: "ItemParameterSetting");

            migrationBuilder.DropColumn(
                name: "DistributionId",
                table: "ItemElementSetting");

            migrationBuilder.RenameColumn(
                name: "ParamDistributionId",
                table: "DistributionSegment",
                newName: "ItemParameterSettingId");

            migrationBuilder.RenameIndex(
                name: "IX_DistributionSegment_ParamDistributionId",
                table: "DistributionSegment",
                newName: "IX_DistributionSegment_ItemParameterSettingId");

            migrationBuilder.AddColumn<Guid>(
                name: "ItemElementSettingId",
                table: "DistributionSegment",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DistributionSegment_ItemElementSettingId",
                table: "DistributionSegment",
                column: "ItemElementSettingId");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributionSegment_ItemElementSetting_ItemElementSettingId",
                table: "DistributionSegment",
                column: "ItemElementSettingId",
                principalTable: "ItemElementSetting",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributionSegment_ItemParameterSetting_ItemParameterSetti~",
                table: "DistributionSegment",
                column: "ItemParameterSettingId",
                principalTable: "ItemParameterSetting",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributionSegment_ItemElementSetting_ItemElementSettingId",
                table: "DistributionSegment");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributionSegment_ItemParameterSetting_ItemParameterSetti~",
                table: "DistributionSegment");

            migrationBuilder.DropIndex(
                name: "IX_DistributionSegment_ItemElementSettingId",
                table: "DistributionSegment");

            migrationBuilder.DropColumn(
                name: "ItemElementSettingId",
                table: "DistributionSegment");

            migrationBuilder.RenameColumn(
                name: "ItemParameterSettingId",
                table: "DistributionSegment",
                newName: "ParamDistributionId");

            migrationBuilder.RenameIndex(
                name: "IX_DistributionSegment_ItemParameterSettingId",
                table: "DistributionSegment",
                newName: "IX_DistributionSegment_ParamDistributionId");

            migrationBuilder.AddColumn<Guid>(
                name: "DistributionId",
                table: "ItemParameterSetting",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DistributionId",
                table: "ItemElementSetting",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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

            migrationBuilder.CreateIndex(
                name: "IX_ItemParameterSetting_DistributionId",
                table: "ItemParameterSetting",
                column: "DistributionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemElementSetting_DistributionId",
                table: "ItemElementSetting",
                column: "DistributionId");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributionSegment_ParamDistribution_ParamDistributionId",
                table: "DistributionSegment",
                column: "ParamDistributionId",
                principalTable: "ParamDistribution",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemElementSetting_ParamDistribution_DistributionId",
                table: "ItemElementSetting",
                column: "DistributionId",
                principalTable: "ParamDistribution",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemParameterSetting_ParamDistribution_DistributionId",
                table: "ItemParameterSetting",
                column: "DistributionId",
                principalTable: "ParamDistribution",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
