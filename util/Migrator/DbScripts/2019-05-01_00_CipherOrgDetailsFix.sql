IF OBJECT_ID('[dbo].[CipherDetails_ReadById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[CipherDetails_ReadById]
END
GO

IF OBJECT_ID('[dbo].[CipherOrganizationDetails_ReadById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[CipherOrganizationDetails_ReadById]
END
GO

CREATE PROCEDURE [dbo].[CipherOrganizationDetails_ReadById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        C.*
    FROM
        [dbo].[CipherView] C
    WHERE
        C.[Id] = @Id
END
GO
