using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ReLiveWP.Backend.Mailbox.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    DeviceId = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: true),
                    IMEI = table.Column<string>(type: "text", nullable: true),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    OS = table.Column<string>(type: "text", nullable: true),
                    OSLanguage = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    EnableOutboundSMS = table.Column<int>(type: "integer", nullable: true),
                    MobileOperator = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FolderEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    ServerId = table.Column<string>(type: "text", nullable: false),
                    ParentServerId = table.Column<string>(type: "text", nullable: true),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    FolderType = table.Column<int>(type: "integer", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolderEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Folders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ParentServerId = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SourceId = table.Column<string>(type: "text", nullable: true),
                    AccountName = table.Column<string>(type: "text", nullable: true),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CollectionId = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    ServerId = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    DeviceId = table.Column<string>(type: "text", nullable: false),
                    CollectionId = table.Column<string>(type: "text", nullable: false),
                    SyncKey = table.Column<string>(type: "text", nullable: false),
                    Watermark = table.Column<long>(type: "bigint", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CachedAnnotationNames = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CollectionId = table.Column<string>(type: "text", nullable: false),
                    ServerId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ItemClass = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Timezone = table.Column<string>(type: "text", nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DtStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Uid = table.Column<string>(type: "text", nullable: true),
                    ClientUid = table.Column<string>(type: "text", nullable: true),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    Reminder = table.Column<long>(type: "bigint", nullable: true),
                    AllDayEvent = table.Column<bool>(type: "boolean", nullable: true),
                    BusyStatus = table.Column<byte>(type: "smallint", nullable: true),
                    Sensitivity = table.Column<byte>(type: "smallint", nullable: true),
                    MeetingStatus = table.Column<byte>(type: "smallint", nullable: true),
                    OrganizerName = table.Column<string>(type: "text", nullable: true),
                    OrganizerEmail = table.Column<string>(type: "text", nullable: true),
                    DbCalendarItem_Notes = table.Column<string>(type: "text", nullable: true),
                    NativeBodyType = table.Column<byte>(type: "smallint", nullable: true),
                    BodyLegacy = table.Column<string>(type: "text", nullable: true),
                    BodyTruncated = table.Column<bool>(type: "boolean", nullable: true),
                    AppointmentReplyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResponseType = table.Column<long>(type: "bigint", nullable: true),
                    ResponseRequested = table.Column<bool>(type: "boolean", nullable: true),
                    DisallowNewTimeProposal = table.Column<bool>(type: "boolean", nullable: true),
                    OnlineMeetingConfLink = table.Column<string>(type: "text", nullable: true),
                    OnlineMeetingExternalLink = table.Column<string>(type: "text", nullable: true),
                    RecurrenceType = table.Column<byte>(type: "smallint", nullable: true),
                    RecurrenceOccurrences = table.Column<int>(type: "integer", nullable: true),
                    RecurrenceInterval = table.Column<int>(type: "integer", nullable: true),
                    RecurrenceWeekOfMonth = table.Column<byte>(type: "smallint", nullable: true),
                    RecurrenceDayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    RecurrenceMonthOfYear = table.Column<byte>(type: "smallint", nullable: true),
                    RecurrenceUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecurrenceDayOfMonth = table.Column<byte>(type: "smallint", nullable: true),
                    RecurrenceCalendarType = table.Column<byte>(type: "smallint", nullable: true),
                    RecurrenceIsLeapMonth = table.Column<bool>(type: "boolean", nullable: true),
                    RecurrenceFirstDayOfWeek = table.Column<byte>(type: "smallint", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    MiddleName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Suffix = table.Column<string>(type: "text", nullable: true),
                    FileAs = table.Column<string>(type: "text", nullable: true),
                    Alias = table.Column<string>(type: "text", nullable: true),
                    NickName = table.Column<string>(type: "text", nullable: true),
                    YomiFirstName = table.Column<string>(type: "text", nullable: true),
                    YomiLastName = table.Column<string>(type: "text", nullable: true),
                    YomiCompanyName = table.Column<string>(type: "text", nullable: true),
                    CompanyName = table.Column<string>(type: "text", nullable: true),
                    Department = table.Column<string>(type: "text", nullable: true),
                    JobTitle = table.Column<string>(type: "text", nullable: true),
                    OfficeLocation = table.Column<string>(type: "text", nullable: true),
                    AccountName = table.Column<string>(type: "text", nullable: true),
                    ManagerName = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<string>(type: "text", nullable: true),
                    GovernmentId = table.Column<string>(type: "text", nullable: true),
                    AssistantName = table.Column<string>(type: "text", nullable: true),
                    Email1Address = table.Column<string>(type: "text", nullable: true),
                    Email2Address = table.Column<string>(type: "text", nullable: true),
                    Email3Address = table.Column<string>(type: "text", nullable: true),
                    BusinessPhoneNumber = table.Column<string>(type: "text", nullable: true),
                    Business2PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    BusinessFaxNumber = table.Column<string>(type: "text", nullable: true),
                    HomePhoneNumber = table.Column<string>(type: "text", nullable: true),
                    Home2PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    HomeFaxNumber = table.Column<string>(type: "text", nullable: true),
                    MobilePhoneNumber = table.Column<string>(type: "text", nullable: true),
                    CarPhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PagerNumber = table.Column<string>(type: "text", nullable: true),
                    RadioPhoneNumber = table.Column<string>(type: "text", nullable: true),
                    AssistantPhoneNumber = table.Column<string>(type: "text", nullable: true),
                    CompanyMainPhone = table.Column<string>(type: "text", nullable: true),
                    MMS = table.Column<string>(type: "text", nullable: true),
                    IMAddress = table.Column<string>(type: "text", nullable: true),
                    IMAddress2 = table.Column<string>(type: "text", nullable: true),
                    IMAddress3 = table.Column<string>(type: "text", nullable: true),
                    BusinessAddressStreet = table.Column<string>(type: "text", nullable: true),
                    BusinessAddressCity = table.Column<string>(type: "text", nullable: true),
                    BusinessAddressState = table.Column<string>(type: "text", nullable: true),
                    BusinessAddressPostalCode = table.Column<string>(type: "text", nullable: true),
                    BusinessAddressCountry = table.Column<string>(type: "text", nullable: true),
                    HomeAddressStreet = table.Column<string>(type: "text", nullable: true),
                    HomeAddressCity = table.Column<string>(type: "text", nullable: true),
                    HomeAddressState = table.Column<string>(type: "text", nullable: true),
                    HomeAddressPostalCode = table.Column<string>(type: "text", nullable: true),
                    HomeAddressCountry = table.Column<string>(type: "text", nullable: true),
                    OtherAddressStreet = table.Column<string>(type: "text", nullable: true),
                    OtherAddressCity = table.Column<string>(type: "text", nullable: true),
                    OtherAddressState = table.Column<string>(type: "text", nullable: true),
                    OtherAddressPostalCode = table.Column<string>(type: "text", nullable: true),
                    OtherAddressCountry = table.Column<string>(type: "text", nullable: true),
                    Spouse = table.Column<string>(type: "text", nullable: true),
                    Birthday = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Anniversary = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WebPage = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Picture = table.Column<byte[]>(type: "bytea", nullable: true),
                    WeightedRank = table.Column<int>(type: "integer", nullable: true),
                    To = table.Column<string>(type: "text", nullable: true),
                    Cc = table.Column<string>(type: "text", nullable: true),
                    Bcc = table.Column<string>(type: "text", nullable: true),
                    From = table.Column<string>(type: "text", nullable: true),
                    ReplyTo = table.Column<string>(type: "text", nullable: true),
                    DisplayTo = table.Column<string>(type: "text", nullable: true),
                    Sender = table.Column<string>(type: "text", nullable: true),
                    Email_Subject = table.Column<string>(type: "text", nullable: true),
                    DateReceived = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ThreadTopic = table.Column<string>(type: "text", nullable: true),
                    Importance = table.Column<byte>(type: "smallint", nullable: true),
                    Read = table.Column<bool>(type: "boolean", nullable: true),
                    MessageClass = table.Column<string>(type: "text", nullable: true),
                    InternetCPID = table.Column<string>(type: "text", nullable: true),
                    ContentClass = table.Column<string>(type: "text", nullable: true),
                    ConversationId = table.Column<byte[]>(type: "bytea", nullable: true),
                    ConversationIndex = table.Column<byte[]>(type: "bytea", nullable: true),
                    LastVerbExecuted = table.Column<int>(type: "integer", nullable: true),
                    LastVerbExecutionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: true),
                    BodyType = table.Column<byte>(type: "smallint", nullable: true),
                    Email_NativeBodyType = table.Column<byte>(type: "smallint", nullable: true),
                    MimeRaw = table.Column<string>(type: "text", nullable: true),
                    FlagStatus = table.Column<byte>(type: "smallint", nullable: true),
                    FlagType = table.Column<string>(type: "text", nullable: true),
                    FlagSubject = table.Column<string>(type: "text", nullable: true),
                    FlagDateCompleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FlagCompleteTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FlagStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FlagDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FlagUtcStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FlagUtcDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FlagReminderSet = table.Column<bool>(type: "boolean", nullable: true),
                    FlagReminderTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    Id = table.Column<string>(type: "text", nullable: false),
                    CalendarItemId = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AttendeeStatus = table.Column<byte>(type: "smallint", nullable: true),
                    AttendeeType = table.Column<byte>(type: "smallint", nullable: true)
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
                    Id = table.Column<string>(type: "text", nullable: false),
                    CalendarItemId = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false)
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
                    Id = table.Column<string>(type: "text", nullable: false),
                    CalendarItemId = table.Column<string>(type: "text", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: true),
                    ExceptionStartTime = table.Column<string>(type: "text", nullable: true),
                    InstanceId = table.Column<string>(type: "text", nullable: true),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    Sensitivity = table.Column<byte>(type: "smallint", nullable: true),
                    BusyStatus = table.Column<byte>(type: "smallint", nullable: true),
                    AllDayEvent = table.Column<bool>(type: "boolean", nullable: true),
                    Reminder = table.Column<long>(type: "bigint", nullable: true),
                    DtStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MeetingStatus = table.Column<byte>(type: "smallint", nullable: true),
                    AppointmentReplyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResponseType = table.Column<long>(type: "bigint", nullable: true),
                    OnlineMeetingConfLink = table.Column<string>(type: "text", nullable: true),
                    OnlineMeetingExternalLink = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    BodyLegacy = table.Column<string>(type: "text", nullable: true)
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
                name: "ContactAnnotations",
                columns: table => new
                {
                    ContactItemId = table.Column<string>(type: "text", nullable: false),
                    Cid = table.Column<long>(type: "bigint", nullable: true),
                    ObjectId = table.Column<string>(type: "text", nullable: true),
                    WLId = table.Column<string>(type: "text", nullable: true),
                    ImMri = table.Column<string>(type: "text", nullable: true),
                    ContactType = table.Column<string>(type: "text", nullable: true),
                    UserTileUrl = table.Column<string>(type: "text", nullable: true),
                    UserTileHash = table.Column<string>(type: "text", nullable: true),
                    TrustLevel = table.Column<int>(type: "integer", nullable: true),
                    FavoriteOrder = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactAnnotations", x => x.ContactItemId);
                    table.ForeignKey(
                        name: "FK_ContactAnnotations_Items_ContactItemId",
                        column: x => x.ContactItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContactCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ContactItemId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
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
                    Id = table.Column<string>(type: "text", nullable: false),
                    ContactItemId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
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
                    Id = table.Column<string>(type: "text", nullable: false),
                    CalendarExceptionId = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AttendeeStatus = table.Column<byte>(type: "smallint", nullable: true),
                    AttendeeType = table.Column<byte>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarExceptionAttendees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarExceptionAttendees_CalendarExceptions_CalendarExcep~",
                        column: x => x.CalendarExceptionId,
                        principalTable: "CalendarExceptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalendarExceptionCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CalendarExceptionId = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarExceptionCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarExceptionCategories_CalendarExceptions_CalendarExce~",
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
                name: "IX_DeviceInfos_UserId_DeviceId",
                table: "DeviceInfos",
                columns: new[] { "UserId", "DeviceId" },
                unique: true);

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
                name: "ContactAnnotations");

            migrationBuilder.DropTable(
                name: "ContactCategories");

            migrationBuilder.DropTable(
                name: "ContactChildren");

            migrationBuilder.DropTable(
                name: "DeviceInfos");

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
