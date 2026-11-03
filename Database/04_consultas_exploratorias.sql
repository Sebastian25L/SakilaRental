/* =====================================================================
   04_consultas_exploratorias.sql
   Sentencias SQL para la exploracion del informe (se piden 3 de 10).
   Ejecutar cada una por separado y capturar el resultado en SSMS.
   ===================================================================== */
USE sakila;
GO

/* 1. Peliculas con su idioma y precio de renta */
SELECT TOP 20 f.film_id, f.title, l.name AS idioma, f.rental_rate, f.rating
FROM film f
INNER JOIN language l ON l.language_id = f.language_id
ORDER BY f.title;

/* 2. Copias fisicas por pelicula */
SELECT TOP 20 f.title, COUNT(i.inventory_id) AS copias
FROM film f
INNER JOIN inventory i ON i.film_id = f.film_id
GROUP BY f.title
ORDER BY copias DESC, f.title;

/* 3. Alquileres abiertos (sin devolver) con cliente y pelicula */
SELECT r.rental_id, c.first_name + ' ' + c.last_name AS cliente, f.title, r.rental_date
FROM rental r
INNER JOIN customer  c ON c.customer_id  = r.customer_id
INNER JOIN inventory i ON i.inventory_id = r.inventory_id
INNER JOIN film      f ON f.film_id      = i.film_id
WHERE r.return_date IS NULL
ORDER BY r.rental_date DESC;

/* 4. Alquileres por cliente (top 10) */
SELECT TOP 10 c.customer_id, c.first_name, c.last_name, COUNT(*) AS total
FROM rental r
INNER JOIN customer c ON c.customer_id = r.customer_id
GROUP BY c.customer_id, c.first_name, c.last_name
ORDER BY total DESC;

/* 5. Peliculas por clasificacion */
SELECT rating, COUNT(*) AS total FROM film GROUP BY rating ORDER BY total DESC;

/* 6. Ingresos por tienda */
SELECT s.store_id, SUM(p.amount) AS ingresos
FROM payment p
INNER JOIN staff s ON s.staff_id = p.staff_id
GROUP BY s.store_id;

/* 7. Top 10 de peliculas mas alquiladas (base del reporte) */
SELECT TOP 10 f.film_id, f.title, COUNT(r.rental_id) AS veces_alquilada
FROM film f
INNER JOIN inventory i ON i.film_id = f.film_id
INNER JOIN rental    r ON r.inventory_id = i.inventory_id
GROUP BY f.film_id, f.title
ORDER BY veces_alquilada DESC, f.title;

/* 8. Copias disponibles vs. alquiladas */
SELECT disponible, COUNT(*) AS copias FROM vw_inventario_estado GROUP BY disponible;

/* 9. Peliculas sin ninguna copia en inventario */
SELECT f.film_id, f.title
FROM film f
LEFT JOIN inventory i ON i.film_id = f.film_id
WHERE i.inventory_id IS NULL;

/* 10. Usuarios del sistema y su cliente asociado */
SELECT u.usuario_id, u.nombre_usuario, u.rol, c.first_name, c.last_name
FROM Usuarios u
LEFT JOIN customer c ON c.customer_id = u.customer_id;
GO
