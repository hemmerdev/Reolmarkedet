USE ReolmarkedetDB;
GO

ALTER TABLE dbo.SHELF
ADD RowLabel NVARCHAR(20) NULL,
    PositionInRow INT NULL;
GO

CREATE UNIQUE INDEX UX_SHELF_Location
ON dbo.SHELF (RowLabel, PositionInRow)
WHERE RowLabel IS NOT NULL AND PositionInRow IS NOT NULL;