-- Optional demo data. Run after 001_CreateInitialSchema.sql and 002_SeedInitialData.sql.
-- Uses only existing, active shelves 1-80 that have never been rented.
-- All people and contact details are fictional. Run RemoveRealisticDemoData.sql to undo.
-- The dates are relative to the day this script runs: six completed months plus today.

USE ReolmarkedetDB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Today DATE = CONVERT(DATE, GETDATE());
DECLARE @CurrentMonthStart DATE = DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1);
DECLARE @LastCompletedDay DATE = DATEADD(DAY, -1, @CurrentMonthStart);

DECLARE @FreeShelves TABLE (
    ShelfKind CHAR(1) NOT NULL,
    Slot INT NOT NULL,
    ShelfID INT NOT NULL,
    PRIMARY KEY (ShelfKind, Slot)
);

-- Reserve 26 shelves of the six-shelf type and 8 with a clothes rail.
;WITH Available AS (
    SELECT
        CASE st.ShelfTypeName
            WHEN N'6 hylder' THEN 'S'
            WHEN N'3 hylder og bøjlestang' THEN 'R'
        END AS ShelfKind,
        s.ShelfID,
        ROW_NUMBER() OVER (
            PARTITION BY st.ShelfTypeName ORDER BY s.ShelfNumber
        ) AS Slot
    FROM dbo.SHELF AS s
    JOIN dbo.SHELFTYPE AS st ON st.ShelfTypeID = s.ShelfTypeID
    WHERE s.ShelfNumber BETWEEN 1 AND 80
      AND s.IsActive = 1
      AND st.ShelfTypeName IN (N'6 hylder', N'3 hylder og bøjlestang')
      AND NOT EXISTS (
          SELECT 1 FROM dbo.RENTAL AS r WHERE r.ShelfID = s.ShelfID
      )
)
INSERT INTO @FreeShelves (ShelfKind, Slot, ShelfID)
SELECT ShelfKind, CONVERT(INT, Slot), ShelfID
FROM Available
WHERE (ShelfKind = 'S' AND Slot <= 26)
   OR (ShelfKind = 'R' AND Slot <= 8);

IF EXISTS (
    SELECT 1 FROM dbo.TENANT
    WHERE Email LIKE N'demo[0-9][0-9]@reolmarkedet-demo.invalid'
)
BEGIN
    PRINT N'Demo data already exists. Run RemoveRealisticDemoData.sql before reseeding.';
    RETURN;
END;

IF (SELECT COUNT(*) FROM dbo.SHELF WHERE ShelfNumber BETWEEN 1 AND 80) <> 80
BEGIN
    THROW 51003, N'Run 002_SeedInitialData.sql first so shelves 1-80 exist.', 1;
END;

IF (SELECT COUNT(*) FROM @FreeShelves WHERE ShelfKind = 'S') <> 26
   OR (SELECT COUNT(*) FROM @FreeShelves WHERE ShelfKind = 'R') <> 8
BEGIN
    THROW 51000,
        N'Need 26 never-rented active six-shelf units and 8 never-rented active clothes-rail units among shelves 1-80. No data was inserted.',
        1;
END;

DECLARE @Tenants TABLE (Seq INT PRIMARY KEY, Name NVARCHAR(100) NOT NULL);
INSERT INTO @Tenants (Seq, Name) VALUES
    (1, N'Anna Mikkelsen'), (2, N'Peter Sørensen'),
    (3, N'Mette Jensen'), (4, N'Lars Nielsen'),
    (5, N'Sofie Rasmussen'), (6, N'Anders Pedersen'),
    (7, N'Camilla Andersen'), (8, N'Mikkel Christensen'),
    (9, N'Louise Thomsen'), (10, N'Jonas Poulsen'),
    (11, N'Ida Kristensen'), (12, N'Kasper Larsen'),
    (13, N'Line Hansen'), (14, N'Thomas Eriksen'),
    (15, N'Freja Mortensen'), (16, N'Emil Jørgensen'),
    (17, N'Sara Olsen'), (18, N'Martin Carlsen'),
    (19, N'Julie Holm'), (20, N'Rasmus Friis'),
    (21, N'Laura Dahl'), (22, N'Nikolaj Brandt'),
    (23, N'Emma Kjær'), (24, N'Oliver Lund');

DECLARE @Catalog TABLE (
    Seq INT PRIMARY KEY,
    Description NVARCHAR(255) NOT NULL,
    Price DECIMAL(10, 2) NOT NULL
);
INSERT INTO @Catalog (Seq, Description, Price) VALUES
    (1, N'Keramikvase, blå', 125.00),
    (2, N'Brætspil: Ticket to Ride', 180.00),
    (3, N'Vinterjakke, str. M', 295.00),
    (4, N'Bordlampe i messing', 225.00),
    (5, N'Roman: Den uendelige historie', 45.00),
    (6, N'Lædertaske, brun', 350.00),
    (7, N'Kaffekværn', 175.00),
    (8, N'Børnecykel, 20 tommer', 650.00),
    (9, N'Plakat i ramme', 90.00),
    (10, N'Vintersko, str. 39', 240.00),
    (11, N'LEGO-sæt, brugt og komplet', 425.00),
    (12, N'To keramikkrus', 70.00),
    (13, N'Strikket uldtrøje, str. L', 160.00),
    (14, N'Støbejernsgryde', 300.00),
    (15, N'Puslespil, 1000 brikker', 85.00),
    (16, N'Vintage spejl', 475.00),
    (17, N'Højttaler, bærbar', 520.00),
    (18, N'Børnebøger, sæt med 4', 110.00),
    (19, N'Guitar med taske', 950.00),
    (20, N'Skjorte i hør, str. M', 95.00),
    (21, N'Sidebord i egetræ', 800.00),
    (22, N'Kamera med objektiv', 1450.00),
    (23, N'Dekorativ skål', 130.00),
    (24, N'Babyalarm', 400.00),
    (25, N'Kogebog', 60.00),
    (26, N'Skiudstyr, hjelm', 275.00),
    (27, N'Værktøjskasse', 360.00),
    (28, N'Tøjstativ', 190.00),
    (29, N'Spilkonsol med controller', 1950.00),
    (30, N'Porcelænsfigur', 55.00);

DECLARE @TenantSeq INT = 1;
DECLARE @TenantID INT;
DECLARE @TenantName NVARCHAR(100);
DECLARE @RentalCount INT;
DECLARE @LocalRental INT;
DECLARE @RentalOrdinal INT = 0;
DECLARE @ShelfKind CHAR(1);
DECLARE @ShelfSlot INT;
DECLARE @ShelfID INT;
DECLARE @RentalID INT;
DECLARE @RentalStart DATE;
DECLARE @RentalEnd DATE;
DECLARE @MonthlyRent DECIMAL(10, 2);
DECLARE @IsCustomPrice BIT;
DECLARE @PaymentMethod NVARCHAR(50);
DECLARE @ItemCount INT;
DECLARE @ItemIndex INT;
DECLARE @CatalogSeq INT;
DECLARE @Description NVARCHAR(255);
DECLARE @ItemPrice DECIMAL(10, 2);
DECLARE @ItemID INT;
DECLARE @Barcode NVARCHAR(50);
DECLARE @LastSaleDay DATE;
DECLARE @SaleDate DATE;
DECLARE @SalePrice DECIMAL(10, 2);
DECLARE @AvailableDays INT;
DECLARE @CreatedRentals INT = 0;
DECLARE @CreatedItems INT = 0;
DECLARE @CreatedSales INT = 0;

BEGIN TRY
    BEGIN TRANSACTION;

    WHILE @TenantSeq <= 24
    BEGIN
        SELECT @TenantName = Name FROM @Tenants WHERE Seq = @TenantSeq;

        -- Some tenants have finished renting; others have current rentals.
        SET @RentalEnd = CASE
            WHEN @TenantSeq % 9 = 0 THEN DATEADD(DAY, -1, DATEADD(MONTH, -1, @CurrentMonthStart))
            WHEN @TenantSeq % 8 = 0 THEN @LastCompletedDay
            ELSE NULL
        END;

        INSERT INTO dbo.TENANT
            (Name, Phone, Email, BankRegistrationNumber, BankAccountNumber, IsActive)
        VALUES
            (@TenantName,
             N'20' + RIGHT(N'000000' + CONVERT(NVARCHAR(6), @TenantSeq), 6),
             N'demo' + RIGHT(N'00' + CONVERT(NVARCHAR(2), @TenantSeq), 2)
                + N'@reolmarkedet-demo.invalid',
             N'0000',
             RIGHT(N'0000000000' + CONVERT(NVARCHAR(10), @TenantSeq), 10),
             CASE WHEN @RentalEnd IS NULL THEN 1 ELSE 0 END);
        SET @TenantID = CONVERT(INT, SCOPE_IDENTITY());

        SET @RentalCount = CASE
            WHEN @TenantSeq = 1 THEN 3
            WHEN @TenantSeq % 3 = 0 THEN 2
            ELSE 1
        END;
        SET @LocalRental = 1;

        WHILE @LocalRental <= @RentalCount
        BEGIN
            SET @RentalOrdinal += 1;
            SET @ShelfKind = CASE WHEN @RentalOrdinal % 4 = 0 THEN 'R' ELSE 'S' END;
            SET @ShelfSlot = CASE
                WHEN @ShelfKind = 'R' THEN @RentalOrdinal / 4
                ELSE @RentalOrdinal - @RentalOrdinal / 4
            END;
            SET @ShelfID = NULL;
            SELECT @ShelfID = ShelfID
            FROM @FreeShelves
            WHERE ShelfKind = @ShelfKind AND Slot = @ShelfSlot;

            SET @RentalStart = DATEADD(
                DAY,
                1 + ((@TenantSeq * 3 + @LocalRental * 2) % 18),
                DATEADD(MONTH, -6 + (@TenantSeq % 6), @CurrentMonthStart)
            );
            SET @IsCustomPrice = CASE WHEN @TenantSeq = 5 THEN 1 ELSE 0 END;
            SET @MonthlyRent = CASE
                WHEN @IsCustomPrice = 1 THEN 700.00
                WHEN @LocalRental = 1 THEN 850.00
                WHEN @LocalRental <= 3 THEN 825.00
                ELSE 800.00
            END;
            SET @PaymentMethod = CASE @TenantSeq % 3
                WHEN 0 THEN N'Cash'
                WHEN 1 THEN N'Card'
                ELSE N'MobilePay'
            END;

            IF @ShelfID IS NULL OR (@RentalEnd IS NOT NULL AND @RentalEnd <= @RentalStart)
            BEGIN
                THROW 51001, N'Invalid demo rental dates or shelf assignment.', 1;
            END;

            INSERT INTO dbo.RENTAL
                (StartDate, EndDate, MonthlyRent, IsCustomPrice,
                 InitialPaymentMethod, TenantID, ShelfID)
            VALUES
                (@RentalStart, @RentalEnd, @MonthlyRent, @IsCustomPrice,
                 @PaymentMethod, @TenantID, @ShelfID);
            SET @RentalID = CONVERT(INT, SCOPE_IDENTITY());
            SET @CreatedRentals += 1;

            SET @ItemCount = 20 + (@RentalOrdinal % 11);
            SET @ItemIndex = 1;
            SET @LastSaleDay = CASE
                WHEN @RentalEnd IS NOT NULL AND @RentalEnd < @LastCompletedDay
                    THEN @RentalEnd
                ELSE @LastCompletedDay
            END;
            SET @AvailableDays = DATEDIFF(DAY, @RentalStart, @LastSaleDay);

            IF @AvailableDays < 0
            BEGIN
                THROW 51002, N'A demo rental has no completed sale days.', 1;
            END;

            WHILE @ItemIndex <= @ItemCount
            BEGIN
                SET @CatalogSeq = 1 + ((@RentalOrdinal * 7 + @ItemIndex * 11) % 30);
                SELECT @Description = Description, @ItemPrice = Price
                FROM @Catalog WHERE Seq = @CatalogSeq;
                SET @Barcode = N'DEM-' + CONVERT(NVARCHAR(8), @CurrentMonthStart, 112)
                    + N'-' + RIGHT(N'000' + CONVERT(NVARCHAR(3), @RentalOrdinal), 3)
                    + N'-' + RIGHT(N'00' + CONVERT(NVARCHAR(2), @ItemIndex), 2);

                INSERT INTO dbo.ITEM (Description, Price, Barcode, RentalID)
                VALUES (@Description, @ItemPrice, @Barcode, @RentalID);
                SET @ItemID = CONVERT(INT, SCOPE_IDENTITY());
                SET @CreatedItems += 1;

                -- Keep some current stock unsold. Finished rentals have no remaining stock.
                -- Tenant 23 deliberately has no sales, so a zero-sales settlement can be seen.
                IF @TenantSeq <> 23
                   AND (@RentalEnd IS NOT NULL OR @ItemIndex % 4 <> 0)
                BEGIN
                    SET @SaleDate = CASE
                        WHEN @RentalEnd IS NULL AND @ItemIndex <= 2 THEN @Today
                        ELSE DATEADD(
                            DAY,
                            (@ItemIndex * 29 + @RentalOrdinal * 17) % (@AvailableDays + 1),
                            @RentalStart
                        )
                    END;
                    SET @SalePrice = CASE
                        WHEN @ItemIndex % 9 = 0
                            THEN CONVERT(DECIMAL(10, 2), ROUND(@ItemPrice * 0.90, 2))
                        ELSE @ItemPrice
                    END;

                    INSERT INTO dbo.SALE (SaleDate, SalePrice, Notes, PaymentMethod, ItemID)
                    VALUES
                        (@SaleDate, @SalePrice,
                         CASE
                             WHEN @ItemIndex % 9 = 0 THEN N'Aftalt pris'
                             WHEN @ItemIndex % 13 = 0 THEN N'Original emballage'
                             ELSE NULL
                         END,
                         CASE (@TenantSeq + @ItemIndex) % 3
                             WHEN 0 THEN N'Cash'
                             WHEN 1 THEN N'Card'
                             ELSE N'MobilePay'
                         END,
                         @ItemID);
                    SET @CreatedSales += 1;
                END;

                SET @ItemIndex += 1;
            END;

            SET @LocalRental += 1;
        END;

        SET @TenantSeq += 1;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    24 AS DemoTenants,
    @CreatedRentals AS DemoRentals,
    @CreatedItems AS DemoItems,
    @CreatedSales AS DemoSales,
    @CreatedItems - @CreatedSales AS UnsoldItems,
    DATEADD(MONTH, -6, @CurrentMonthStart) AS EarliestStartMonth,
    @Today AS LatestSaleDate;
