# Uso funcional

## CyberArk Audit

### Flujo recomendado

1. `Load Accounts File` para un `CSV/XLSX` manual o `Sync From API`.
2. Seleccionar plataformas y safes.
3. Opcionalmente añadir exclusiones temporales de servidores en `Session Excluded Servers` pegando una columna o importando un `csv/txt`.
4. Revisar `Errors In Final Output`.
5. Ajustar qué errores se exportan y su orden con `Up` y `Down`.
6. Pulsar `Run Audit`.
7. Pulsar `Export Error Summary`.

### Exclusión de servidores

- `Persistent Excluded Servers` en `Settings & Logs` guarda hosts excluidos en la configuración segura y aplica también a `headless`.
- `Session Excluded Servers` en `CyberArk Audit` permite añadir una sobreescritura puntual para la sesión actual.
- `txt`: una línea por servidor.
- `csv`: se toma la primera columna no vacía de cada fila y se ignoran cabeceras comunes como `Server` o `Address`.
- El filtrado compara el valor exacto de `Address` tras normalizar espacios y sin distinguir mayúsculas/minúsculas.

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
