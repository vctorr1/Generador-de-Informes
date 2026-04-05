# PrivilegedAuditSuite

Aplicación nativa para Windows 10/11 construida con `C#`, `.NET 8` y `WPF` para auditoría de cuentas privilegiadas e identidades. La solución cubre auditoría CyberArk, conciliación de identidades con Entra ID, carga manual de informes `CSV/XLSX`, ejecución desatendida y almacenamiento seguro de configuración con `DPAPI`.

## Capacidades principales

- Auditoría de cuentas CyberArk por carga manual o sincronización vía PVWA API.
- Filtros combinados por `CPM Disabled`, plataformas y safes.
- Exclusión de servidores por `Address`, configurable de forma persistente y también temporal desde la pantalla de auditoría mediante texto o `csv/txt`.
- Clasificación y normalización de errores CPM con agrupación por códigos como `winRc=`, `Error code:`, `ORA-`, `HRESULT` y firma común saneada cuando no existe código.
- Exportación local de resumen de errores a `CSV/XLSX` con formato orientado a reporting:
  - columna `Error`
  - columna `Num Errores`
  - fila vacía
  - `Total`
  - suma final
- Selección y orden manual de categorías/firmas de error antes del export.
- Conciliación de identidades entre Entra ID y CyberArk Identity / CyberArk Audit.
- Modo `headless` para ejecuciones programadas.
- Configuración cifrada localmente con `Windows DPAPI`.

## Estructura de la solución

- `PrivilegedAuditSuite.App`
  Interfaz WPF con MVVM.
- `PrivilegedAuditSuite.Application`
  Servicios de negocio y contratos.
- `PrivilegedAuditSuite.Domain`
  Modelos y contratos del dominio.
- `PrivilegedAuditSuite.Infrastructure`
  Integraciones con PVWA, Graph, DPAPI, import/export.
- `PrivilegedAuditSuite.Headless`
  Runner de consola para automatización.
- `PrivilegedAuditSuite.Tests`
  Tests unitarios con xUnit.

## Requisitos

- Windows 10/11
- SDK `.NET 8`
- Acceso a CyberArk PVWA si se usa sincronización API
- Credenciales de aplicación para Entra ID si se usa Graph

## Ejecución local

```powershell
dotnet build .\PrivilegedAuditSuite.App\PrivilegedAuditSuite.App.csproj
dotnet run --project .\PrivilegedAuditSuite.App\PrivilegedAuditSuite.App.csproj
```

## Modo desatendido

```powershell
dotnet run --project .\PrivilegedAuditSuite.Headless\PrivilegedAuditSuite.Headless.csproj -- --task audit --config "C:\Ruta\appsettings.secure"
dotnet run --project .\PrivilegedAuditSuite.Headless\PrivilegedAuditSuite.Headless.csproj -- --task reconcile --config "C:\Ruta\appsettings.secure"
```

## Validación

```powershell
dotnet build .\PrivilegedAuditSuite.slnx --no-restore -m:1 -v minimal
dotnet test .\PrivilegedAuditSuite.Tests\PrivilegedAuditSuite.Tests.csproj --no-build --no-restore -v minimal
```

## Documentación

- [Arquitectura](docs/ARCHITECTURE.md)
- [Seguridad y configuración](docs/SECURITY.md)
- [Uso funcional](docs/USAGE.md)

## Estado actual

Implementado y validado:

- shell WPF con pestañas de auditoría, conciliación y configuración
- importación manual `CSV/XLSX`
- login y lectura de cuentas CyberArk por API
- conciliación LINQ
- export local de resumen de errores
- configuración cifrada
- tests unitarios clave

Pendiente para una siguiente iteración:

- escritura avanzada sobre Excel en OneDrive mediante Graph Workbook API
- sustitución de la integración actual de Entra REST por `Microsoft.Graph` SDK si se quiere una alineación estricta con ese requisito
