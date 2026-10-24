# NeoJava

NeoJava has evolved from a simple Java starter into a custom competitive-programming runtime called NeoLang.

This project is not just Java code in a folder; it includes a lightweight language runtime, a challenge system, and a judge-style workflow for coding problems.

## What is included

- NeoLang: a tiny custom programming language with variables, conditionals, loops, arrays, and function-like calls
- Challenge manager: a small competitive-programming library with built-in problem definitions
- CLI runner: supports listing, describing, running, and judging NeoScript files
- Example challenge scripts in the `examples/` directory

## Project structure

- `src/main/java/com/neojava/neo/NeoLang.java` – main CLI entry point
- `src/main/java/com/neojava/neo/NeoInterpreter.java` – custom language interpreter
- `src/main/java/com/neojava/neo/NeoChallengeManager.java` – built-in challenge catalog
- `examples/` – NeoScript example programs and challenge files

## NeoScript language basics

```neo
let name = "Neo"
let score = 42

if score >= 40 {
  print "Rank: Elite"
} else {
  print "Rank: Rookie"
}

loop i from 1 to 5 {
  print i
}
```

Supported concepts:

- variable assignment with `let` and `=`
- `if` / `else` blocks
- Java-like `while (cond) { ... }` loops
- Java-like `for (let i = 0; i < 10; i = i + 1) { ... }`
- `switch (value) { case 1: ... default: ... }`
- `break` and `continue`
- `loop i from start to end { ... }`
- `print` statements
- arithmetic: `+ - * / %`
- comparisons: `== != > >= < <=`
- arrays: `[1, 2, 3]` and indexing like `values[0]`
- property access: `values.length`

```neo
for (let i = 1; i <= 5; i = i + 1) {
  print i
}

while (count < 3) {
  count = count + 1
  if (count == 2) {
    continue
  }
  print count
}

switch (mode) {
  case 1:
    print "one"
    break
  default:
    print "other"
}
```

## Build and run

Use any Java 21 JDK available on your machine.

```powershell
cd "C:\Users\OMAR AHMED\Documents\GitHub\NeoJava"
set JAVA_HOME=C:\Program Files\Eclipse Adoptium\jdk-21.0.12.101-hotspot
set PATH=%JAVA_HOME%\bin;%PATH%

mkdir out
javac -d out (Get-ChildItem -Recurse -Path src\main\java -Filter *.java | Select-Object -ExpandProperty FullName)
java -cp out com.neojava.neo.NeoLang --list
```

## Example commands

```powershell
java -cp out com.neojava.neo.NeoLang --list
java -cp out com.neojava.neo.NeoLang --describe neo-sum
java -cp out com.neojava.neo.NeoLang --run examples\sum.neo
java -cp out com.neojava.neo.NeoLang --judge neo-sum examples\sum.neo
```

## Sample challenge scripts

- `examples/hello.neo`
- `examples/sum.neo`
- `examples/prime.neo`
- `examples/fib.neo`

This project is intentionally a strong foundation for a richer competitive programming platform with more advanced challenge types, scoring, team modes, and a full online judge later on.
