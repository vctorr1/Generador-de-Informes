# Arquitectura

## Principios

- `MVVM` limpio en la capa WPF.
- Lógica de negocio fuera de la vista.
- Servicios para integraciones externas.
- Separación explícita entre dominio, aplicación, infraestructura y presentación.

## Capas

## Domain

Contiene los modelos nucleares:

- `CyberArkAccount`
- `EntraUser`
- `AppConfiguration`
- modelos de errores, progreso y conciliación

No depende de infraestructura ni UI.

## Application

Contiene:

- interfaces de servicios
- reglas de negocio
- clasificación de errores
- filtrado de cuentas
- conciliación de identidades

Es la capa que define el comportamiento funcional reusable.

## Infrastructure

Implementa:

- `CyberArkAuthenticationService`
- `CyberArkApiService`
- `EntraIdGraphService`
- `ManualReportImportService`
- `ErrorSummaryExportService`
- `DpapiSecretProtector`
- `EncryptedJsonConfigurationStore`

Aquí viven las llamadas HTTP, acceso a Excel/CSV y persistencia segura.

## App

Interfaz `WPF` con:

- `MainWindow`
- `MainViewModel`
- `CyberArkAuditViewModel`
- `IdentityReconciliationViewModel`
- `SettingsViewModel`

La UI usa comandos asíncronos y bindings para evitar bloqueo del hilo principal.

## Headless

Proyecto de consola para ejecución por programador de tareas o pipelines operativos.

## Flujo de auditoría CyberArk

1. Cargar cuentas por archivo o PVWA API.
2. Poblar filtros de plataformas y safes.
3. Normalizar y clasificar errores CPM.
4. Permitir selección/orden de errores a exportar.
5. Generar resumen final y exportarlo.

## Flujo de conciliación

1. Cargar usuarios Entra y CyberArk Identity / Audit.
2. Aplicar reglas de mapeo de grupos.
3. Detectar huérfanos y discrepancias.
4. Mostrar y registrar resultados.

