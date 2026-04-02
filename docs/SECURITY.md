# Seguridad y configuración

## Gestión de secretos

La aplicación no hardcodea tokens ni credenciales. La configuración se guarda cifrada localmente usando `Windows DPAPI` a través de:

- `DpapiSecretProtector`
- `EncryptedJsonConfigurationStore`

El fichero por defecto se almacena en:

`%LocalAppData%\PrivilegedAuditSuite\appsettings.secure`

## Buenas prácticas implementadas

- credenciales fuera del código
- cifrado local ligado al contexto Windows
- separación entre autenticación y consumo de API
- logs sin necesidad de exponer secretos
- uso de `async/await` para evitar congelación de la UI

## Recomendaciones operativas

- usar una cuenta de servicio dedicada para PVWA y Graph
- limitar permisos en Entra a los scopes estrictamente necesarios
- proteger el acceso al perfil Windows donde se almacena el fichero cifrado
- no reutilizar exports con datos sensibles fuera del entorno autorizado
- revisar el contenido de logs antes de compartirlos externamente

## Consideraciones sobre errores

Los errores CPM se normalizan priorizando códigos técnicos:

- `winRc=`
- `RC=`
- `HRESULT`
- `NTSTATUS`
- `Error code`
- `ORA-`
- `SQLxxxx`

Si no existe un código, el sistema genera una firma común saneada para evitar que nombres de cuenta, hosts o rutas rompan la agrupación.

