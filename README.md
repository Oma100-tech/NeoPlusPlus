# Neo++

Neo++ is its own programming language and developer platform for Windows. Neo++ source files use the `.neo` extension and are compiled to NVM bytecode, then executed by the **NVM (Neo Virtual Machine)** implemented in the Neo++ runtime. Java is optional and used only through the Java interoperability bridge.

## Projects

- `NeoPlusPlus/NeoPlusPlusRuntime` — Neo++ lexer, compiler, NVM bytecode instruction set, stack VM, built-ins and Java source bridge
- `NeoPlusPlus/NeoPlusPlusCli` — standalone `NeoPlusPlusCli.exe` for running and disassembling `.neo` programs
- `NeoPlusPlus/NeoPlusPlusApp` — dark desktop IDE with source editor, challenge hub, runtime status, Java bridge, profiles and local leaderboard
- `NeoPlusPlus/NeoPlusPlusSetup` — branded GUI installer and uninstaller
- `NeoPlusPlus/examples` — sample `.neo` and Java interoperability programs
- `src/` — earlier prototypes retained as historical reference; they are not the Neo++ runtime

## Neo++ language

```neo
const title = "Neo++";

fn factorial(n) {
    if (n <= 1) {
        return 1;
    }
    return n * factorial(n - 1);
}

let total = 0;
for (let i = 1; i <= 5; i++) {
    total += i;
}

print title + " total = " + total;
print "5! = " + factorial(5);
```

The language currently supports:

- `let` variables and immutable `const` values
- numbers, strings, booleans and `null`
- arrays, indexing, `.length`, `len()` and string `contains()`
- arithmetic, comparisons and boolean operators
- `if` / `else`, `switch` / `case` / `default`
- `for`, `while`, `do` / `while`, and inclusive ascending `loop i from start to end`
- `break` and `continue`
- named functions, parameters, return values and recursion
- line comments beginning with `//` or `#`
- NVM instruction and call-depth limits

Statements use semicolons. See `NeoPlusPlus/examples/` for runnable samples.

## Run Neo++ from the command line

Build the CLI:

```powershell
cd "C:\path\to\NeoJava\NeoPlusPlus"
dotnet build NeoPlusPlusCli\NeoPlusPlusCli.csproj -c Release
```

Run a source file with the SDK:

```powershell
dotnet run --project NeoPlusPlusCli -- run examples\hello.neo
dotnet run --project NeoPlusPlusCli -- disassemble examples\functions.neo
dotnet run --project NeoPlusPlusCli -- --version
```

The self-contained command-line EXE is included in the installer package as `NeoPlusPlusCli.exe`.

## Java interoperability

Neo++ can launch a Java single-source program through the host JDK. For example:

```neo
let response = java("JavaBridgeDemo.java");
print "Java said: " + response;
```

Save the `.neo` script beside `JavaBridgeDemo.java` and run the script from the CLI or IDE. The `java()` built-in returns Java standard output as a Neo++ string. This is a process-based bridge, not an embedded JVM or direct Java object/API binding; it requires Java 11+ on `JAVA_HOME` or `PATH`.

The IDE also has a Java Bridge tab for choosing and running a `.java` source file directly.

## Desktop IDE and runtime hub

The IDE includes a Neo++ source editor, new/open/save controls, NVM bytecode disassembly, run and submit actions, challenge categories and difficulty filters, runtime/JDK status, the Java Bridge tab, local profiles, points and a local leaderboard. Challenge submissions are checked against expected output by the Neo++ VM.

Profiles and scores are stored locally on the Windows account. There is no online login or global leaderboard.

## Build the IDE

Requirements: Windows and the .NET 8 SDK.

```powershell
cd "C:\path\to\NeoJava\NeoPlusPlus"
dotnet build NeoPlusPlusApp\NeoPlusPlusApp.csproj -c Release
dotnet build NeoPlusPlusCli\NeoPlusPlusCli.csproj -c Release
dotnet build NeoPlusPlusSetup\NeoPlusPlusSetup.csproj -c Release
```

## Generate the Windows setup package

```powershell
cd "C:\path\to\NeoJava\NeoPlusPlus"
.\build-release.ps1
```

The script publishes self-contained x64 executables into `dist\`:

- `NeoPlusPlusSetup.exe` — GUI installer wizard
- `NeoPlusPlus.exe` — desktop IDE
- `NeoPlusPlusCli.exe` — standalone Neo++ command-line runtime
- `examples\` — sample Neo++ programs and the Java bridge demo

The installer lets you choose an installation folder and optionally create a desktop shortcut. It creates a Start-menu shortcut (available through Windows Search), registers an Apps & Features uninstall entry for the current user, and installs the IDE, CLI and examples without requiring administrator rights.

## Runtime safety

Neo++ VM programs run with instruction and call-depth limits. The Java bridge launches an external Java process with an execution timeout and output cap. This is not a security sandbox: only run `.neo` or Java programs you trust.
