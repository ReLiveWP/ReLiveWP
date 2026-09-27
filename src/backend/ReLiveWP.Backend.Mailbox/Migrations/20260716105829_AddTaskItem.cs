using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Mailbox.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Task_Complete",
                table: "Items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Task_DateCompleted",
                table: "Items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Task_DueDate",
                table: "Items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Task_Importance",
                table: "Items",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Task_NativeBodyType",
                table: "Items",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Task_Notes",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Task_RecurrenceCalendarType",
                table: "Items",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Task_RecurrenceDayOfMonth",
                table: "Items",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Task_RecurrenceDayOfWeek",
                table: "Items",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Task_RecurrenceDeadOccur",
                table: "Items",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Task_RecurrenceFirstDayOfWeek",
                table: "Items",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Task_RecurrenceInterval",
                table: "Items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Task_RecurrenceIsLeapMonth",
                table: "Items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Task_RecurrenceMonthOfYear",
                table: "Items",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Task_RecurrenceOccurrences",
                table: "Items",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Task_RecurrenceRegenerate",
                table: "Items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Task_RecurrenceStart",
                table: "Items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Task_RecurrenceType",
                table: "Items",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Task_RecurrenceUntil",
                table: "Items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Task_RecurrenceWeekOfMonth",
                table: "Items",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Task_ReminderSet",
                table: "Items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Task_ReminderTime",
                table: "Items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Task_Sensitivity",
                table: "Items",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Task_StartDate",
                table: "Items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Task_Subject",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Task_UtcDueDate",
                table: "Items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Task_UtcStartDate",
                table: "Items",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Task_Complete",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_DateCompleted",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_DueDate",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_Importance",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_NativeBodyType",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_Notes",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_RecurrenceCalendarType",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_RecurrenceDayOfMonth",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_RecurrenceDayOfWeek",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_RecurrenceDeadOccur",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_RecurrenceFirstDayOfWeek",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_RecurrenceInterval",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_RecurrenceIsLeapMonth",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_RecurrenceMonthOfYear",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_RecurrenceOccurrences",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_RecurrenceRegenerate",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_RecurrenceStart",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_RecurrenceType",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_RecurrenceUntil",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_RecurrenceWeekOfMonth",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_ReminderSet",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_ReminderTime",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_Sensitivity",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_StartDate",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_Subject",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_UtcDueDate",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Task_UtcStartDate",
                table: "Items");
        }
    }
}
