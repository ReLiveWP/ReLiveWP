using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Services.Exchange.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FolderEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    EventType = table.Column<int>(type: "INTEGER", nullable: false),
                    ServerId = table.Column<string>(type: "TEXT", nullable: false),
                    ParentServerId = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    FolderType = table.Column<int>(type: "INTEGER", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolderEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Folders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ParentServerId = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CollectionId = table.Column<string>(type: "TEXT", nullable: false),
                    EventType = table.Column<int>(type: "INTEGER", nullable: false),
                    ServerId = table.Column<string>(type: "TEXT", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    CollectionId = table.Column<string>(type: "TEXT", nullable: false),
                    SyncKey = table.Column<string>(type: "TEXT", nullable: false),
                    Watermark = table.Column<long>(type: "INTEGER", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CollectionId = table.Column<string>(type: "TEXT", nullable: false),
                    ServerId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ItemClass = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Timezone = table.Column<string>(type: "TEXT", nullable: true),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DtStamp = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Uid = table.Column<string>(type: "TEXT", nullable: true),
                    ClientUid = table.Column<string>(type: "TEXT", nullable: true),
                    Subject = table.Column<string>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    Reminder = table.Column<uint>(type: "INTEGER", nullable: true),
                    AllDayEvent = table.Column<bool>(type: "INTEGER", nullable: true),
                    BusyStatus = table.Column<byte>(type: "INTEGER", nullable: true),
                    Sensitivity = table.Column<byte>(type: "INTEGER", nullable: true),
                    MeetingStatus = table.Column<byte>(type: "INTEGER", nullable: true),
                    OrganizerName = table.Column<string>(type: "TEXT", nullable: true),
                    OrganizerEmail = table.Column<string>(type: "TEXT", nullable: true),
                    CalendarItem_Notes = table.Column<string>(type: "TEXT", nullable: true),
                    NativeBodyType = table.Column<byte>(type: "INTEGER", nullable: true),
                    BodyLegacy = table.Column<string>(type: "TEXT", nullable: true),
                    BodyTruncated = table.Column<bool>(type: "INTEGER", nullable: true),
                    AppointmentReplyTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResponseType = table.Column<uint>(type: "INTEGER", nullable: true),
                    ResponseRequested = table.Column<bool>(type: "INTEGER", nullable: true),
                    DisallowNewTimeProposal = table.Column<bool>(type: "INTEGER", nullable: true),
                    OnlineMeetingConfLink = table.Column<string>(type: "TEXT", nullable: true),
                    OnlineMeetingExternalLink = table.Column<string>(type: "TEXT", nullable: true),
                    RecurrenceType = table.Column<byte>(type: "INTEGER", nullable: true),
                    RecurrenceOccurrences = table.Column<ushort>(type: "INTEGER", nullable: true),
                    RecurrenceInterval = table.Column<ushort>(type: "INTEGER", nullable: true),
                    RecurrenceWeekOfMonth = table.Column<byte>(type: "INTEGER", nullable: true),
                    RecurrenceDayOfWeek = table.Column<ushort>(type: "INTEGER", nullable: true),
                    RecurrenceMonthOfYear = table.Column<byte>(type: "INTEGER", nullable: true),
                    RecurrenceUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecurrenceDayOfMonth = table.Column<byte>(type: "INTEGER", nullable: true),
                    RecurrenceCalendarType = table.Column<byte>(type: "INTEGER", nullable: true),
                    RecurrenceIsLeapMonth = table.Column<bool>(type: "INTEGER", nullable: true),
                    RecurrenceFirstDayOfWeek = table.Column<byte>(type: "INTEGER", nullable: true),
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    MiddleName = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Suffix = table.Column<string>(type: "TEXT", nullable: true),
                    FileAs = table.Column<string>(type: "TEXT", nullable: true),
                    Alias = table.Column<string>(type: "TEXT", nullable: true),
                    NickName = table.Column<string>(type: "TEXT", nullable: true),
                    YomiFirstName = table.Column<string>(type: "TEXT", nullable: true),
                    YomiLastName = table.Column<string>(type: "TEXT", nullable: true),
                    YomiCompanyName = table.Column<string>(type: "TEXT", nullable: true),
                    CompanyName = table.Column<string>(type: "TEXT", nullable: true),
                    Department = table.Column<string>(type: "TEXT", nullable: true),
                    JobTitle = table.Column<string>(type: "TEXT", nullable: true),
                    OfficeLocation = table.Column<string>(type: "TEXT", nullable: true),
                    AccountName = table.Column<string>(type: "TEXT", nullable: true),
                    ManagerName = table.Column<string>(type: "TEXT", nullable: true),
                    CustomerId = table.Column<string>(type: "TEXT", nullable: true),
                    GovernmentId = table.Column<string>(type: "TEXT", nullable: true),
                    AssistantName = table.Column<string>(type: "TEXT", nullable: true),
                    Email1Address = table.Column<string>(type: "TEXT", nullable: true),
                    Email2Address = table.Column<string>(type: "TEXT", nullable: true),
                    Email3Address = table.Column<string>(type: "TEXT", nullable: true),
                    BusinessPhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Business2PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    BusinessFaxNumber = table.Column<string>(type: "TEXT", nullable: true),
                    HomePhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Home2PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    HomeFaxNumber = table.Column<string>(type: "TEXT", nullable: true),
                    MobilePhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    CarPhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PagerNumber = table.Column<string>(type: "TEXT", nullable: true),
                    RadioPhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    AssistantPhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    CompanyMainPhone = table.Column<string>(type: "TEXT", nullable: true),
                    MMS = table.Column<string>(type: "TEXT", nullable: true),
                    IMAddress = table.Column<string>(type: "TEXT", nullable: true),
                    IMAddress2 = table.Column<string>(type: "TEXT", nullable: true),
                    IMAddress3 = table.Column<string>(type: "TEXT", nullable: true),
                    BusinessAddressStreet = table.Column<string>(type: "TEXT", nullable: true),
                    BusinessAddressCity = table.Column<string>(type: "TEXT", nullable: true),
                    BusinessAddressState = table.Column<string>(type: "TEXT", nullable: true),
                    BusinessAddressPostalCode = table.Column<string>(type: "TEXT", nullable: true),
                    BusinessAddressCountry = table.Column<string>(type: "TEXT", nullable: true),
                    HomeAddressStreet = table.Column<string>(type: "TEXT", nullable: true),
                    HomeAddressCity = table.Column<string>(type: "TEXT", nullable: true),
                    HomeAddressState = table.Column<string>(type: "TEXT", nullable: true),
                    HomeAddressPostalCode = table.Column<string>(type: "TEXT", nullable: true),
                    HomeAddressCountry = table.Column<string>(type: "TEXT", nullable: true),
                    OtherAddressStreet = table.Column<string>(type: "TEXT", nullable: true),
                    OtherAddressCity = table.Column<string>(type: "TEXT", nullable: true),
                    OtherAddressState = table.Column<string>(type: "TEXT", nullable: true),
                    OtherAddressPostalCode = table.Column<string>(type: "TEXT", nullable: true),
                    OtherAddressCountry = table.Column<string>(type: "TEXT", nullable: true),
                    Spouse = table.Column<string>(type: "TEXT", nullable: true),
                    Birthday = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Anniversary = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WebPage = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Picture = table.Column<byte[]>(type: "BLOB", nullable: true),
                    WeightedRank = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Folders_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalendarAttendees",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    CalendarItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    AttendeeStatus = table.Column<byte>(type: "INTEGER", nullable: true),
                    AttendeeType = table.Column<byte>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarAttendees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarAttendees_Items_CalendarItemId",
                        column: x => x.CalendarItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalendarCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    CalendarItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarCategories_Items_CalendarItemId",
                        column: x => x.CalendarItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalendarExceptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    CalendarItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: true),
                    ExceptionStartTime = table.Column<string>(type: "TEXT", nullable: true),
                    InstanceId = table.Column<string>(type: "TEXT", nullable: true),
                    Subject = table.Column<string>(type: "TEXT", nullable: true),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    Sensitivity = table.Column<byte>(type: "INTEGER", nullable: true),
                    BusyStatus = table.Column<byte>(type: "INTEGER", nullable: true),
                    AllDayEvent = table.Column<bool>(type: "INTEGER", nullable: true),
                    Reminder = table.Column<uint>(type: "INTEGER", nullable: true),
                    DtStamp = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MeetingStatus = table.Column<byte>(type: "INTEGER", nullable: true),
                    AppointmentReplyTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResponseType = table.Column<uint>(type: "INTEGER", nullable: true),
                    OnlineMeetingConfLink = table.Column<string>(type: "TEXT", nullable: true),
                    OnlineMeetingExternalLink = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    BodyLegacy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarExceptions_Items_CalendarItemId",
                        column: x => x.CalendarItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContactCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ContactItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactCategories_Items_ContactItemId",
                        column: x => x.ContactItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContactChildren",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ContactItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactChildren", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactChildren_Items_ContactItemId",
                        column: x => x.ContactItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalendarExceptionAttendees",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    CalendarExceptionId = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    AttendeeStatus = table.Column<byte>(type: "INTEGER", nullable: true),
                    AttendeeType = table.Column<byte>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarExceptionAttendees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarExceptionAttendees_CalendarExceptions_CalendarExceptionId",
                        column: x => x.CalendarExceptionId,
                        principalTable: "CalendarExceptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalendarExceptionCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    CalendarExceptionId = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarExceptionCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarExceptionCategories_CalendarExceptions_CalendarExceptionId",
                        column: x => x.CalendarExceptionId,
                        principalTable: "CalendarExceptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarAttendees_CalendarItemId",
                table: "CalendarAttendees",
                column: "CalendarItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarCategories_CalendarItemId",
                table: "CalendarCategories",
                column: "CalendarItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarExceptionAttendees_CalendarExceptionId",
                table: "CalendarExceptionAttendees",
                column: "CalendarExceptionId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarExceptionCategories_CalendarExceptionId",
                table: "CalendarExceptionCategories",
                column: "CalendarExceptionId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarExceptions_CalendarItemId",
                table: "CalendarExceptions",
                column: "CalendarItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactCategories_ContactItemId",
                table: "ContactCategories",
                column: "ContactItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactChildren_ContactItemId",
                table: "ContactChildren",
                column: "ContactItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FolderEvents_UserId_Id",
                table: "FolderEvents",
                columns: new[] { "UserId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Folders_UserId",
                table: "Folders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_UserId_Id",
                table: "Folders",
                columns: new[] { "UserId", "Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemEvents_UserId_CollectionId_Id",
                table: "ItemEvents",
                columns: new[] { "UserId", "CollectionId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_CollectionId",
                table: "Items",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_UserId_CollectionId",
                table: "Items",
                columns: new[] { "UserId", "CollectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_UserId_CollectionId_ServerId",
                table: "Items",
                columns: new[] { "UserId", "CollectionId", "ServerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncStates_UserId_DeviceId_CollectionId",
                table: "SyncStates",
                columns: new[] { "UserId", "DeviceId", "CollectionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarAttendees");

            migrationBuilder.DropTable(
                name: "CalendarCategories");

            migrationBuilder.DropTable(
                name: "CalendarExceptionAttendees");

            migrationBuilder.DropTable(
                name: "CalendarExceptionCategories");

            migrationBuilder.DropTable(
                name: "ContactCategories");

            migrationBuilder.DropTable(
                name: "ContactChildren");

            migrationBuilder.DropTable(
                name: "FolderEvents");

            migrationBuilder.DropTable(
                name: "ItemEvents");

            migrationBuilder.DropTable(
                name: "SyncStates");

            migrationBuilder.DropTable(
                name: "CalendarExceptions");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Folders");
        }
    }
}
