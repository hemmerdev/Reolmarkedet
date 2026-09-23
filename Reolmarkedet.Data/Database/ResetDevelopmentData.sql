USE ReolmarkedetDB;
GO

SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    -- Delete child records before their referenced parent records.
    DELETE FROM dbo.SALE;
    DELETE FROM dbo.ITEM;
    DELETE FROM dbo.RENTAL;
    DELETE FROM dbo.SHELF;
    DELETE FROM dbo.TENANT;
    DELETE FROM dbo.SHELFTYPE;

    -- Reset generated IDs so new test records begin at 1.
    DBCC CHECKIDENT ('dbo.SALE', RESEED, 0);
    DBCC CHECKIDENT ('dbo.ITEM', RESEED, 0);
    DBCC CHECKIDENT ('dbo.RENTAL', RESEED, 0);
    DBCC CHECKIDENT ('dbo.SHELF', RESEED, 0);
    DBCC CHECKIDENT ('dbo.TENANT', RESEED, 0);
    DBCC CHECKIDENT ('dbo.SHELFTYPE', RESEED, 0);

    -- Restore the standard shelf types required by the application.
    INSERT INTO dbo.SHELFTYPE (ShelfTypeName)
    VALUES
        (N'6 hylder'),
        (N'3 hylder og bøjlestang');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO