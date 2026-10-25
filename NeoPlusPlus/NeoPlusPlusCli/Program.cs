using NeoPlusPlus.Runtime;

if (args.Length == 0 || args[0] is "--help" or "-h")
{
    PrintUsage();
    return;
}

if (args[0] is "--version" or "-v")
{
    Console.WriteLine($"Neo++ {NeoRuntime.Version} | NVM bytecode runtime");
    return;
}

if (args.Length < 2)
{
    Console.Error.WriteLine("A .neo source path is required.");
    PrintUsage();
    Environment.ExitCode = 2;
    return;
}

var command = args[0].ToLowerInvariant();
try
{
    var sourcePath = Path.GetFullPath(args[1]);
    var source = await File.ReadAllTextAsync(sourcePath);
    var runtime = new NeoRuntime();
    switch (command)
    {
        case "run":
        case "--run":
            var result = runtime.Run(source, sourcePath);
            Console.Write(result.Output);
            Console.Error.WriteLine($"[Neo++ {NeoRuntime.Version} | {result.Elapsed.TotalMilliseconds:F1} ms]");
            break;
        case "disassemble":
        case "--disassemble":
            Console.Write(runtime.Disassemble(source));
            break;
        default:
            Console.Error.WriteLine($"Unknown command: {args[0]}");
            PrintUsage();
            Environment.ExitCode = 2;
            break;
    }
}
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NeoLanguageException or InvalidOperationException or System.ComponentModel.Win32Exception or ArgumentException)
{
    Console.Error.WriteLine(exception.Message);
    Environment.ExitCode = 1;
}

static void PrintUsage()
{
    Console.WriteLine("Neo++ — language runtime and NVM bytecode tools");
    Console.WriteLine("Usage:");
    Console.WriteLine("  neo++ --version");
    Console.WriteLine("  neo++ run <file.neo>");
    Console.WriteLine("  neo++ disassemble <file.neo>");
    Console.WriteLine();
    Console.WriteLine("Neo++ programs run on the Neo Virtual Machine. Use java(\"Example.java\")");
    Console.WriteLine("inside a .neo file to call a Java source program through the JDK bridge.");
}
