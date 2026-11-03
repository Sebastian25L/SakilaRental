/* =====================================================================
   03_vista_disponibilidad.sql
   Vista con el estado de cada copia: una copia esta DISPONIBLE si no tiene
   ningun alquiler abierto (return_date IS NULL).
   ===================================================================== */
USE sakila;
GO

CREATE OR ALTER VIEW dbo.vw_inventario_estado AS
SELECT i.inventory_id,
       i.film_id,
       i.store_id,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.rental r
                         WHERE r.inventory_id = i.inventory_id
                           AND r.return_date IS NULL)
            THEN 0 ELSE 1 END AS disponible
FROM dbo.inventory i;
GO

SELECT TOP 10 * FROM dbo.vw_inventario_estado ORDER BY inventory_id;
GO
