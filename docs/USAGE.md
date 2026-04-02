# Uso funcional

## CyberArk Audit

### Flujo recomendado

1. `Load Accounts File` para un `CSV/XLSX` manual o `Sync From API`.
2. Seleccionar plataformas y safes.
3. Revisar `Errors In Final Output`.
4. Ajustar qué errores se exportan y su orden con `Up` y `Down`.
5. Pulsar `Run Audit`.
6. Pulsar `Export Error Summary`.

### Formato de salida

El resumen exportado genera:

- cabecera `Error`
- cabecera `Num Errores`
- una fila por error/código/firma seleccionada
- una fila vacía
- `Total`
- valor total en la celda inferior

## Identity Reconciliation

### Fuentes soportadas

- Entra ID por `CSV/XLSX`
- CyberArk Identity por `CSV/XLSX`
- Entra ID por API
- fallback al conjunto cargado en `CyberArk Audit` si no existe fichero manual CyberArk Identity

### Resultado

La vista muestra:

- usuarios Entra
- usuarios CyberArk
- discrepancias y cuentas huérfanas

## Settings & Logs

Desde esta pestaña se gestionan:

- conexión CyberArk
- credenciales Entra
- destino futuro OneDrive/Excel
- configuración cifrada
- logs en tiempo real
