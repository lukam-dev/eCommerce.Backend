SET XACT_ABORT ON;
-- OIDC subjects are case-sensitive identifiers. Preserve that semantic on SQL Server.
DROP INDEX IX_Carts_OwnerId ON dbo.Carts;
ALTER TABLE dbo.Carts ALTER COLUMN OwnerId nvarchar(200) COLLATE Latin1_General_100_BIN2 NOT NULL;
CREATE INDEX IX_Carts_OwnerId ON dbo.Carts(OwnerId);

