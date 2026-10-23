# DaniShell 4.1.0

- Sesiones nativas CMD y PowerShell con prompt de DaniShell, accesos de inicio y ejecución por CLI.
- Tuberías de texto concurrentes entre procesos; filtros incrementales y ordenación externa por bloques.
- Códigos de salida, `&&`, `||`, `<`, `>`, `>>`, `2>` y `2>&1` en el modo propio.
- Cancelación de tuberías y cierre anticipado de productores cuando el consumidor termina.
- Búsqueda inversa del historial con Ctrl+R, deduplicación y exclusión de comandos que empiezan con espacio.
- Autocompletado contextual y cíclico con Tab/Shift+Tab, opciones y rutas con espacios.
- Contrato de plugins con manifiesto de API, versión mínima, streaming y plantilla compilable.
- Releases automáticas desde etiquetas v*, después de compilación, pruebas y comprobación del paquete portable.

Las sesiones nativas usan los motores instalados en Windows. El modo propio de DaniShell tiene sintaxis y filtros de texto propios; no sustituye el lenguaje completo de PowerShell ni transmite objetos entre sus filtros.
