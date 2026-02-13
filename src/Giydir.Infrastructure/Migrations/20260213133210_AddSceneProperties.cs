using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Giydir.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSceneProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ModelAssets tablosuna yeni kolonlar ekle
            migrationBuilder.AddColumn<string>(
                name: "DefaultBackground",
                table: "ModelAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultLighting",
                table: "ModelAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultPose",
                table: "ModelAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultCameraAngle",
                table: "ModelAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultMood",
                table: "ModelAssets",
                type: "TEXT",
                nullable: true);

            // Templates tablosuna yeni kolonlar ekle
            migrationBuilder.AddColumn<string>(
                name: "Background",
                table: "Templates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Lighting",
                table: "Templates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pose",
                table: "Templates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CameraAngle",
                table: "Templates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mood",
                table: "Templates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresModel",
                table: "Templates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ModelAssets kolonlarını sil
            migrationBuilder.DropColumn(
                name: "DefaultBackground",
                table: "ModelAssets");

            migrationBuilder.DropColumn(
                name: "DefaultLighting",
                table: "ModelAssets");

            migrationBuilder.DropColumn(
                name: "DefaultPose",
                table: "ModelAssets");

            migrationBuilder.DropColumn(
                name: "DefaultCameraAngle",
                table: "ModelAssets");

            migrationBuilder.DropColumn(
                name: "DefaultMood",
                table: "ModelAssets");

            // Templates kolonlarını sil
            migrationBuilder.DropColumn(
                name: "Background",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "Lighting",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "Pose",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "CameraAngle",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "Mood",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "RequiresModel",
                table: "Templates");
        }
    }
}
