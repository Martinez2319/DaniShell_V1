# Plugin de ejemplo

Con el SDK de .NET 8, desde la raíz:

```powershell
dotnet build Samples/HelloPlugin/HelloPlugin.csproj -c Release
```

Copia solamente `HelloPlugin.dll` y `HelloPlugin.plugin.json` de `bin/Release/net8.0/` a `%USERPROFILE%\.danishell\plugins\`. No copies el ensamblado del host DaniShell. Después ejecuta `plugins reload` y `hello Daniel`.

La API 1 requiere el manifiesto con `apiVersion` y permite declarar `minimumVersion`. Implementa `IStreamingCommand` para leer y escribir flujos sin acumular toda la entrada, y devolver un código de salida. Los plugins anteriores sin manifiesto necesitan añadirlo y recompilar contra la API compatible.
