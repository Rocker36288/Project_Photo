using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project_Photo.Migrations.Post
{
    /// <inheritdoc />
    public partial class InitPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "SocialNetwork");

            migrationBuilder.CreateTable(
                name: "Posts",
                schema: "SocialNetwork",
                columns: table => new
                {
                    PostId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    PostType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PostTypeId = table.Column<int>(type: "int", nullable: true),
                    PostContent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ParentPostId = table.Column<int>(type: "int", nullable: true),
                    GroupId = table.Column<int>(type: "int", nullable: true),
                    EventId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    DeletedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Post", x => x.PostId);
                    table.ForeignKey(
                        name: "FK_Posts_Posts",
                        column: x => x.ParentPostId,
                        principalSchema: "SocialNetwork",
                        principalTable: "Posts",
                        principalColumn: "PostId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ParentPostId",
                schema: "SocialNetwork",
                table: "Posts",
                column: "ParentPostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Posts",
                schema: "SocialNetwork");
        }
    }
}
