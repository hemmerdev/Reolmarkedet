-- Removes only the fictional tenants created by SeedRealisticDemoData.sql
-- and their rentals, items and sales. Shelves and shelf types are preserved.
-- Any records you manually add under these demo tenants are removed too.

USE ReolmarkedetDB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DemoTenants TABLE (TenantID INT PRIMARY KEY);
INSERT INTO @DemoTenants (TenantID)
SELECT TenantID
FROM dbo.TENANT
WHERE Email LIKE N'demo[0-9][0-9]@reolmarkedet-demo.invalid';

BEGIN TRY
    BEGIN TRANSACTION;

    DELETE s
    FROM dbo.SALE AS s
    JOIN dbo.ITEM AS i ON i.ItemID = s.ItemID
    JOIN dbo.RENTAL AS r ON r.RentalID = i.RentalID
    JOIN @DemoTenants AS d ON d.TenantID = r.TenantID;

    DELETE i
    FROM dbo.ITEM AS i
    JOIN dbo.RENTAL AS r ON r.RentalID = i.RentalID
    JOIN @DemoTenants AS d ON d.TenantID = r.TenantID;

    DELETE r
    FROM dbo.RENTAL AS r
    JOIN @DemoTenants AS d ON d.TenantID = r.TenantID;

    DELETE t
    FROM dbo.TENANT AS t
    JOIN @DemoTenants AS d ON d.TenantID = t.TenantID;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT COUNT(*) AS RemovedDemoTenants FROM @DemoTenants;
