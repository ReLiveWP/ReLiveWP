using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Mailbox.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bcc",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Body",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "BodyType",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cc",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentClass",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ConversationId",
                table: "Items",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ConversationIndex",
                table: "Items",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateReceived",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayTo",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Email_NativeBodyType",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email_Subject",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FlagCompleteTime",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FlagDateCompleted",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FlagDueDate",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FlagReminderSet",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FlagReminderTime",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FlagStartDate",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "FlagStatus",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlagSubject",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlagType",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FlagUtcDueDate",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FlagUtcStartDate",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "From",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Importance",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternetCPID",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastVerbExecuted",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastVerbExecutionTime",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MessageClass",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MimeRaw",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Read",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReplyTo",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sender",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThreadTopic",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "To",
                table: "Items",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bcc",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Body",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "BodyType",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Cc",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ContentClass",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ConversationIndex",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DateReceived",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DisplayTo",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Email_NativeBodyType",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Email_Subject",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "FlagCompleteTime",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "FlagDateCompleted",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "FlagDueDate",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "FlagReminderSet",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "FlagReminderTime",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "FlagStartDate",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "FlagStatus",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "FlagSubject",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "FlagType",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "FlagUtcDueDate",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "FlagUtcStartDate",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "From",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Importance",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "InternetCPID",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "LastVerbExecuted",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "LastVerbExecutionTime",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "MessageClass",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "MimeRaw",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Read",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ReplyTo",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Sender",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ThreadTopic",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "To",
                table: "Items");
        }
    }
}
