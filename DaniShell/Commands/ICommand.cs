namespace DaniShell.Commands;

/// <summary>
/// Interfaz para implementar comandos/plugins en DaniShell
/// Los plugins deben implementar esta interfaz y compilarse como DLL
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Nombre del comando (lo que escribe el usuario)
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Descripción corta del comando para help
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Sintaxis de uso del comando
    /// </summary>
    string Usage { get; }
    
    /// <summary>
    /// Categoría del comando (Builtin, Linux, Network, Plugin, etc.)
    /// </summary>
    string Category { get; }
    
    /// <summary>
    /// Aliases alternativos para el comando
    /// </summary>
    string[] Aliases { get; }
    
    /// <summary>
    /// Ejecuta el comando
    /// </summary>
    /// <param name=\"args\">Argumentos pasados al comando</param>
    /// <param name=\"currentDir\">Directorio actual</param>
    /// <returns>Nuevo directorio actual (para comandos como cd)</returns>
    string Execute(string[] args, string currentDir);
}

/// <summary>
/// Clase base abstracta que facilita la implementación de comandos
/// </summary>
public abstract class CommandBase : ICommand
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual string Usage => Name;
    public virtual string Category => "General";
    public virtual string[] Aliases => Array.Empty<string>();
    
    public abstract string Execute(string[] args, string currentDir);
    
    protected void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }
    
    protected void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }
    
    protected void WriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}