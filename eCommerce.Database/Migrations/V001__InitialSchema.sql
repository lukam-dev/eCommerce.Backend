SET XACT_ABORT ON;

CREATE TABLE dbo.Products (
    Id nvarchar(100) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    Name nvarchar(200) NOT NULL,
    Currency char(3) NOT NULL,
    UnitPriceMinor bigint NOT NULL,
    CONSTRAINT CK_Products_Price CHECK (UnitPriceMinor >= 0)
);

CREATE TABLE dbo.Carts (
    Id uniqueidentifier NOT NULL CONSTRAINT DF_Carts_Id DEFAULT NEWSEQUENTIALID() CONSTRAINT PK_Carts PRIMARY KEY,
    OwnerId nvarchar(200) NOT NULL,
    Currency char(3) NOT NULL CONSTRAINT DF_Carts_Currency DEFAULT 'EUR',
    Version int NOT NULL CONSTRAINT DF_Carts_Version DEFAULT 1,
    ExpiresAt datetimeoffset NOT NULL CONSTRAINT DF_Carts_ExpiresAt DEFAULT DATEADD(day, 30, SYSDATETIMEOFFSET()),
    CONSTRAINT CK_Carts_Version CHECK (Version > 0)
);

CREATE TABLE dbo.CartItems (
    CartId uniqueidentifier NOT NULL,
    ProductId nvarchar(100) NOT NULL,
    Quantity int NOT NULL,
    UnitPriceMinor bigint NOT NULL,
    CONSTRAINT PK_CartItems PRIMARY KEY (CartId, ProductId),
    CONSTRAINT FK_CartItems_Carts FOREIGN KEY (CartId) REFERENCES dbo.Carts(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CartItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id),
    CONSTRAINT CK_CartItems_Quantity CHECK (Quantity BETWEEN 1 AND 99),
    CONSTRAINT CK_CartItems_Price CHECK (UnitPriceMinor >= 0)
);

CREATE INDEX IX_Carts_OwnerId ON dbo.Carts(OwnerId);
CREATE INDEX IX_CartItems_ProductId ON dbo.CartItems(ProductId);

