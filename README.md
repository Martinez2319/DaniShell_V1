# DaniShell

Terminal para Windows en C# y .NET 8, con comandos de archivos y texto, utilidades de red, temas, historial, alias y plugins.

## Compilar y ejecutar

Necesitas el **SDK de .NET 8**. Desde la raíz:

```powershell
dotnet build MyShell.sln -c Release
dotnet run --project DaniShell/DaniShell.csproj -c Release
```

La solución de la raíz incluye aplicación y pruebas. La solución DaniShell/DaniShell.sln incluye solo la aplicación.

## Pruebas

```powershell
dotnet run --project Tests/DaniShell.RegressionTests.csproj -c Release
```

Hay 22 comprobaciones sin servicios de red: comillas, tuberías, filtros, conteos, recursos y subprocesos reales con entradas y salidas grandes. Un fallo devuelve un código distinto de cero. GitHub Actions compila, prueba y publica un artefacto portable.

## Publicar para Windows x64

```powershell
dotnet publish DaniShell/DaniShell.csproj -c Release -r win-x64 --self-contained true -o artifacts/portable
```

Distribuye **toda la carpeta** artifacts/portable: contiene DaniShell.exe, commands_cmd.json y banners/. La publicación portable incluye el runtime; la compilación normal necesita el runtime de .NET 8.

## Ejemplos

```text
cat "datos con espacios.txt" | grep error | sort | uniq
echo -e "uno\ndos\ntres" | head -n 2
echo "texto" | tee salida.txt | wc -w
echo "a|b"
awk '{print $2}' datos.txt
sed 's/antes/despues/' datos.txt
```

Las tuberías son de **texto y se almacenan en memoria entre etapas**. Los filtros cat, grep, head, tail, wc, sort, uniq, awk y sed reciben la salida anterior cuando no se indica un archivo. Un argumento `-` representa la entrada estándar. tee guarda y reenvía esa entrada. Los comandos externos también pueden recibirla.

Un carácter `|` entre comillas es texto. Las comillas sin cerrar y las etapas vacías se rechazan. No se implementa toda la sintaxis de Bash o PowerShell: `||`, redirecciones de shell y xargs no forman parte del intérprete de tuberías. awk y sed implementan un subconjunto de sus equivalentes Unix.

wc -l cuenta saltos LF; una última línea sin salto final no aumenta ese contador. wc -w cuenta palabras separadas por espacios en blanco. wc -c cuenta bytes del archivo; para entrada estándar de texto usa UTF-8.

SSH hereda la consola para autenticación y sesiones interactivas; úsalo fuera de una tubería. ssh y nmap necesitan sus ejecutables en PATH. Nmap y los procesos externos drenan stdout y stderr en paralelo.

## Estructura

| Ruta | Contenido |
| --- | --- |
| DaniShell/Core/ | Intérprete, entrada, configuración, temas y procesos. |
| DaniShell/Commands/ | Comandos internos, filtros y red. |
| DaniShell/Plugins/ | Cargador de plugins. |
| DaniShell/banners/ | Recursos copiados al compilar y publicar. |
| Tests/ | Suite de regresión. |
| .github/workflows/ci.yml | Compilación, pruebas y paquete portable. |

Configuración e historial: `%USERPROFILE%\.danishell\`. Los plugins deben referenciar el ensamblado de DaniShell e implementar DaniShell.Commands.ICommand; copiar una interfaz del mismo nombre en otro ensamblado no es suficiente.

## Estado

Esta es la base principal del proyecto. bin/, obj/ y las distribuciones generadas quedan fuera del seguimiento de Git. La limpieza conserva el historial. El código FTP todavía usa APIs obsoletas de .NET y no se prueba contra servidores reales en esta suite.

No se incluye un archivo LICENSE; las condiciones de distribución deben definirse explícitamente.
