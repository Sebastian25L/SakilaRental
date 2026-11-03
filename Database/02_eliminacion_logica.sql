USE sakila;
GO

IF COL_LENGTH('dbo.film', 'activo') IS NULL
    ALTER TABLE dbo.film ADD activo BIT NOT NULL CONSTRAINT DF_film_activo DEFAULT 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_rental_inventory_abierto')
    CREATE INDEX IX_rental_inventory_abierto
    ON dbo.rental (inventory_id) INCLUDE (customer_id, return_date)
    WHERE return_date IS NULL;
GO

SELECT COUNT(*) AS peliculas, SUM(CAST(activo AS INT)) AS activas FROM dbo.film;
GO
