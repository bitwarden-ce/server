IF OBJECT_ID('[dbo].[Organization_Create]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Organization_Create]
END
GO

CREATE PROCEDURE [dbo].[Organization_Create]
    @Id UNIQUEIDENTIFIER,
    @Name NVARCHAR(50),
    @BusinessName NVARCHAR(50),
    @BusinessAddress1 NVARCHAR(50),
    @BusinessAddress2 NVARCHAR(50),
    @BusinessAddress3 NVARCHAR(50),
    @BusinessCountry VARCHAR(2),
    @BusinessTaxNumber NVARCHAR(30),
    @CreationDate DATETIME2(7),
    @RevisionDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON

    INSERT INTO [dbo].[Organization]
    (
        [Id],
        [Name],
        [BusinessName],
        [BusinessAddress1],
        [BusinessAddress2],
        [BusinessAddress3],
        [BusinessCountry],
        [BusinessTaxNumber],
        [CreationDate],
        [RevisionDate]
    )
    VALUES
    (
        @Id,
        @Name,
        @BusinessName,
        @BusinessAddress1,
        @BusinessAddress2,
        @BusinessAddress3,
        @BusinessCountry,
        @BusinessTaxNumber,
        @CreationDate,
        @RevisionDate
    )
END
GO

IF OBJECT_ID('[dbo].[Organization_Update]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Organization_Update]
END
GO

CREATE PROCEDURE [dbo].[Organization_Update]
    @Id UNIQUEIDENTIFIER,
    @Name NVARCHAR(50),
    @BusinessName NVARCHAR(50),
    @BusinessAddress1 NVARCHAR(50),
    @BusinessAddress2 NVARCHAR(50),
    @BusinessAddress3 NVARCHAR(50),
    @BusinessCountry VARCHAR(2),
    @BusinessTaxNumber NVARCHAR(30),
    @CreationDate DATETIME2(7),
    @RevisionDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON

    UPDATE
        [dbo].[Organization]
    SET
        [Name] = @Name,
        [BusinessName] = @BusinessName,
        [BusinessAddress1] = @BusinessAddress1,
        [BusinessAddress2] = @BusinessAddress2,
        [BusinessAddress3] = @BusinessAddress3,
        [BusinessCountry] = @BusinessCountry,
        [BusinessTaxNumber] = @BusinessTaxNumber,
        [CreationDate] = @CreationDate,
        [RevisionDate] = @RevisionDate
    WHERE
        [Id] = @Id
END
GO

IF EXISTS(SELECT * FROM sys.views WHERE [Name] = 'OrganizationView')
BEGIN
    DROP VIEW [dbo].[OrganizationView]
END
GO

CREATE VIEW [dbo].[OrganizationView]
AS
SELECT
    *
FROM
    [dbo].[Organization]
GO

IF EXISTS(SELECT * FROM sys.views WHERE [Name] = 'OrganizationUserOrganizationDetailsView')
BEGIN
    DROP VIEW [dbo].[OrganizationUserOrganizationDetailsView]
END
GO

CREATE VIEW [dbo].[OrganizationUserOrganizationDetailsView]
AS
SELECT
    OU.[UserId],
    OU.[OrganizationId],
    O.[Name],
    OU.[Key],
    OU.[Status],
    OU.[Type]
FROM
    [dbo].[OrganizationUser] OU
INNER JOIN
    [dbo].[Organization] O ON O.[Id] = OU.[OrganizationId]
GO

IF OBJECT_ID('[dbo].[Event]') IS NULL
BEGIN
    CREATE TABLE [dbo].[Event] (
        [Id]                    UNIQUEIDENTIFIER NOT NULL,
        [Type]                  INT              NOT NULL,
        [UserId]                UNIQUEIDENTIFIER NULL,
        [OrganizationId]        UNIQUEIDENTIFIER NULL,
        [CipherId]              UNIQUEIDENTIFIER NULL,
        [CollectionId]          UNIQUEIDENTIFIER NULL,
        [GroupId]               UNIQUEIDENTIFIER NULL,
        [OrganizationUserId]    UNIQUEIDENTIFIER NULL,
        [ActingUserId]          UNIQUEIDENTIFIER NULL,
        [DeviceType]            SMALLINT         NULL,
        [IpAddress]             VARCHAR(50)      NULL,
        [Date]                  DATETIME2 (7)    NOT NULL,
        CONSTRAINT [PK_Event] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_Event_DateOrganizationIdUserId]
        ON [dbo].[Event]([Date] DESC, [OrganizationId] ASC, [ActingUserId] ASC, [CipherId] ASC);
END
GO

IF OBJECT_ID('[dbo].[Event_Create]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Event_Create]
END
GO

CREATE PROCEDURE [dbo].[Event_Create]
    @Id UNIQUEIDENTIFIER,
    @Type INT,
    @UserId UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @CipherId UNIQUEIDENTIFIER,
    @CollectionId UNIQUEIDENTIFIER,
    @GroupId UNIQUEIDENTIFIER,
    @OrganizationUserId UNIQUEIDENTIFIER,
    @ActingUserId UNIQUEIDENTIFIER,
    @DeviceType SMALLINT,
    @IpAddress VARCHAR(50),
    @Date DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON

    INSERT INTO [dbo].[Event]
    (
        [Id],
        [Type],
        [UserId],
        [OrganizationId],
        [CipherId],
        [CollectionId],
        [GroupId],
        [OrganizationUserId],
        [ActingUserId],
        [DeviceType],
        [IpAddress],
        [Date]
    )
    VALUES
    (
        @Id,
        @Type,
        @UserId,
        @OrganizationId,
        @CipherId,
        @CollectionId,
        @GroupId,
        @OrganizationUserId,
        @ActingUserId,
        @DeviceType,
        @IpAddress,
        @Date
    )
END
GO

IF OBJECT_ID('[dbo].[Event_ReadPageByCipherId]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Event_ReadPageByCipherId]
END
GO

CREATE PROCEDURE [dbo].[Event_ReadPageByCipherId]
    @OrganizationId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @CipherId UNIQUEIDENTIFIER,
    @StartDate DATETIME2(7),
    @EndDate DATETIME2(7),
    @BeforeDate DATETIME2(7),
    @PageSize INT
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        *
    FROM
        [dbo].[EventView]
    WHERE
        [Date] >= @StartDate
        AND (@BeforeDate IS NOT NULL OR [Date] <= @EndDate)
        AND (@BeforeDate IS NULL OR [Date] < @BeforeDate)
        AND (
            (@OrganizationId IS NULL AND [OrganizationId] IS NULL)
            OR (@OrganizationId IS NOT NULL AND [OrganizationId] = @OrganizationId)
        )
        AND (
            (@UserId IS NULL AND [UserId] IS NULL)
            OR (@UserId IS NOT NULL AND [UserId] = @UserId)
        )
        AND [CipherId]  = @CipherId
    ORDER BY [Date] DESC
    OFFSET 0 ROWS
    FETCH NEXT @PageSize ROWS ONLY
END
GO

IF OBJECT_ID('[dbo].[Event_ReadPageByOrganizationId]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Event_ReadPageByOrganizationId]
END
GO

CREATE PROCEDURE [dbo].[Event_ReadPageByOrganizationId]
    @OrganizationId UNIQUEIDENTIFIER,
    @StartDate DATETIME2(7),
    @EndDate DATETIME2(7),
    @BeforeDate DATETIME2(7),
    @PageSize INT
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        *
    FROM
        [dbo].[EventView]
    WHERE
        [Date] >= @StartDate
        AND (@BeforeDate IS NOT NULL OR [Date] <= @EndDate)
        AND (@BeforeDate IS NULL OR [Date] < @BeforeDate)
        AND [OrganizationId] = @OrganizationId
    ORDER BY [Date] DESC
    OFFSET 0 ROWS
    FETCH NEXT @PageSize ROWS ONLY
END
GO

IF OBJECT_ID('[dbo].[Event_ReadPageByOrganizationIdActingUserId]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Event_ReadPageByOrganizationIdActingUserId]
END
GO

CREATE PROCEDURE [dbo].[Event_ReadPageByOrganizationIdActingUserId]
    @OrganizationId UNIQUEIDENTIFIER,
    @ActingUserId UNIQUEIDENTIFIER,
    @StartDate DATETIME2(7),
    @EndDate DATETIME2(7),
    @BeforeDate DATETIME2(7),
    @PageSize INT
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        *
    FROM
        [dbo].[EventView]
    WHERE
        [Date] >= @StartDate
        AND (@BeforeDate IS NOT NULL OR [Date] <= @EndDate)
        AND (@BeforeDate IS NULL OR [Date] < @BeforeDate)
        AND [OrganizationId] = @OrganizationId
        AND [ActingUserId] = @ActingUserId
    ORDER BY [Date] DESC
    OFFSET 0 ROWS
    FETCH NEXT @PageSize ROWS ONLY
END
GO

IF OBJECT_ID('[dbo].[Event_ReadPageByUserId]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Event_ReadPageByUserId]
END
GO

CREATE PROCEDURE [dbo].[Event_ReadPageByUserId]
    @UserId UNIQUEIDENTIFIER,
    @StartDate DATETIME2(7),
    @EndDate DATETIME2(7),
    @BeforeDate DATETIME2(7),
    @PageSize INT
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        *
    FROM
        [dbo].[EventView]
    WHERE
        [Date] >= @StartDate
        AND (@BeforeDate IS NOT NULL OR [Date] <= @EndDate)
        AND (@BeforeDate IS NULL OR [Date] < @BeforeDate)
        AND [OrganizationId] IS NULL
        AND [ActingUserId] = @UserId
    ORDER BY [Date] DESC
    OFFSET 0 ROWS
    FETCH NEXT @PageSize ROWS ONLY
END
GO

IF EXISTS(SELECT * FROM sys.views WHERE [Name] = 'EventView')
BEGIN
    DROP VIEW [dbo].[EventView]
END
GO

CREATE VIEW [dbo].[EventView]
AS
SELECT
    *
FROM
    [dbo].[Event]
GO
