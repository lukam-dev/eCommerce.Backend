SET XACT_ABORT ON;

INSERT dbo.Products (Id, Name, Currency, UnitPriceMinor)
SELECT s.Id, s.Name, 'EUR', s.Price
FROM (VALUES
    (N'keyboard', N'Mechanical keyboard', CAST(7990 AS bigint)),
    (N'mouse', N'Wireless mouse', CAST(2490 AS bigint)),
    (N'monitor', N'27-inch monitor', CAST(24900 AS bigint))
) s(Id, Name, Price)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Products p WHERE p.Id = s.Id);

DECLARE @demo uniqueidentifier = '11111111-1111-1111-1111-111111111111';
IF NOT EXISTS (SELECT 1 FROM dbo.Carts WHERE Id = @demo)
BEGIN
    INSERT dbo.Carts (Id, OwnerId, Currency, Version, ExpiresAt)
        VALUES (@demo, N'demo', 'EUR', 1, DATEADD(day, 30, SYSDATETIMEOFFSET()));
    INSERT dbo.CartItems (CartId, ProductId, Quantity, UnitPriceMinor)
        VALUES (@demo, N'keyboard', 1, 7990), (@demo, N'mouse', 2, 2490);
END;

