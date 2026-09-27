USE ReolmarkedetDB;
GO

ALTER TABLE dbo.TENANT
ADD BankRegistrationNumber NVARCHAR(20) NULL,
    BankAccountNumber NVARCHAR(20) NULL;