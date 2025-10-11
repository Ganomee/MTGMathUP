using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MtgMullagain.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable pgvector extension
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

            // Create count_intersection function for overlap queries
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION count_intersection(arr1 integer[], arr2 integer[])
                RETURNS integer AS $$
                BEGIN
                    RETURN (
                        SELECT COUNT(*)
                        FROM unnest(arr1) AS elem
                        WHERE elem = ANY(arr2)
                    );
                END;
                $$ LANGUAGE plpgsql IMMUTABLE;");

            // Create Cards table
            migrationBuilder.CreateTable(
                name: "Cards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ManaCost = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OracleText = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PowerToughness = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Cmc = table.Column<int>(type: "integer", nullable: false),
                    Colors = table.Column<string[]>(type: "text[]", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.Id);
                });

            // Create CardIndexes table
            migrationBuilder.CreateTable(
                name: "CardIndexes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CardId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardIndexes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardIndexes_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create Decks table
            migrationBuilder.CreateTable(
                name: "Decks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Owner = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Decks", x => x.Id);
                });

            // Create DeckCards table (many-to-many)
            migrationBuilder.CreateTable(
                name: "DeckCards",
                columns: table => new
                {
                    DeckId = table.Column<int>(type: "integer", nullable: false),
                    CardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeckCards", x => new { x.DeckId, x.CardId });
                    table.ForeignKey(
                        name: "FK_DeckCards_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeckCards_Decks_DeckId",
                        column: x => x.DeckId,
                        principalTable: "Decks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create Hands table
            migrationBuilder.CreateTable(
                name: "Hands",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CardIntIds = table.Column<int[]>(type: "integer[]", nullable: false),
                    Size = table.Column<short>(type: "smallint", nullable: false),
                    Hash64 = table.Column<long>(type: "bigint", nullable: false),
                    CanonicalKey = table.Column<string>(type: "text", nullable: false, computedColumnSql: "array_to_string(\"CardIntIds\", ',')", stored: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hands", x => x.Id);
                });

            // Create HandEvals table
            migrationBuilder.CreateTable(
                name: "HandEvals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Hand1Id = table.Column<long>(type: "bigint", nullable: false),
                    Hand2Id = table.Column<long>(type: "bigint", nullable: false),
                    PreferredHand = table.Column<int>(type: "integer", nullable: false),
                    ContextJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandEvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandEvals_Hands_Hand1Id",
                        column: x => x.Hand1Id,
                        principalTable: "Hands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HandEvals_Hands_Hand2Id",
                        column: x => x.Hand2Id,
                        principalTable: "Hands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create CardEmbeddings table
            migrationBuilder.CreateTable(
                name: "CardEmbeddings",
                columns: table => new
                {
                    CardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Embedding = table.Column<float[]>(type: "vector(256)", nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardEmbeddings", x => x.CardId);
                    table.ForeignKey(
                        name: "FK_CardEmbeddings_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create HandEmbeddings table
            migrationBuilder.CreateTable(
                name: "HandEmbeddings",
                columns: table => new
                {
                    HandId = table.Column<long>(type: "bigint", nullable: false),
                    Embedding = table.Column<float[]>(type: "vector(256)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandEmbeddings", x => x.HandId);
                    table.ForeignKey(
                        name: "FK_HandEmbeddings_Hands_HandId",
                        column: x => x.HandId,
                        principalTable: "Hands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Cards_Name",
                table: "Cards",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_Type",
                table: "Cards",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_Cmc",
                table: "Cards",
                column: "Cmc");

            migrationBuilder.CreateIndex(
                name: "IX_CardIndexes_CardId",
                table: "CardIndexes",
                column: "CardId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Decks_Name",
                table: "Decks",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Decks_Format",
                table: "Decks",
                column: "Format");

            migrationBuilder.CreateIndex(
                name: "IX_Decks_Owner",
                table: "Decks",
                column: "Owner");

            migrationBuilder.CreateIndex(
                name: "IX_Hands_Hash64",
                table: "Hands",
                column: "Hash64");

            migrationBuilder.CreateIndex(
                name: "IX_Hands_CreatedAt",
                table: "Hands",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Hands_CardIntIds_GIN",
                table: "Hands",
                column: "CardIntIds")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Hands_Hash64_Size_CardIntIds_Unique",
                table: "Hands",
                columns: new[] { "Hash64", "Size", "CardIntIds" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HandEvals_Hand1Id",
                table: "HandEvals",
                column: "Hand1Id");

            migrationBuilder.CreateIndex(
                name: "IX_HandEvals_Hand2Id",
                table: "HandEvals",
                column: "Hand2Id");

            migrationBuilder.CreateIndex(
                name: "IX_HandEvals_PreferredHand",
                table: "HandEvals",
                column: "PreferredHand");

            migrationBuilder.CreateIndex(
                name: "IX_HandEvals_CreatedAt",
                table: "HandEvals",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CardEmbeddings_ModelVersion",
                table: "CardEmbeddings",
                column: "ModelVersion");

            migrationBuilder.CreateIndex(
                name: "IX_CardEmbeddings_CreatedAt",
                table: "CardEmbeddings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CardEmbeddings_Embedding_IVFFlat",
                table: "CardEmbeddings",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "ivfflat")
                .Annotation("Npgsql:IndexOperators", "vector_cosine_ops");

            migrationBuilder.CreateIndex(
                name: "IX_HandEmbeddings_Embedding_IVFFlat",
                table: "HandEmbeddings",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "ivfflat")
                .Annotation("Npgsql:IndexOperators", "vector_cosine_ops");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardEmbeddings");

            migrationBuilder.DropTable(
                name: "CardIndexes");

            migrationBuilder.DropTable(
                name: "DeckCards");

            migrationBuilder.DropTable(
                name: "HandEmbeddings");

            migrationBuilder.DropTable(
                name: "HandEvals");

            migrationBuilder.DropTable(
                name: "Hands");

            migrationBuilder.DropTable(
                name: "Cards");

            migrationBuilder.DropTable(
                name: "Decks");
        }
    }
}
