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
DECLARE @RowLabel NVARCHAR(20);
DECLARE @PositionInRow INT;

WHILE @ShelfNumber <= 80
BEGIN
	IF @ShelfNumber <= 60
		SET @ShelfTypeID = @SixShelvesTypeID;
	ELSE
		SET @ShelfTypeID = @ClothesRailTypeID;

IF @ShelfNumber <= 13
BEGIN 
	SET @RowLabel = N'A';
	SET @PositionInRow = @ShelfNumber;
END;
ELSE IF @ShelfNumber <= 18
BEGIN 
	SET @RowLabel = N'B';
	SET @PositionInRow = @ShelfNumber - 13;
END;
ELSE IF @ShelfNumber <= 21
BEGIN
	SET @RowLabel = N'C';
	SET @PositionInRow = @ShelfNumber - 18;
END;
ELSE IF @ShelfNumber <= 24
BEGIN
	SET @RowLabel = N'D';
	SET @PositionInRow = @ShelfNumber - 21;
END;
ELSE IF @ShelfNumber <= 31
BEGIN
	SET @RowLabel = N'E';
	SET @PositionInRow = @ShelfNumber - 24;
END;
ELSE IF @ShelfNumber <= 38
BEGIN
	SET @RowLabel = N'F';
	SET @PositionInRow = @ShelfNumber - 31;
END;
ELSE IF @ShelfNumber <= 45
BEGIN
	SET @RowLabel = N'G';
	SET @PositionInRow = @ShelfNumber - 38;
END;
ELSE IF @ShelfNumber <= 52
BEGIN
	SET @RowLabel = N'H';
	SET @PositionInRow = @ShelfNumber - 45;
END;
ELSE IF @ShelfNumber <= 59
BEGIN
	SET @RowLabel = N'I';
	SET @PositionInRow = @ShelfNumber - 52;
END;
ELSE IF @ShelfNumber <= 66
BEGIN
	SET @RowLabel = N'J';
	SET @PositionInRow = @ShelfNumber - 59;
END;
ELSE IF @ShelfNumber <= 71
BEGIN
	SET @RowLabel = N'K';
	SET @PositionInRow = @ShelfNumber - 66;
END;
ELSE IF @ShelfNumber <= 76
BEGIN
	SET @RowLabel = N'L';
	SET @PositionInRow = @ShelfNumber - 71;
END;
ELSE IF @ShelfNumber <= 78
BEGIN
	SET @RowLabel = N'M';
	SET @PositionInRow = @ShelfNumber - 76;
END;
ELSE IF @ShelfNumber <= 80
BEGIN
	SET @RowLabel = N'N';
	SET @PositionInRow = @ShelfNumber - 78;
END;

	IF NOT EXISTS (
		SELECT 1 
		FROM dbo.SHELF 
		WHERE ShelfNumber = @ShelfNumber)
	BEGIN
		INSERT INTO dbo.SHELF (ShelfNumber, IsActive, ShelfTypeID, RowLabel, PositionInRow)
		VALUES (@ShelfNumber, 1, @ShelfTypeID, @RowLabel, @PositionInRow);
	END;

	SET @ShelfNumber = @ShelfNumber + 1;
END;

