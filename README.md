# DaniShell 4.1.0

Terminal personalizada para Windows en C# y .NET 8. Combina una interfaz propia —temas, banners, alias, historial y plugins— con acceso a **CMD y PowerShell reales**.

[Descargar la versión portable](https://github.com/Martinez2319/DaniShell_V1/releases/latest) · [Cambios](CHANGELOG.md) · [Crear un plugin](Samples/HelloPlugin/README.md)

## Empezar

Extrae **todo el ZIP** en una carpeta. No necesitas instalar .NET para usar el paquete portable Windows x64.

| Abrir | Resultado |
| --- | --- |
| `DaniShell.exe` | Interfaz personalizada y comandos propios. |
| `DaniShell-CMD.cmd` | Sesión real de CMD con prompt DaniShell. |
| `DaniShell-PowerShell.cmd` | Sesión real de PowerShell con prompt DaniShell. |

Desde el modo propio, escribe `shell cmd` o `shell powershell`. Escribe `exit` para volver a DaniShell. Dentro de una sesión nativa se conservan variables y directorio entre comandos; sus cambios de estado pertenecen a esa sesión. PowerShell usa `pwsh.exe` si está en PATH y, en su defecto, Windows PowerShell.

```text
shell cmd
set NOMBRE=Daniel
echo %NOMBRE%
exit
shell powershell
$n = 41
[pscustomobject]@{Valor=$n+1} | Select-Object -ExpandProperty Valor
exit
```

También puedes ejecutar un script completo en su motor nativo desde DaniShell:

```text
cmd /c ver
ps '$n=41; $n+1'
```

Una línea que empieza con `cmd`, `ps`, `powershell` o `pwsh` entrega el resto a ese motor. Así PowerShell interpreta sus objetos, variables, comillas y tuberías. Para usar todo su lenguaje o sus scripts, entra en la sesión nativa. El modo propio utiliza filtros de texto y su propia sintaxis.

## Personalización y edición

- `help`, `theme`, `banner`, `prompt`, `config` y `alias` permiten explorar y personalizar la terminal.
- **Tab / Shift+Tab** recorren comandos, alias, plugins, ejecutables de PATH, opciones conocidas y rutas, incluyendo nombres con espacios.
- **Ctrl+R** busca en el historial; repetir Ctrl+R recorre coincidencias, Enter recupera la elegida y Esc cancela.
- El historial elimina duplicados anteriores. Un comando que empieza con un espacio no se guarda.
- Configuración, historial y plugins: `%USERPROFILE%\.danishell\`. `DANISHELL_HOME` permite elegir otra carpeta.

Estas funciones de edición corresponden al modo propio. Las sesiones nativas usan la edición e historial de su motor.

## Tuberías y archivos en el modo propio

```text
cat "datos con espacios.txt" | grep error | sort | uniq
echo -e "uno\ndos\ntres" | head -n 2
echo "texto" | tee salida.txt | wc -w
echo "a|b"
echo primero > "salida con espacios.txt"
echo segundo >> "salida con espacios.txt"
cat < "salida con espacios.txt" | wc -l
cat inexistente.txt 2> errores.txt || echo recuperado
cat inexistente.txt > resultado.txt 2>&1
status
```

Las etapas se ejecutan simultáneamente mediante flujos de texto entre procesos. Los filtros `cat`, `grep`, `head`, `tail`, `wc`, `sort`, `uniq`, `awk`, `sed` y `tee` leen incrementalmente; `sort` ordena por bloques y usa archivos temporales. `tail` retiene las últimas líneas solicitadas. Un argumento `-` representa la entrada estándar; los filtros la usan al omitir un archivo dentro de una tubería. Ctrl+C cancela la tubería y devuelve 130. Si un consumidor como `head` termina antes, se detiene su productor.

| Operador | Uso |
| --- | --- |
| `\|` | Enviar texto a la siguiente etapa. |
| `&&` / `\|\|` | Ejecutar según éxito / fallo del grupo anterior. |
| `<` | Leer de un archivo. |
| `>` / `>>` | Sustituir / añadir salida UTF-8. |
| `2>` / `2>>` | Sustituir / añadir errores. |
| `2>&1` | Enviar errores al destino actual de la salida. |

`status` muestra el código anterior; una tubería devuelve el de su última etapa. Los comandos externos propagan su código. Las comillas protegen operadores y espacios. Las comillas sin cerrar, etapas vacías y más de 32 etapas se rechazan. Las etapas tienen estado aislado: usa `cd`, cambios de configuración y sesiones interactivas fuera de tuberías.

Este intérprete no implementa toda la gramática de CMD, Bash o PowerShell, ni tuberías de objetos o datos binarios. `awk` y `sed` son subconjuntos de sus equivalentes Unix. Para scripts y sintaxis completa usa el motor nativo. `wc -l` cuenta saltos LF; `wc -w`, palabras separadas por espacios en blanco; `wc -c`, bytes del archivo o UTF-8 al leer texto de entrada estándar.

SSH hereda la consola para autenticación y sesiones interactivas; úsalo fuera de una tubería. `ssh` y `nmap` necesitan sus ejecutables en PATH.

## Uso desde otra terminal

```powershell
.\DaniShell.exe --version
.\DaniShell.exe -c 'echo -e "b\na" | sort'
.\DaniShell.exe --shell cmd
.\DaniShell.exe --shell powershell
.\DaniShell.exe --shell cmd -c 'exit 7'
.\DaniShell.exe --shell powershell -c '$n=41; $n+1'
```

`-c` ejecuta una línea y devuelve su código de salida. `--shell ... -c` ejecuta un script en el motor seleccionado; los scripts de PowerShell se ejecutan sin cargar el perfil. Sin argumentos y con entrada redirigida, DaniShell procesa líneas de comandos del modo propio.

## Compilar, probar y publicar

Necesitas el **SDK de .NET 8**. Desde la raíz:

```powershell
dotnet build MyShell.sln -c Release
dotnet run --project DaniShell/DaniShell.csproj -c Release
dotnet run --project Tests/DaniShell.RegressionTests.csproj -c Release
dotnet publish DaniShell/DaniShell.csproj -c Release -r win-x64 --self-contained true -o artifacts/portable
```

La solución raíz incluye aplicación y pruebas. Hay **41 pruebas de regresión** sin servicios de red: sintaxis, filtros, redirecciones, códigos, sesiones nativas con estado, tuberías grandes, cancelación, consumidores que terminan antes, historial, completado y plugins. Los fallos devuelven un código distinto de cero.

GitHub Actions compila, prueba y verifica el ejecutable portable y sus recursos. Una etiqueta `v*` que coincide con la versión del proyecto publica una Release con ZIP después de superar las comprobaciones. Distribuye toda la carpeta publicada: ejecutable, `commands_cmd.json`, `banners/` y lanzadores.

## Plugins y estructura

La [plantilla HelloPlugin](Samples/HelloPlugin/README.md) muestra la API 1. Cada DLL necesita su manifiesto `.plugin.json` con `apiVersion: 1`; `minimumVersion` permite declarar la versión mínima. Implementa `IStreamingCommand` para flujos y códigos de salida. Los plugins anteriores sin manifiesto necesitan adaptarse y recompilarse. Los plugins ejecutan código con los permisos del usuario.

| Ruta | Contenido |
| --- | --- |
| `DaniShell/Core/` | Intérprete, motores, tuberías, entrada, configuración y temas. |
| `DaniShell/Commands/` | Comandos internos, filtros y red. |
| `DaniShell/Plugins/` | Cargador y comprobación de compatibilidad. |
| `DaniShell/Launchers/` | Accesos para CMD y PowerShell. |
| `Samples/HelloPlugin/` | Plugin compilable y manifiesto. |
| `Tests/` | Suite de regresión. |
| `.github/workflows/ci.yml` | Verificación, paquete y releases. |

Esta es la base principal del proyecto. `bin/`, `obj/` y distribuciones generadas quedan fuera de Git. FTP conserva APIs obsoletas de .NET; las conexiones de red no se prueban contra servidores reales. No se incluye un archivo LICENSE; las condiciones de distribución deben definirse explícitamente.
