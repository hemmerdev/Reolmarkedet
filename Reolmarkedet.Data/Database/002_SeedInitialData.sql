USE ReolmarkedetDB
GO

IF NOT EXISTS (
	SELECT 1 
	FROM dbo.SHELFTYPE 
	WHERE ShelfTypeName = N'6 hylder')
BEGIN
	INSERT INTO dbo.SHELFTYPE (ShelfTypeName)
	VALUES (N'6 hylder');
END;

IF NOT EXISTS (
	SELECT 1 
	FROM dbo.SHELFTYPE 
	WHERE ShelfTypeName = N'3 hylder og bøjlestang')
BEGIN
	INSERT INTO dbo.SHELFTYPE (ShelfTypeName)
	VALUES (N'3 hylder og bøjlestang');
END;

DECLARE @SixShelvesTypeID INT;
DECLARE @ClothesRailTypeID INT;

SELECT @SixShelvesTypeID = ShelfTypeID 
FROM dbo.SHELFTYPE 
WHERE ShelfTypeName = N'6 hylder';

SELECT @ClothesRailTypeID = ShelfTypeID
FROM dbo.SHELFTYPE 
WHERE ShelfTypeName = N'3 hylder og bøjlestang';

IF @SixShelvesTypeID IS NULL OR @ClothesRailTypeID IS NULL
BEGIN
	PRINT N'De nødvendige hyldetyper findes ikke.';
	RETURN;
END;

DECLARE @ShelfNumber INT = 1;
DECLARE @ShelfTypeID INT;

WHILE @ShelfNumber <= 80
BEGIN
	IF @ShelfNumber <= 60
		SET @ShelfTypeID = @SixShelvesTypeID;
	ELSE
		SET @ShelfTypeID = @ClothesRailTypeID;


	IF NOT EXISTS (
		SELECT 1 
		FROM dbo.SHELF 
		WHERE ShelfNumber = @ShelfNumber)
	BEGIN
		INSERT INTO dbo.SHELF (ShelfNumber, IsActive, ShelfTypeID)
		VALUES (@ShelfNumber, 1, @ShelfTypeID);
	END;

	SET @ShelfNumber = @ShelfNumber + 1;
END;

