using System.Diagnostics;
using System.Net;
using System.Text;

namespace DaniShell.Commands;

/// <summary>
/// Comandos de red: ping, tracert, ipinfo, netstat, http*, ftp*, ssh, nmap
/// </summary>
public static class NetworkCommands
{
    private static readonly HttpClient _http = new(new HttpClientHandler { AllowAutoRedirect = true });

    public static bool IsNetworkCommand(string cmd)
    {
        var netCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ping", "tracert", "ipinfo", "netstat", "nmap",
            "httpget", "httphead", "httppost",
            "ftplist", "ftpget", "ftpput",
            "ssh", "telnet", "nslookup", "dig"
        };
        return netCommands.Contains(cmd);
    }

    public static void Execute(string cmd, string[] args, string currentDir)
    {
        switch (cmd.ToLower())
        {
            case "ipinfo":
                IpInfo();
                break;
            case "netstat":
                NetstatSummary();
                break;
            case "httpget":
                HttpGet(args, currentDir);
                break;
            case "httphead":
                HttpHead(args);
                break;
            case "httppost":
                HttpPost(args);
                break;
            case "ftplist":
                FtpList(args);
                break;
            case "ftpget":
                FtpGet(args, currentDir);
                break;
            case "ftpput":
                FtpPut(args, currentDir);
                break;
            case "ssh":
                SshWrapper(args, currentDir);
                break;
            case "nmap":
                ExecuteNmap(args, currentDir);
                break;
            case "nslookup":
            case "dig":
                Core.Shell.ExecuteExternal($"nslookup {string.Join(" ", args)}", currentDir);
                break;
            default:
                // ping, tracert, telnet - usar comando externo
                Core.Shell.ExecuteExternal($"{cmd} {string.Join(" ", args)}", currentDir);
                break;
        }
    }

    private static void IpInfo()
    {
        Core.ThemeManager.WriteLine("=== Información de Red ===", "Info");
        Core.Shell.ExecuteExternal("ipconfig /all", Directory.GetCurrentDirectory());
    }

    private static void NetstatSummary()
    {
        Core.ThemeManager.WriteLine("=== Conexiones de Red ===", "Info");
        Core.Shell.ExecuteExternal("netstat -ano", Directory.GetCurrentDirectory());
    }

    private static void HttpGet(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: httpget <url> [archivo_salida]");
            return;
        }

        var url = args[0];
        var outFile = args.Length > 1 ? args[1] : null;

        try
        {
            var response = _http.GetAsync(url).Result;
            var data = response.Content.ReadAsByteArrayAsync().Result;

            Core.ThemeManager.WriteLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}", "Info");
            
            foreach (var h in response.Headers)
                Console.WriteLine($"{h.Key}: {string.Join(", ", h.Value)}");
            foreach (var h in response.Content.Headers)
                Console.WriteLine($"{h.Key}: {string.Join(", ", h.Value)}");

            Console.WriteLine();

            if (outFile != null)
            {
                var path = Path.IsPathRooted(outFile) ? outFile : Path.Combine(dir, outFile);
                File.WriteAllBytes(path, data);
                Core.ThemeManager.WriteSuccess($"Guardado en: {path} ({data.Length} bytes)");
            }
            else
            {
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                if (contentType.Contains("text") || contentType.Contains("json") || contentType.Contains("xml"))
                    Console.WriteLine(Encoding.UTF8.GetString(data));
                else
                    Console.WriteLine($"[{data.Length} bytes recibidos]");
            }
        }
        catch (Exception ex)
        {
            Core.ThemeManager.WriteError($"HTTP error: {ex.Message}");
        }
    }

    private static void HttpHead(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: httphead <url>");
            return;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Head, args[0]);
            var response = _http.SendAsync(request).Result;

            Core.ThemeManager.WriteLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}", "Info");
            
            foreach (var h in response.Headers)
                Console.WriteLine($"{h.Key}: {string.Join(", ", h.Value)}");
            foreach (var h in response.Content.Headers)
                Console.WriteLine($"{h.Key}: {string.Join(", ", h.Value)}");
        }
        catch (Exception ex)
        {
            Core.ThemeManager.WriteError($"HTTP error: {ex.Message}");
        }
    }

    private static void HttpPost(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Uso: httppost <url> key=val key=val ...");
            return;
        }

        var url = args[0];
        var dict = new Dictionary<string, string>();

        foreach (var kv in args.Skip(1))
        {
            var idx = kv.IndexOf('=');
            if (idx > 0)
                dict[kv[..idx]] = kv[(idx + 1)..];
        }

        try
        {
            var content = new FormUrlEncodedContent(dict);
            var response = _http.PostAsync(url, content).Result;
            var text = response.Content.ReadAsStringAsync().Result;

            Core.ThemeManager.WriteLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}", "Info");
            Console.WriteLine(text);
        }
        catch (Exception ex)
        {
            Core.ThemeManager.WriteError($"HTTP error: {ex.Message}");
        }
    }

    private static NetworkCredential? ParseFtpCreds(Uri uri)
    {
        if (!string.IsNullOrEmpty(uri.UserInfo) && uri.UserInfo.Contains(':'))
        {
            var parts = uri.UserInfo.Split(':', 2);
            return new NetworkCredential(
                WebUtility.UrlDecode(parts[0]),
                WebUtility.UrlDecode(parts[1]));
        }
        return null;
    }

    private static void FtpList(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: ftplist ftp://user:pass@host/ruta");
            return;
        }

        if (!Uri.TryCreate(args[0], UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeFtp)
        {
            Core.ThemeManager.WriteError("URL FTP inválida.");
            return;
        }

        try
        {
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            
            var cred = ParseFtpCreds(uri);
            if (cred != null) request.Credentials = cred;

            using var response = (FtpWebResponse)request.GetResponse();
            using var reader = new StreamReader(response.GetResponseStream());
            Console.WriteLine(reader.ReadToEnd());
        }
        catch (Exception ex)
        {
            Core.ThemeManager.WriteError($"FTP error: {ex.Message}");
        }
    }

    private static void FtpGet(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: ftpget <ftp-url> [archivo_salida]");
            return;
        }

        if (!Uri.TryCreate(args[0], UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeFtp)
        {
            Core.ThemeManager.WriteError("URL FTP inválida.");
            return;
        }

        var outFile = args.Length > 1 ? args[1] : Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrEmpty(outFile)) outFile = "download.bin";

        try
        {
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            
            var cred = ParseFtpCreds(uri);
            if (cred != null) request.Credentials = cred;

            using var response = (FtpWebResponse)request.GetResponse();
            using var stream = response.GetResponseStream();
            using var fs = File.Create(Path.Combine(dir, outFile));
            stream!.CopyTo(fs);

            Core.ThemeManager.WriteSuccess($"Descargado: {outFile} ({response.StatusDescription.Trim()})");
        }
        catch (Exception ex)
        {
            Core.ThemeManager.WriteError($"FTP error: {ex.Message}");
        }
    }

    private static void FtpPut(string[] args, string dir)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Uso: ftpput <archivo_local> <ftp-url>");
            return;
        }

        var localPath = Path.IsPathRooted(args[0]) ? args[0] : Path.Combine(dir, args[0]);
        if (!File.Exists(localPath))
        {
            Core.ThemeManager.WriteError("Archivo local no existe.");
            return;
        }

        if (!Uri.TryCreate(args[1], UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeFtp)
        {
            Core.ThemeManager.WriteError("URL FTP inválida.");
            return;
        }

        try
        {
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            
            var cred = ParseFtpCreds(uri);
            if (cred != null) request.Credentials = cred;

            using var fs = File.OpenRead(localPath);
            using var rs = request.GetRequestStream();
            fs.CopyTo(rs);

            using var response = (FtpWebResponse)request.GetResponse();
            Core.ThemeManager.WriteSuccess($"Subido: {Path.GetFileName(localPath)} ({response.StatusDescription.Trim()})");
        }
        catch (Exception ex)
        {
            Core.ThemeManager.WriteError($"FTP error: {ex.Message}");
        }
    }

    private static void SshWrapper(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: ssh user@host [comando]");
            Console.WriteLine("Ej:  ssh usuario@192.168.1.50");
            Console.WriteLine("     ssh usuario@192.168.1.50 \"ls -la\"");
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "ssh",
            Arguments = string.Join(' ', args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = dir
        };

        try
        {
            using var process = Process.Start(psi);
            var output = process!.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(output))
                Console.WriteLine(output);
            if (!string.IsNullOrWhiteSpace(error))
                Core.ThemeManager.WriteError(error);
        }
        catch (Exception ex)
        {
            Core.ThemeManager.WriteError($"No se pudo ejecutar ssh. Asegúrate de tener OpenSSH instalado.");
            Core.ThemeManager.WriteMuted($"Error: {ex.Message}");
        }
    }

    private static void ExecuteNmap(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: nmap <objetivo> [opciones]");
            Console.WriteLine("Ej: nmap 192.168.1.1 -p 22,80,443");
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "nmap",
            Arguments = string.Join(' ', args),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = dir
        };

        try
        {
            using var process = Process.Start(psi);
            var output = process!.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(output))
                Console.WriteLine(output);
            if (!string.IsNullOrWhiteSpace(error))
                Core.ThemeManager.WriteError(error);
        }
        catch (Exception ex)
        {
            Core.ThemeManager.WriteError("Error ejecutando nmap. Asegúrate de tenerlo instalado y en PATH.");
            Core.ThemeManager.WriteMuted($"Detalle: {ex.Message}");
        }
    }
}
