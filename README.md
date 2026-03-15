# 🚀 DaniShell v4.0

<div align=\"center\">

![GitHub stars](https://img.shields.io/github/stars/Martinez2319/DaniShell?style=social)
![GitHub forks](https://img.shields.io/github/forks/Martinez2319/DaniShell?style=social)
![GitHub contributors](https://img.shields.io/github/contributors/Martinez2319/DaniShell?style=flat-square&color=green)
![Último commit](https://img.shields.io/github/last-commit/Martinez2319/DaniShell?style=flat-square&color=blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple?style=flat-square)
![Platform](https://img.shields.io/badge/Plataforma-Windows-0078D6?style=flat-square)
![License](https://img.shields.io/github/license/Martinez2319/DaniShell?style=flat-square&color=brightgreen)

**La terminal que Windows siempre necesitó**  
*Comandos Linux nativos + compatibilidad total Windows + personalización cyber extrema*

[🔽 Descargar última versión](https://github.com/Martinez2319/DaniShell/releases) • [🐛 Reportar bug o sugerir feature](https://github.com/Martinez2319/DaniShell/issues)

</div>

---

## 🎯 ¿Qué es DaniShell?

**DaniShell** es una terminal avanzada 100% nativa para Windows que fusiona lo mejor de Linux y Windows **sin WSL ni dependencias externas**:

- ✅ +30 comandos Linux reales (`grep`, `awk`, `sed`, `find`, `cat`, `tail`...)
- ✅ Ejecuta cualquier comando CMD o PowerShell directamente
- ✅ **Portable total**: un solo .exe, llévalo en USB
- ✅ Temas cyberpunk / hacker personalizables
- ✅ Sistema de plugins en C#, aliases, historial inteligente y autocompletado Tab

> *\"La seguridad no es un producto, es un proceso.\"*

---

## ✨ Características clave

### 🐧 Comandos Linux integrados

```bash
grep -r \"error\" logs/              # Búsqueda recursiva hardcore
find . -name \"*.cs\" -type f        # Encuentra archivos C# rápido
tail -n 50 server.log              # Últimas 50 líneas
sed 's/old/new/g' config.txt       # Reemplazos masivos
awk '{print $1, $3}' data.csv      # Extrae columnas como pro
cat -n archivo.txt                 # Ver archivo con números de línea
head -20 README.md                 # Primeras 20 líneas
```

### 🎨 Temas cyber/hacker

| Tema | Estilo |
|------|--------|
| `hacker` | 💚 Verde Matrix legendario |
| `cyberpunk` | 💜 Neón rosa + morado |
| `ocean` | 💙 Profundo azul marino |
| `fire` | 🔥 Rojo/naranja intenso |
| `neon` | 🌈 Explosión de colores |
| `dark` | ⬛ Oscuro clásico |
| `light` | ⬜ Tema claro |

```bash
theme                    # Ver temas disponibles
theme hacker             # Activar modo Matrix
theme create mitema      # Crear tema personalizado
theme edit mitema        # Editar colores
```

### ⚡ Boost de productividad

- 📝 **Aliases personalizados**: `alias deploy=\"dotnet publish -c Release\"`
- 🔍 **Historial con búsqueda**: Flechas ↑↓ para navegar
- ⌨️ **Autocompletado Tab**: Archivos, comandos, paths
- 🔙 **cd -**: Volver al directorio anterior
- 🔌 **Plugins extensibles**: Crea comandos en .NET

### 🌐 Comandos de red avanzados

| Categoría | Comandos |
|-----------|----------|
| **Diagnóstico** | `ping`, `tracert`, `ipinfo`, `netstat`, `nmap`, `nslookup` |
| **HTTP** | `httpget`, `httphead`, `httppost`, `curl`, `wget` |
| **FTP** | `ftplist`, `ftpget`, `ftpput` |
| **SSH** | `ssh usuario@host` |

---

## 📦 Instalación

### Opción recomendada: Portable

1. Ve a → [**Releases**](https://github.com/Martinez2319/DaniShell/releases)
2. Descarga `DaniShell_Portable.zip`
3. Extrae y ejecuta `DaniShell.exe` → **¡Listo, sin instalar nada!**

### Desde código fuente

```bash
git clone https://github.com/Martinez2319/DaniShell.git
cd DaniShell
dotnet build -c Release
```

### Publicar ejecutable único (self-contained)

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## 🖼️ DaniShell en acción

```
╔════════════════════════════════════════════════════════════════════════╗
║     ██████╗  █████╗ ███╗   ██╗██╗███████╗██╗  ██╗███████╗██╗     ██╗   ║
║     ██╔══██╗██╔══██╗████╗  ██║██║██╔════╝██║  ██║██╔════╝██║     ██║   ║
║     ██║  ██║███████║██╔██╗ ██║██║███████╗███████║█████╗  ██║     ██║   ║
║     ██║  ██║██╔══██║██║╚██╗██║██║╚════██║██╔══██║██╔══╝  ██║     ██║   ║
║     ██████╔╝██║  ██║██║ ╚████║██║███████║██║  ██║███████╗███████╗██║   ║
║     ╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═══╝╚═╝╚══════╝╚═╝  ╚═╝╚══════╝╚══════╝╚═╝   ║
╚════════════════════════════════════════════════════════════════════════╝

[2026-03-15 15:47:00] [DaniShell:daniel] C:\Proyectos>
ls -la
📁 Directorios:
  📁 src       📁 docs      📁 tests

📄 Archivos:
  💻 Program.cs    📝 README.md    🧩 config.json

[2026-03-15 15:47:05] [DaniShell:daniel] C:\Proyectos>
grep -r \"TODO\" src/
src/core.cs:88: // TODO: Agregar soporte IPv6
src/utils.cs:42: // TODO: Optimizar búsqueda
```

---

## 🆚 ¿Por qué DaniShell gana?

| Característica | CMD | PowerShell | WSL | DaniShell |
|----------------|-----|------------|-----|-----------|
| Comandos Linux nativos | ❌ | Parcial | ✅ | ✅ **+30 reales** |
| Temas personalizados | ❌ | Básico | ❌ | ✅ **cyber/hacker** |
| Portable sin install | ✅ | ❌ | ❌ | ✅ |
| Plugins en .NET | ❌ | Parcial | ✅ | ✅ |
| Sin dependencias pesadas | ✅ | ❌ | ❌ | ✅ |
| Aliases fáciles | ❌ | ✅ | ✅ | ✅ |
| Fácil de usar | ⭐⭐ | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐ |

---

## ⌨️ Atajos de teclado

| Tecla | Acción |
|-------|--------|
| `Tab` | Autocompletado |
| `↑` / `↓` | Navegar historial |
| `←` / `→` | Mover cursor |
| `Home` / `End` | Inicio / fin de línea |
| `Ctrl+C` | Cancelar comando |
| `Esc` | Limpiar línea actual |

---

## 📁 Archivos de configuración

```
%USERPROFILE%\.danishell\
├── config.json          # Configuración general
├── aliases.json         # Aliases personalizados
├── themes.json          # Temas custom
├── history              # Historial de comandos
└── plugins\             # Carpeta para plugins (.dll)
```

---

## 🔧 Crear tu propio plugin

```csharp
using DaniShell.Commands;

public class SaludoPlugin : ICommand
{
    public string Name => \"saludo\";
    public string Description => \"Saluda al usuario\";
    public string Usage => \"saludo [nombre]\";
    public string Category => \"Personal\";
    public string[] Aliases => new[] { \"hola\" };

    public string Execute(string[] args, string currentDir)
    {
        string nombre = args.Length > 0 ? args[0] : \"crack\";
        Console.WriteLine($\"¡Qué onda, {nombre}! 🔥\");
        return currentDir;
    }
}
```

→ Compila como DLL → copia a `%USERPROFILE%\.danishell\plugins\` → ejecuta `plugins reload`

---

## 📞 Contacto & Redes

<div align=\"center\">

[![Email](https://img.shields.io/badge/Email-D14836?style=for-the-badge&logo=gmail&logoColor=white)](mailto:ahernandez89017@gmail.com)
[![Instagram](https://img.shields.io/badge/Instagram-E4405F?style=for-the-badge&logo=instagram&logoColor=white)](https://instagram.com/tu_usuario)
[![GitHub](https://img.shields.io/badge/GitHub-100000?style=for-the-badge&logo=github&logoColor=white)](https://github.com/Martinez2319)

**Desarrollado por Daniel**  
📍 Ciudad Juárez, Chihuahua, México 🇲🇽

</div>

---

## 📄 Licencia

**MIT License** – Libre para usar, modificar, compartir y aprender.

---

<div align=\"center\">

### ⭐ Si te mola DaniShell, ¡dale estrella al repo! ⭐

**Ayuda a que más gente descubra esta herramienta 🔥**

[🔽 Descargar](https://github.com/Martinez2319/DaniShell/releases) • [📖 Guía de Uso](docs/README_USO.md) • [🐛 Issues](https://github.com/Martinez2319/DaniShell/issues)

</div>
