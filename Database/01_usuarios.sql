USE sakila;
GO

IF OBJECT_ID('dbo.Usuarios', 'U') IS NOT NULL
    DROP TABLE dbo.Usuarios;
GO

DECLARE @tipo NVARCHAR(30);
SELECT @tipo = DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'customer' AND COLUMN_NAME = 'customer_id';

DECLARE @sql NVARCHAR(MAX) = N'
CREATE TABLE dbo.Usuarios (
    usuario_id     INT IDENTITY(1,1) CONSTRAINT PK_Usuarios PRIMARY KEY,
    nombre_usuario NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Usuarios_nombre UNIQUE,
    password_hash  NVARCHAR(200) NOT NULL,
    rol            NVARCHAR(20)  NOT NULL
                   CONSTRAINT CK_Usuarios_rol CHECK (rol IN (N''Administrador'', N''Cliente'')),
    customer_id    ' + @tipo + N' NULL
                   CONSTRAINT FK_Usuarios_customer FOREIGN KEY REFERENCES dbo.customer(customer_id),
    activo         BIT NOT NULL CONSTRAINT DF_Usuarios_activo DEFAULT 1,
    fecha_registro DATETIME NOT NULL CONSTRAINT DF_Usuarios_fecha DEFAULT GETDATE()
);';
EXEC sp_executesql @sql;
GO

INSERT INTO dbo.Usuarios (nombre_usuario, password_hash, rol, customer_id)
VALUES (N'admin', N'PBKDF2-SHA256$100000$qQF213toGt9hK/6mIBHilg==$Zl8d5xPpfrwM/vq2e2CZ8krmCMkgpI9/itkv7ViI7HE=', N'Administrador', NULL);

;WITH c AS (
    SELECT TOP 3 customer_id, ROW_NUMBER() OVER (ORDER BY customer_id) AS n
    FROM dbo.customer ORDER BY customer_id
)
INSERT INTO dbo.Usuarios (nombre_usuario, password_hash, rol, customer_id)
SELECT 'cliente' + CAST(c.n AS NVARCHAR(5)),
       CASE c.n
            WHEN 1 THEN N'PBKDF2-SHA256$100000$UvBd4v4v0u5wkaWryDwIGg==$Uc8i2JiMvDZAVSdkg6QRxgM4F4ji3fnrItJgqjglpCs='
            WHEN 2 THEN N'PBKDF2-SHA256$100000$z7fIIG5PivXP2pzoVySU5Q==$/bY1fqAxTiPFr+k+S2n5KmIOwLz0vrnvElkAqN8w4y8='
            ELSE        N'PBKDF2-SHA256$100000$fAIIopjEKdQvna/tXqJopQ==$emfCGjkXasV69c3TkDk8k0BjayxHeJhoUy+KYwpV5VM=' END,
       N'Cliente', c.customer_id
FROM c;
GO

SELECT usuario_id, nombre_usuario, rol, customer_id, activo FROM dbo.Usuarios;
GO
