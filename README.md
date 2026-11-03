# Sakila Rental – Mini sistema de renta de películas

Proyecto de Desarrollo Web (ESPE Santo Domingo). Aplicación **ASP.NET Core MVC + ADO.NET + SQL Server** sobre la base de datos **Sakila**.

## Funcionalidades

| Rol | Puede |
|---|---|
| **Cliente** | Ver catálogo y detalle, alquilar, ver *Mis alquileres*, devolver, ver reporte |
| **Administrador** | CRUD de películas (baja lógica y restauración), gestionar copias del inventario, ver todos los alquileres y registrar devoluciones, ver reporte |

### Reglas de negocio
- **Disponibilidad:** una copia está disponible si no tiene un alquiler abierto (`return_date IS NULL`). Solo se alquila si existe una copia libre (asignada dentro de una transacción con `UPDLOCK, HOLDLOCK`).
- **Película activa:** no se alquilan ni se agregan copias a películas dadas de baja.
- **Sin duplicados:** un cliente no puede tener dos alquileres abiertos de la misma película.
- **Propiedad:** un cliente solo puede devolver sus propios alquileres. El cliente se obtiene de **Session**, nunca de un formulario.
- **Estado:** solo se devuelve un alquiler activo; la devolución registra `return_date` y **no borra** el historial.
- **Baja lógica (Reto 2):** `film.activo = 0`. No se permite dar de baja una película con alquileres sin devolver. Una copia con historial no se puede eliminar.
- **Autorización:** filtro global de sesión + atributo `[RolRequerido]` por rol.
- **Contraseñas (Reto 1):** PBKDF2‑SHA256, salt aleatorio de 16 bytes, 100 000 iteraciones.
- **Validación:** DataAnnotations + `ModelState.IsValid`, consultas parametrizadas, tokens antiforgery.

## Requisitos
- .NET 10 SDK
- SQL Server / LocalDB con la base **sakila** cargada
- SQL Server Management Studio (para ejecutar los scripts)

## Puesta en marcha
1. Ejecuta en SSMS, **en orden**, los scripts de la carpeta `Database/`:
   1. `01_usuarios.sql` – tabla `Usuarios` y usuarios de prueba
   2. `02_eliminacion_logica.sql` – columna `film.activo` e índice
   3. `03_vista_disponibilidad.sql` – vista `vw_inventario_estado`
   4. `04_consultas_exploratorias.sql` – consultas de exploración (opcional)
2. Copia `SakilaWebMVC/appsettings.json` a `SakilaWebMVC/appsettings.Development.json` (archivo ignorado por Git) y escribe tu cadena de conexión real:
   ```json
   "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=sakila;User Id=sa;Password=TU_CONTRASEÑA;TrustServerCertificate=True;"
   ```
3. Ejecuta:
   ```
   cd SakilaWebMVC
   dotnet run
   ```
   o abre `SakilaWeb.slnx` en Visual Studio y presiona F5.

## Usuarios de prueba
| Usuario | Contraseña | Rol |
|---|---|---|
| `admin` | `Admin123*` | Administrador |
| `cliente1`, `cliente2`, `cliente3` | `Cliente123*` | Cliente (ligados a los 3 primeros `customer`) |

## Estructura
```
Database/            scripts SQL
SakilaWebMVC/
  Controllers/       Acceso, Home, Peliculas, Inventario, Alquileres, Reportes (ADO.NET dentro de cada controlador, como en clase)
  Filters/           SesionRequeridaFilter (global), RolRequeridoAttribute
  Helpers/           PasswordHasher, Roles, SesionExtensions
  Models/            modelos y ViewModels
  Views/             Razor + Partial Views (_Layout, _Mensajes, _Disponibilidad, _Paginacion, _PeliculaFormulario, _UsuarioNav)
```
