using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgMullagain.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardIndexes_Cards_CardId",
                table: "CardIndexes");

            migrationBuilder.DropForeignKey(
                name: "FK_DeckCards_Cards_CardId",
                table: "DeckCards");

            migrationBuilder.DropForeignKey(
                name: "FK_DeckCards_Decks_DeckId",
                table: "DeckCards");

            migrationBuilder.DropForeignKey(
                name: "FK_HandEvals_Hands_Hand1Id",
                table: "HandEvals");

            migrationBuilder.DropForeignKey(
                name: "FK_HandEvals_Hands_Hand2Id",
                table: "HandEvals");

            migrationBuilder.DropForeignKey(
                name: "FK_HandEvals_Hands_HandId",
                table: "HandEvals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Hands",
                table: "Hands");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Decks",
                table: "Decks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cards",
                table: "Cards");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImportMetadata",
                table: "ImportMetadata");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HandEvals",
                table: "HandEvals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeckCards",
                table: "DeckCards");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CardIndexes",
                table: "CardIndexes");

            migrationBuilder.RenameTable(
                name: "Hands",
                newName: "hands");

            migrationBuilder.RenameTable(
                name: "Decks",
                newName: "decks");

            migrationBuilder.RenameTable(
                name: "Cards",
                newName: "cards");

            migrationBuilder.RenameTable(
                name: "ImportMetadata",
                newName: "import_metadata");

            migrationBuilder.RenameTable(
                name: "HandEvals",
                newName: "hand_evals");

            migrationBuilder.RenameTable(
                name: "DeckCards",
                newName: "deck_cards");

            migrationBuilder.RenameTable(
                name: "CardIndexes",
                newName: "card_indexes");

            migrationBuilder.RenameColumn(
                name: "Size",
                table: "hands",
                newName: "size");

            migrationBuilder.RenameColumn(
                name: "Hash64",
                table: "hands",
                newName: "hash64");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "hands",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "hands",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CardIntIds",
                table: "hands",
                newName: "card_int_ids");

            migrationBuilder.RenameColumn(
                name: "CanonicalKey",
                table: "hands",
                newName: "canonical_key");

            migrationBuilder.RenameColumn(
                name: "Owner",
                table: "decks",
                newName: "owner");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "decks",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Format",
                table: "decks",
                newName: "format");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "decks",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "decks",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "decks",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "cards",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "cards",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Colors",
                table: "cards",
                newName: "colors");

            migrationBuilder.RenameColumn(
                name: "Cmc",
                table: "cards",
                newName: "cmc");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "cards",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "cards",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "PowerToughness",
                table: "cards",
                newName: "power_toughness");

            migrationBuilder.RenameColumn(
                name: "OracleText",
                table: "cards",
                newName: "oracle_text");

            migrationBuilder.RenameColumn(
                name: "ManaCost",
                table: "cards",
                newName: "mana_cost");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "cards",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "import_metadata",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "import_metadata",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "import_metadata",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "ImportedAt",
                table: "import_metadata",
                newName: "imported_at");

            migrationBuilder.RenameColumn(
                name: "DownloadUri",
                table: "import_metadata",
                newName: "download_uri");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "hand_evals",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PreferredHand",
                table: "hand_evals",
                newName: "preferred_hand");

            migrationBuilder.RenameColumn(
                name: "Hand2Id",
                table: "hand_evals",
                newName: "hand2_id");

            migrationBuilder.RenameColumn(
                name: "Hand1Id",
                table: "hand_evals",
                newName: "hand1_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "hand_evals",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ContextJson",
                table: "hand_evals",
                newName: "context_json");

            migrationBuilder.RenameIndex(
                name: "IX_HandEvals_HandId",
                table: "hand_evals",
                newName: "IX_hand_evals_HandId");

            migrationBuilder.RenameColumn(
                name: "Count",
                table: "deck_cards",
                newName: "count");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "deck_cards",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CardId",
                table: "deck_cards",
                newName: "card_id");

            migrationBuilder.RenameColumn(
                name: "DeckId",
                table: "deck_cards",
                newName: "deck_id");

            migrationBuilder.RenameIndex(
                name: "IX_DeckCards_CardId",
                table: "deck_cards",
                newName: "IX_deck_cards_card_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "card_indexes",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "card_indexes",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CardId",
                table: "card_indexes",
                newName: "card_id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "import_metadata",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "p_k_hands",
                table: "hands",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "p_k_decks",
                table: "decks",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "p_k_cards",
                table: "cards",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "p_k_import_metadata",
                table: "import_metadata",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "p_k_hand_evals",
                table: "hand_evals",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_deck_cards",
                table: "deck_cards",
                columns: new[] { "deck_id", "card_id" });

            migrationBuilder.AddPrimaryKey(
                name: "p_k_card_indexes",
                table: "card_indexes",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_card_indexes_cards_card_id",
                table: "card_indexes",
                column: "card_id",
                principalTable: "cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_deck_cards_cards_card_id",
                table: "deck_cards",
                column: "card_id",
                principalTable: "cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_deck_cards_decks_deck_id",
                table: "deck_cards",
                column: "deck_id",
                principalTable: "decks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_hand_evals_hands_HandId",
                table: "hand_evals",
                column: "HandId",
                principalTable: "hands",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_hand_evals_hands_hand1_id",
                table: "hand_evals",
                column: "hand1_id",
                principalTable: "hands",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_hand_evals_hands_hand2_id",
                table: "hand_evals",
                column: "hand2_id",
                principalTable: "hands",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_card_indexes_cards_card_id",
                table: "card_indexes");

            migrationBuilder.DropForeignKey(
                name: "f_k_deck_cards_cards_card_id",
                table: "deck_cards");

            migrationBuilder.DropForeignKey(
                name: "f_k_deck_cards_decks_deck_id",
                table: "deck_cards");

            migrationBuilder.DropForeignKey(
                name: "FK_hand_evals_hands_HandId",
                table: "hand_evals");

            migrationBuilder.DropForeignKey(
                name: "FK_hand_evals_hands_hand1_id",
                table: "hand_evals");

            migrationBuilder.DropForeignKey(
                name: "FK_hand_evals_hands_hand2_id",
                table: "hand_evals");

            migrationBuilder.DropPrimaryKey(
                name: "p_k_hands",
                table: "hands");

            migrationBuilder.DropPrimaryKey(
                name: "p_k_decks",
                table: "decks");

            migrationBuilder.DropPrimaryKey(
                name: "p_k_cards",
                table: "cards");

            migrationBuilder.DropPrimaryKey(
                name: "p_k_import_metadata",
                table: "import_metadata");

            migrationBuilder.DropPrimaryKey(
                name: "p_k_hand_evals",
                table: "hand_evals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_deck_cards",
                table: "deck_cards");

            migrationBuilder.DropPrimaryKey(
                name: "p_k_card_indexes",
                table: "card_indexes");

            migrationBuilder.RenameTable(
                name: "hands",
                newName: "Hands");

            migrationBuilder.RenameTable(
                name: "decks",
                newName: "Decks");

            migrationBuilder.RenameTable(
                name: "cards",
                newName: "Cards");

            migrationBuilder.RenameTable(
                name: "import_metadata",
                newName: "ImportMetadata");

            migrationBuilder.RenameTable(
                name: "hand_evals",
                newName: "HandEvals");

            migrationBuilder.RenameTable(
                name: "deck_cards",
                newName: "DeckCards");

            migrationBuilder.RenameTable(
                name: "card_indexes",
                newName: "CardIndexes");

            migrationBuilder.RenameColumn(
                name: "size",
                table: "Hands",
                newName: "Size");

            migrationBuilder.RenameColumn(
                name: "hash64",
                table: "Hands",
                newName: "Hash64");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Hands",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Hands",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "card_int_ids",
                table: "Hands",
                newName: "CardIntIds");

            migrationBuilder.RenameColumn(
                name: "canonical_key",
                table: "Hands",
                newName: "CanonicalKey");

            migrationBuilder.RenameColumn(
                name: "owner",
                table: "Decks",
                newName: "Owner");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Decks",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "format",
                table: "Decks",
                newName: "Format");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Decks",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Decks",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Decks",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "Cards",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Cards",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "colors",
                table: "Cards",
                newName: "Colors");

            migrationBuilder.RenameColumn(
                name: "cmc",
                table: "Cards",
                newName: "Cmc");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Cards",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Cards",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "power_toughness",
                table: "Cards",
                newName: "PowerToughness");

            migrationBuilder.RenameColumn(
                name: "oracle_text",
                table: "Cards",
                newName: "OracleText");

            migrationBuilder.RenameColumn(
                name: "mana_cost",
                table: "Cards",
                newName: "ManaCost");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Cards",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "ImportMetadata",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ImportMetadata",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "ImportMetadata",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "imported_at",
                table: "ImportMetadata",
                newName: "ImportedAt");

            migrationBuilder.RenameColumn(
                name: "download_uri",
                table: "ImportMetadata",
                newName: "DownloadUri");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "HandEvals",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "preferred_hand",
                table: "HandEvals",
                newName: "PreferredHand");

            migrationBuilder.RenameColumn(
                name: "hand2_id",
                table: "HandEvals",
                newName: "Hand2Id");

            migrationBuilder.RenameColumn(
                name: "hand1_id",
                table: "HandEvals",
                newName: "Hand1Id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "HandEvals",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "context_json",
                table: "HandEvals",
                newName: "ContextJson");

            migrationBuilder.RenameIndex(
                name: "IX_hand_evals_HandId",
                table: "HandEvals",
                newName: "IX_HandEvals_HandId");

            migrationBuilder.RenameColumn(
                name: "count",
                table: "DeckCards",
                newName: "Count");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "DeckCards",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "card_id",
                table: "DeckCards",
                newName: "CardId");

            migrationBuilder.RenameColumn(
                name: "deck_id",
                table: "DeckCards",
                newName: "DeckId");

            migrationBuilder.RenameIndex(
                name: "IX_deck_cards_card_id",
                table: "DeckCards",
                newName: "IX_DeckCards_CardId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "CardIndexes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "CardIndexes",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "card_id",
                table: "CardIndexes",
                newName: "CardId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ImportMetadata",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Hands",
                table: "Hands",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Decks",
                table: "Decks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cards",
                table: "Cards",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImportMetadata",
                table: "ImportMetadata",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HandEvals",
                table: "HandEvals",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeckCards",
                table: "DeckCards",
                columns: new[] { "DeckId", "CardId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_CardIndexes",
                table: "CardIndexes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CardIndexes_Cards_CardId",
                table: "CardIndexes",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeckCards_Cards_CardId",
                table: "DeckCards",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeckCards_Decks_DeckId",
                table: "DeckCards",
                column: "DeckId",
                principalTable: "Decks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HandEvals_Hands_Hand1Id",
                table: "HandEvals",
                column: "Hand1Id",
                principalTable: "Hands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HandEvals_Hands_Hand2Id",
                table: "HandEvals",
                column: "Hand2Id",
                principalTable: "Hands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HandEvals_Hands_HandId",
                table: "HandEvals",
                column: "HandId",
                principalTable: "Hands",
                principalColumn: "Id");
        }
    }
}
