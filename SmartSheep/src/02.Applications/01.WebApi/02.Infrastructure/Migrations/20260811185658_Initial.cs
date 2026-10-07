using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppClient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ClientSecret = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiredInSecond = table.Column<double>(type: "float", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppClient", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppMenu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FkParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "VARCHAR(256)", maxLength: 256, nullable: false),
                    Controller = table.Column<string>(type: "VARCHAR(256)", maxLength: 256, nullable: true),
                    SequenceNumber = table.Column<string>(type: "VARCHAR(256)", maxLength: 256, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IconActive = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSection = table.Column<bool>(type: "bit", nullable: false),
                    AccessTypes = table.Column<string>(type: "VARCHAR(2000)", maxLength: 2000, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppMenu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppMenu_AppMenu_FkParentId",
                        column: x => x.FkParentId,
                        principalTable: "AppMenu",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppRole",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "VARCHAR(256)", maxLength: 256, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRole", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsHtmlTemplate = table.Column<bool>(type: "bit", nullable: false),
                    EmailCc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Farm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Code = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "VARCHAR(150)", maxLength: 150, nullable: false),
                    Location = table.Column<string>(type: "VARCHAR(250)", maxLength: 250, nullable: true),
                    Longitude = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: true),
                    Latitude = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "VARCHAR(500)", maxLength: 500, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Farm", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobExecutionLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "VARCHAR(20)", maxLength: 20, nullable: false),
                    StatusNotes = table.Column<string>(type: "VARCHAR(2000)", maxLength: 2000, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobExecutionLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Livestock",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Code = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "VARCHAR(150)", maxLength: 150, nullable: false),
                    Sex = table.Column<string>(type: "VARCHAR(10)", maxLength: 10, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Livestock", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "News",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Code = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "VARCHAR(256)", maxLength: 256, nullable: false),
                    ShortContent = table.Column<string>(type: "VARCHAR(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "VARCHAR(MAX)", nullable: false),
                    ImageThumbnail = table.Column<string>(type: "VARCHAR(1000)", maxLength: 1000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_News", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReminderTemplate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Code = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "VARCHAR(150)", maxLength: 150, nullable: false),
                    ReminderText = table.Column<string>(type: "VARCHAR(2000)", maxLength: 2000, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScopeClient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopeClient", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SucklingStatistic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Code = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: false),
                    PeriodDescription = table.Column<string>(type: "VARCHAR(250)", maxLength: 250, nullable: false),
                    TotalFrequency = table.Column<int>(type: "int", nullable: false),
                    TotalDurationSeconds = table.Column<int>(type: "int", nullable: false),
                    RequiresReminder = table.Column<bool>(type: "bit", nullable: false),
                    ActivityFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivityTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HourNumber = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SucklingStatistic", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfig",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    StatisticsDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    SourceVideoHealthCheckIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemNotification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PositionId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    DateRead = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemNotification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserLogin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Username = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "VARCHAR(512)", maxLength: 512, nullable: false),
                    Email = table.Column<string>(type: "VARCHAR(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "VARCHAR(256)", maxLength: 256, nullable: false),
                    UserImage = table.Column<string>(type: "VARCHAR(1000)", maxLength: 1000, nullable: true),
                    LatestLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HP = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogin", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuFavorite",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Email = table.Column<string>(type: "VARCHAR(256)", maxLength: 256, nullable: false),
                    FkAppMenuId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuFavorite", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuFavorite_AppMenu_FkAppMenuId",
                        column: x => x.FkAppMenuId,
                        principalTable: "AppMenu",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppMenuRole",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FkAppRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FkAppMenuId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccessTypes = table.Column<string>(type: "VARCHAR(2000)", maxLength: 2000, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppMenuRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppMenuRole_AppMenu_FkAppMenuId",
                        column: x => x.FkAppMenuId,
                        principalTable: "AppMenu",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AppMenuRole_AppRole_FkAppRoleId",
                        column: x => x.FkAppRoleId,
                        principalTable: "AppRole",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Barn",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Code = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "VARCHAR(150)", maxLength: 150, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "VARCHAR(20)", maxLength: 20, nullable: false),
                    FkFarmId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FkLivestockId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Barn", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Barn_Farm_FkFarmId",
                        column: x => x.FkFarmId,
                        principalTable: "Farm",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Barn_Livestock_FkLivestockId",
                        column: x => x.FkLivestockId,
                        principalTable: "Livestock",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppScopeClient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FkAppClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FkScopeClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppScopeClient", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppScopeClient_AppClient_FkAppClientId",
                        column: x => x.FkAppClientId,
                        principalTable: "AppClient",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AppScopeClient_ScopeClient_FkScopeClientId",
                        column: x => x.FkScopeClientId,
                        principalTable: "ScopeClient",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReminderLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FkReminderTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FkSucklingStatisticId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Username = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "VARCHAR(20)", maxLength: 20, nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "VARCHAR(1000)", maxLength: 1000, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReminderLog_ReminderTemplate_FkReminderTemplateId",
                        column: x => x.FkReminderTemplateId,
                        principalTable: "ReminderTemplate",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReminderLog_SucklingStatistic_FkSucklingStatisticId",
                        column: x => x.FkSucklingStatisticId,
                        principalTable: "SucklingStatistic",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserDeviceToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FkUserId = table.Column<Guid>(type: "uniqueidentifier", maxLength: 20, nullable: false),
                    DeviceToken = table.Column<string>(type: "VARCHAR(512)", maxLength: 512, nullable: false),
                    Platform = table.Column<string>(type: "VARCHAR(20)", maxLength: 20, nullable: true),
                    DeviceName = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeviceToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDeviceToken_UserLogin_FkUserId",
                        column: x => x.FkUserId,
                        principalTable: "UserLogin",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLoginRole",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FkUserId = table.Column<Guid>(type: "uniqueidentifier", maxLength: 20, nullable: false),
                    FkRoleId = table.Column<Guid>(type: "uniqueidentifier", maxLength: 20, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLoginRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLoginRole_AppRole_FkRoleId",
                        column: x => x.FkRoleId,
                        principalTable: "AppRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserLoginRole_UserLogin_FkUserId",
                        column: x => x.FkUserId,
                        principalTable: "UserLogin",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SourceVideo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FkBarnId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: false),
                    SourceVideoUrl = table.Column<string>(type: "VARCHAR(1000)", maxLength: 1000, nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    Username = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: true),
                    Password = table.Column<string>(type: "VARCHAR(250)", maxLength: 250, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceVideo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceVideo_Barn_FkBarnId",
                        column: x => x.FkBarnId,
                        principalTable: "Barn",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SucklingActivity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FkBarnId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActivityStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivityEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalFrequency = table.Column<int>(type: "int", nullable: false),
                    TotalDurationSeconds = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SucklingActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SucklingActivity_Barn_FkBarnId",
                        column: x => x.FkBarnId,
                        principalTable: "Barn",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SourceVideoStatusLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FkIdSourceVideo = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "VARCHAR(20)", maxLength: 20, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceVideoStatusLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceVideoStatusLog_SourceVideo_FkIdSourceVideo",
                        column: x => x.FkIdSourceVideo,
                        principalTable: "SourceVideo",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppMenu_FkParentId",
                table: "AppMenu",
                column: "FkParentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMenuRole_FkAppMenuId",
                table: "AppMenuRole",
                column: "FkAppMenuId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMenuRole_FkAppRoleId",
                table: "AppMenuRole",
                column: "FkAppRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AppScopeClient_FkAppClientId",
                table: "AppScopeClient",
                column: "FkAppClientId");

            migrationBuilder.CreateIndex(
                name: "IX_AppScopeClient_FkScopeClientId",
                table: "AppScopeClient",
                column: "FkScopeClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Barn_FkFarmId",
                table: "Barn",
                column: "FkFarmId");

            migrationBuilder.CreateIndex(
                name: "IX_Barn_FkLivestockId",
                table: "Barn",
                column: "FkLivestockId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuFavorite_FkAppMenuId",
                table: "MenuFavorite",
                column: "FkAppMenuId");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderLog_FkReminderTemplateId",
                table: "ReminderLog",
                column: "FkReminderTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderLog_FkSucklingStatisticId",
                table: "ReminderLog",
                column: "FkSucklingStatisticId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceVideo_FkBarnId",
                table: "SourceVideo",
                column: "FkBarnId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceVideoStatusLog_FkIdSourceVideo",
                table: "SourceVideoStatusLog",
                column: "FkIdSourceVideo");

            migrationBuilder.CreateIndex(
                name: "IX_SucklingActivity_FkBarnId",
                table: "SucklingActivity",
                column: "FkBarnId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceToken_DeviceToken",
                table: "UserDeviceToken",
                column: "DeviceToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceToken_FkUserId",
                table: "UserDeviceToken",
                column: "FkUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginRole_FkRoleId",
                table: "UserLoginRole",
                column: "FkRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginRole_FkUserId",
                table: "UserLoginRole",
                column: "FkUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppMenuRole");

            migrationBuilder.DropTable(
                name: "AppScopeClient");

            migrationBuilder.DropTable(
                name: "EmailTemplate");

            migrationBuilder.DropTable(
                name: "JobExecutionLog");

            migrationBuilder.DropTable(
                name: "MenuFavorite");

            migrationBuilder.DropTable(
                name: "News");

            migrationBuilder.DropTable(
                name: "ReminderLog");

            migrationBuilder.DropTable(
                name: "SourceVideoStatusLog");

            migrationBuilder.DropTable(
                name: "SucklingActivity");

            migrationBuilder.DropTable(
                name: "SystemConfig");

            migrationBuilder.DropTable(
                name: "SystemNotification");

            migrationBuilder.DropTable(
                name: "UserDeviceToken");

            migrationBuilder.DropTable(
                name: "UserLoginRole");

            migrationBuilder.DropTable(
                name: "AppClient");

            migrationBuilder.DropTable(
                name: "ScopeClient");

            migrationBuilder.DropTable(
                name: "AppMenu");

            migrationBuilder.DropTable(
                name: "ReminderTemplate");

            migrationBuilder.DropTable(
                name: "SucklingStatistic");

            migrationBuilder.DropTable(
                name: "SourceVideo");

            migrationBuilder.DropTable(
                name: "AppRole");

            migrationBuilder.DropTable(
                name: "UserLogin");

            migrationBuilder.DropTable(
                name: "Barn");

            migrationBuilder.DropTable(
                name: "Farm");

            migrationBuilder.DropTable(
                name: "Livestock");
        }
    }
}
