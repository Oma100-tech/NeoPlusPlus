using System.Globalization;
using System.Text;

namespace NeoPlusPlus.Runtime;

public sealed record NeoRunResult(string Output, string Error, TimeSpan Elapsed);

public sealed class NeoRuntime
{
    public const string Version = "0.2.0";

    public NeoRunResult Run(string source, string? sourcePath = null)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var program = new NeoCompiler(source).Compile();
        var output = new StringBuilder();
        var vm = new NeoVirtualMachine(program, output, sourcePath);
        vm.Execute();
        timer.Stop();
        return new NeoRunResult(output.ToString(), string.Empty, timer.Elapsed);
    }

    public string Disassemble(string source)
    {
        var program = new NeoCompiler(source).Compile();
        var output = new StringBuilder();
        AppendChunk(output, "main", program.Main);
        foreach (var function in program.Functions.Values.OrderBy(function => function.Name, StringComparer.Ordinal))
            AppendChunk(output, $"fn {function.Name}/{function.Parameters.Count}", function.Code);
        return output.ToString();
    }

    private static void AppendChunk(StringBuilder output, string name, IReadOnlyList<NeoInstruction> instructions)
    {
        output.AppendLine($"== {name} ==");
        for (int i = 0; i < instructions.Count; i++)
        {
            var instruction = instructions[i];
            output.Append(i.ToString("D4", CultureInfo.InvariantCulture))
                .Append("  ")
                .Append(instruction.Code.ToString().PadRight(20))
                .Append(instruction.Operand is null ? string.Empty : instruction.Operand)
                .Append("  [line ")
                .Append(instruction.Line)
                .AppendLine("]");
        }
    }
}

internal sealed record NeoInstruction(NeoOpCode Code, object? Operand = null, int Line = 1);
internal sealed record NeoFunction(string Name, IReadOnlyList<string> Parameters, IReadOnlyList<NeoInstruction> Code);
internal sealed record NeoProgram(IReadOnlyList<NeoInstruction> Main, IReadOnlyDictionary<string, NeoFunction> Functions);

internal enum NeoOpCode
{
    Constant, Null, True, False, DefineGlobal, DefineGlobalConst, GetGlobal, SetGlobal, GetLocal, SetLocal,
    Pop, Print, Add, Subtract, Multiply, Divide, Modulo, Negate, Not,
    Equal, NotEqual, Greater, GreaterEqual, Less, LessEqual, And, Or,
    Jump, JumpIfFalse, JumpIfTrue, Loop, BuildArray, GetIndex, GetProperty, Call, Return
}

public sealed class NeoLanguageException(string message, int line) : Exception($"line {line}: {message}");

internal enum NeoTokenKind
{
    End, Identifier, Number, String,
    LeftParen, RightParen, LeftBrace, RightBrace, LeftBracket, RightBracket,
    Comma, Semicolon, Dot, Colon,
    Plus, Minus, Star, Slash, Percent, PlusPlus, MinusMinus, PlusEqual, MinusEqual,
    Equal, EqualEqual, Bang, BangEqual, Greater, GreaterEqual, Less, LessEqual, AndAnd, OrOr,
    Let, Const, Print, If, Else, While, Do, For, Loop, From, To, Break, Continue,
    Fn, Return, True, False, Null, Switch, Case, Default
}

internal sealed record NeoToken(NeoTokenKind Kind, string Text, object? Value, int Line);

internal sealed class NeoLexer(string source)
{
    private readonly List<NeoToken> _tokens = [];
    private int _start;
    private int _current;
    private int _line = 1;

    public IReadOnlyList<NeoToken> Scan()
    {
        while (!AtEnd)
        {
            _start = _current;
            ScanToken();
        }

        _tokens.Add(new NeoToken(NeoTokenKind.End, string.Empty, null, _line));
        return _tokens;
    }

    private bool AtEnd => _current >= source.Length;
    private char Peek => AtEnd ? '\0' : source[_current];
    private char PeekNext => _current + 1 >= source.Length ? '\0' : source[_current + 1];

    private void ScanToken()
    {
        char character = Advance();
        switch (character)
        {
            case '(': Add(NeoTokenKind.LeftParen); break;
            case ')': Add(NeoTokenKind.RightParen); break;
            case '{': Add(NeoTokenKind.LeftBrace); break;
            case '}': Add(NeoTokenKind.RightBrace); break;
            case '[': Add(NeoTokenKind.LeftBracket); break;
            case ']': Add(NeoTokenKind.RightBracket); break;
            case ',': Add(NeoTokenKind.Comma); break;
            case ';': Add(NeoTokenKind.Semicolon); break;
            case '.': Add(NeoTokenKind.Dot); break;
            case ':': Add(NeoTokenKind.Colon); break;
            case '+': Add(Match('=') ? NeoTokenKind.PlusEqual : Match('+') ? NeoTokenKind.PlusPlus : NeoTokenKind.Plus); break;
            case '-': Add(Match('=') ? NeoTokenKind.MinusEqual : Match('-') ? NeoTokenKind.MinusMinus : NeoTokenKind.Minus); break;
            case '*': Add(NeoTokenKind.Star); break;
            case '%': Add(NeoTokenKind.Percent); break;
            case '!': Add(Match('=') ? NeoTokenKind.BangEqual : NeoTokenKind.Bang); break;
            case '=': Add(Match('=') ? NeoTokenKind.EqualEqual : NeoTokenKind.Equal); break;
            case '>': Add(Match('=') ? NeoTokenKind.GreaterEqual : NeoTokenKind.Greater); break;
            case '<': Add(Match('=') ? NeoTokenKind.LessEqual : NeoTokenKind.Less); break;
            case '&':
                if (Match('&')) Add(NeoTokenKind.AndAnd);
                else throw Error("Expected '&' after '&'.");
                break;
            case '|':
                if (Match('|')) Add(NeoTokenKind.OrOr);
                else throw Error("Expected '|' after '|'.");
                break;
            case '/':
                if (Match('/'))
                {
                    while (Peek != '\n' && !AtEnd) Advance();
                }
                else
                {
                    Add(NeoTokenKind.Slash);
                }
                break;
            case '#':
                while (Peek != '\n' && !AtEnd) Advance();
                break;
            case '"':
            case '\'':
                ReadString(character);
                break;
            case '\n':
                _line++;
                break;
            case ' ':
            case '\r':
            case '\t':
                break;
            default:
                if (char.IsDigit(character)) ReadNumber();
                else if (char.IsLetter(character) || character == '_') ReadIdentifier();
                else throw Error($"Unexpected character '{character}'.");
                break;
        }
    }

    private void ReadString(char quote)
    {
        var value = new StringBuilder();
        while (!AtEnd && Peek != quote)
        {
            if (Peek == '\n') _line++;
            char character = Advance();
            if (character == '\\' && !AtEnd)
            {
                character = Advance();
                value.Append(character switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    _ => character
                });
            }
            else
            {
                value.Append(character);
            }
        }

        if (AtEnd) throw Error("Unterminated string.");
        Advance();
        _tokens.Add(new NeoToken(NeoTokenKind.String, source[_start.._current], value.ToString(), _line));
    }

    private void ReadNumber()
    {
        while (char.IsDigit(Peek)) Advance();
        if (Peek == '.' && char.IsDigit(PeekNext))
        {
            Advance();
            while (char.IsDigit(Peek)) Advance();
        }

        string text = source[_start.._current];
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            throw Error("Invalid number.");
        }
        _tokens.Add(new NeoToken(NeoTokenKind.Number, text, number, _line));
    }

    private void ReadIdentifier()
    {
        while (char.IsLetterOrDigit(Peek) || Peek == '_') Advance();
        string text = source[_start.._current];
        var kind = text switch
        {
            "let" or "var" => NeoTokenKind.Let,
            "const" => NeoTokenKind.Const,
            "print" => NeoTokenKind.Print,
            "if" => NeoTokenKind.If,
            "else" => NeoTokenKind.Else,
            "while" => NeoTokenKind.While,
            "do" => NeoTokenKind.Do,
            "for" => NeoTokenKind.For,
            "loop" => NeoTokenKind.Loop,
            "from" => NeoTokenKind.From,
            "to" => NeoTokenKind.To,
            "break" => NeoTokenKind.Break,
            "continue" => NeoTokenKind.Continue,
            "fn" => NeoTokenKind.Fn,
            "return" => NeoTokenKind.Return,
            "true" => NeoTokenKind.True,
            "false" => NeoTokenKind.False,
            "null" => NeoTokenKind.Null,
            "switch" => NeoTokenKind.Switch,
            "case" => NeoTokenKind.Case,
            "default" => NeoTokenKind.Default,
            _ => NeoTokenKind.Identifier
        };
        _tokens.Add(new NeoToken(kind, text, null, _line));
    }

    private char Advance() => source[_current++];

    private bool Match(char expected)
    {
        if (AtEnd || source[_current] != expected) return false;
        _current++;
        return true;
    }

    private void Add(NeoTokenKind kind) =>
        _tokens.Add(new NeoToken(kind, source[_start.._current], null, _line));

    private NeoLanguageException Error(string message) => new(message, _line);
}

internal sealed class NeoCompiler
{
    private sealed class LoopContext
    {
        public bool IsLoop { get; init; }
        public List<int> Breaks { get; } = [];
        public List<int> Continues { get; } = [];
        public int ContinueTarget { get; set; } = -1;
    }

    private readonly IReadOnlyList<NeoToken> _tokens;
    private readonly List<NeoInstruction> _code = [];
    private readonly Dictionary<string, NeoFunction> _functions = new(StringComparer.Ordinal);
    private readonly HashSet<string> _locals;
    private readonly HashSet<string> _constants;
    private readonly Stack<LoopContext> _loops = new();
    private int _current;
    private int _hiddenId;
    private bool _inFunction;

    public NeoCompiler(string source, IEnumerable<string>? parameters = null, bool inFunction = false)
        : this(new NeoLexer(source).Scan(), parameters, inFunction)
    {
    }

    private NeoCompiler(IReadOnlyList<NeoToken> tokens, IEnumerable<string>? parameters, bool inFunction)
    {
        _tokens = tokens;
        _locals = new HashSet<string>(parameters ?? [], StringComparer.Ordinal);
        _constants = new HashSet<string>(StringComparer.Ordinal);
        _inFunction = inFunction;
    }

    public NeoProgram Compile()
    {
        while (!AtEnd)
        {
            if (Match(NeoTokenKind.Fn)) CompileFunction(Previous);
            else Declaration();
        }
        Emit(NeoOpCode.Null);
        Emit(NeoOpCode.Return);
        return new NeoProgram(_code, _functions);
    }

    private void CompileFunction(NeoToken fnToken)
    {
        if (_inFunction) throw Error(fnToken, "Nested function declarations are not supported.");
        var name = Consume(NeoTokenKind.Identifier, "Expected a function name.");
        Consume(NeoTokenKind.LeftParen, "Expected '(' after function name.");
        var parameters = new List<string>();
        if (!Check(NeoTokenKind.RightParen))
        {
            do
            {
                parameters.Add(Consume(NeoTokenKind.Identifier, "Expected parameter name.").Text);
            } while (Match(NeoTokenKind.Comma));
        }
        Consume(NeoTokenKind.RightParen, "Expected ')' after parameters.");
        Consume(NeoTokenKind.LeftBrace, "Expected '{' before function body.");
        int bodyStart = _current;
        int depth = 1;
        while (depth > 0 && !AtEnd)
        {
            if (Match(NeoTokenKind.LeftBrace)) depth++;
            else if (Match(NeoTokenKind.RightBrace)) depth--;
            else Advance();
        }
        if (depth != 0) throw Error(Previous, "Unterminated function body.");

        var bodyTokens = _tokens.Skip(bodyStart).Take(_current - bodyStart - 1).ToList();
        bodyTokens.Add(new NeoToken(NeoTokenKind.End, string.Empty, null, Previous.Line));
        var compiler = new NeoCompiler(bodyTokens, parameters, inFunction: true);
        var functionProgram = compiler.Compile();
        if (!_functions.TryAdd(name.Text, new NeoFunction(name.Text, parameters, functionProgram.Main)))
        {
            throw Error(name, $"Function '{name.Text}' is already defined.");
        }
    }

    private void Declaration()
    {
        if (Match(NeoTokenKind.Let)) { VariableDeclaration(isConstant: false, Previous); return; }
        if (Match(NeoTokenKind.Const)) { VariableDeclaration(isConstant: true, Previous); return; }
        Statement();
    }

    private void VariableDeclaration(bool isConstant, NeoToken keyword)
    {
        var name = Consume(NeoTokenKind.Identifier, "Expected a variable name.");
        Consume(NeoTokenKind.Equal, "Expected '=' after variable name.");
        Expression();
        Consume(NeoTokenKind.Semicolon, "Expected ';' after variable declaration.");
        if (_inFunction)
        {
            if (!_locals.Add(name.Text)) throw Error(name, $"Variable '{name.Text}' is already defined in this function.");
            Emit(NeoOpCode.SetLocal, name.Text, keyword.Line);
            Emit(NeoOpCode.Pop, line: keyword.Line);
            if (isConstant) _constants.Add(name.Text);
        }
        else
        {
            Emit(isConstant ? NeoOpCode.DefineGlobalConst : NeoOpCode.DefineGlobal, name.Text, keyword.Line);
            if (isConstant) _constants.Add(name.Text);
        }
    }

    private void Statement()
    {
        if (Match(NeoTokenKind.Print)) { PrintStatement(Previous); return; }
        if (Match(NeoTokenKind.If)) { IfStatement(Previous); return; }
        if (Match(NeoTokenKind.While)) { WhileStatement(Previous); return; }
        if (Match(NeoTokenKind.Do)) { DoWhileStatement(Previous); return; }
        if (Match(NeoTokenKind.For)) { ForStatement(Previous); return; }
        if (Match(NeoTokenKind.Loop)) { RangeLoop(Previous); return; }
        if (Match(NeoTokenKind.Switch)) { SwitchStatement(Previous); return; }
        if (Match(NeoTokenKind.Break)) { BreakStatement(Previous); return; }
        if (Match(NeoTokenKind.Continue)) { ContinueStatement(Previous); return; }
        if (Match(NeoTokenKind.Return)) { ReturnStatement(Previous); return; }
        if (Match(NeoTokenKind.LeftBrace)) { Block(); return; }
        if (Match(NeoTokenKind.Identifier))
        {
            var identifier = Previous;
            if (Check(NeoTokenKind.LeftParen))
            {
                _current--;
                Expression();
                Consume(NeoTokenKind.Semicolon, "Expected ';' after function call.");
                Emit(NeoOpCode.Pop, line: identifier.Line);
            }
            else
            {
                IdentifierStatement(identifier);
            }
            return;
        }
        throw Error(Peek, "Expected a statement.");
    }

    private void PrintStatement(NeoToken keyword)
    {
        bool wrapped = Match(NeoTokenKind.LeftParen);
        Expression();
        if (wrapped) Consume(NeoTokenKind.RightParen, "Expected ')' after value.");
        Consume(NeoTokenKind.Semicolon, "Expected ';' after print.");
        Emit(NeoOpCode.Print, line: keyword.Line);
    }

    private void IfStatement(NeoToken keyword)
    {
        bool wrapped = Match(NeoTokenKind.LeftParen);
        Expression();
        if (wrapped) Consume(NeoTokenKind.RightParen, "Expected ')' after condition.");
        int thenJump = EmitJump(NeoOpCode.JumpIfFalse, keyword.Line);
        Emit(NeoOpCode.Pop, line: keyword.Line);
        RequireBlock("Expected '{' after if condition.");
        int elseJump = EmitJump(NeoOpCode.Jump, keyword.Line);
        PatchJump(thenJump);
        Emit(NeoOpCode.Pop, line: keyword.Line);
        if (Match(NeoTokenKind.Else))
        {
            if (Match(NeoTokenKind.If))
            {
                IfStatement(Previous);
            }
            else
            {
                RequireBlock("Expected '{' after else.");
            }
        }
        PatchJump(elseJump);
    }

    private void WhileStatement(NeoToken keyword)
    {
        bool wrapped = Match(NeoTokenKind.LeftParen);
        int start = _code.Count;
        Expression();
        if (wrapped) Consume(NeoTokenKind.RightParen, "Expected ')' after condition.");
        int exitJump = EmitJump(NeoOpCode.JumpIfFalse, keyword.Line);
        Emit(NeoOpCode.Pop, line: keyword.Line);
        var loop = new LoopContext { IsLoop = true, ContinueTarget = start };
        _loops.Push(loop);
        RequireBlock("Expected '{' after while condition.");
        EmitLoop(start, keyword.Line);
        PatchJump(exitJump);
        Emit(NeoOpCode.Pop, line: keyword.Line);
        PatchLoop(loop, keyword.Line);
    }

    private void DoWhileStatement(NeoToken keyword)
    {
        int start = _code.Count;
        var loop = new LoopContext { IsLoop = true };
        _loops.Push(loop);
        RequireBlock("Expected '{' after do.");
        loop.ContinueTarget = _code.Count;
        Consume(NeoTokenKind.While, "Expected while after do-block.");
        bool wrapped = Match(NeoTokenKind.LeftParen);
        Expression();
        if (wrapped) Consume(NeoTokenKind.RightParen, "Expected ')' after condition.");
        Consume(NeoTokenKind.Semicolon, "Expected ';' after do-while.");
        int exitJump = EmitJump(NeoOpCode.JumpIfFalse, keyword.Line);
        Emit(NeoOpCode.Pop, line: keyword.Line);
        EmitLoop(start, keyword.Line);
        PatchJump(exitJump);
        Emit(NeoOpCode.Pop, line: keyword.Line);
        PatchLoop(loop, keyword.Line);
    }

    private void ForStatement(NeoToken keyword)
    {
        Consume(NeoTokenKind.LeftParen, "Expected '(' after for.");
        if (Match(NeoTokenKind.Semicolon))
        {
        }
        else if (Match(NeoTokenKind.Let))
        {
            VariableDeclaration(isConstant: false, Previous);
        }
        else
        {
            IdentifierStatement(Consume(NeoTokenKind.Identifier, "Expected initializer."));
        }

        int conditionStart = _code.Count;
        bool hasCondition = !Check(NeoTokenKind.Semicolon);
        if (hasCondition) Expression();
        else Emit(NeoOpCode.True, line: keyword.Line);
        Consume(NeoTokenKind.Semicolon, "Expected ';' after for condition.");
        int exitJump = EmitJump(NeoOpCode.JumpIfFalse, keyword.Line);
        Emit(NeoOpCode.Pop, line: keyword.Line);

        int updateStart = _current;
        bool hasUpdate = !Check(NeoTokenKind.RightParen);
        if (hasUpdate)
        {
            SkipUpdate();
        }
        Consume(NeoTokenKind.RightParen, "Expected ')' after for clauses.");

        var loop = new LoopContext { IsLoop = true };
        _loops.Push(loop);
        RequireBlock("Expected '{' after for clauses.");
        int bodyEnd = _current;
        int updateAddress = _code.Count;
        loop.ContinueTarget = updateAddress;
        if (hasUpdate)
        {
            _current = updateStart;
            if (Match(NeoTokenKind.Identifier)) IdentifierStatement(Previous, expectSemicolon: false);
            _current = bodyEnd;
        }
        EmitLoop(conditionStart, keyword.Line);
        PatchJump(exitJump);
        Emit(NeoOpCode.Pop, line: keyword.Line);
        PatchLoop(loop, keyword.Line);
    }

    private void SkipUpdate()
    {
        Consume(NeoTokenKind.Identifier, "Expected update variable.");
        if (Match(NeoTokenKind.Equal) || Match(NeoTokenKind.PlusEqual) || Match(NeoTokenKind.MinusEqual))
        {
            Expression();
        }
        else if (!Match(NeoTokenKind.PlusPlus) && !Match(NeoTokenKind.MinusMinus))
        {
            throw Error(Peek, "Expected assignment or increment in for update.");
        }
    }

    private void RangeLoop(NeoToken keyword)
    {
        var variable = Consume(NeoTokenKind.Identifier, "Expected loop variable.");
        Consume(NeoTokenKind.From, "Expected 'from' after loop variable.");
        Expression();
        string limitName = $"$limit{_hiddenId++}";
        Emit(NeoOpCode.DefineGlobal, limitName, keyword.Line);
        Consume(NeoTokenKind.To, "Expected 'to' after loop start.");
        Expression();
        string endName = $"$end{_hiddenId++}";
        Emit(NeoOpCode.DefineGlobal, endName, keyword.Line);
        Emit(NeoOpCode.GetGlobal, limitName, keyword.Line);
        Emit(NeoOpCode.DefineGlobal, variable.Text, keyword.Line);
        int start = _code.Count;
        Emit(NeoOpCode.GetGlobal, variable.Text, keyword.Line);
        Emit(NeoOpCode.GetGlobal, endName, keyword.Line);
        Emit(NeoOpCode.LessEqual, line: keyword.Line);
        int exitJump = EmitJump(NeoOpCode.JumpIfFalse, keyword.Line);
        Emit(NeoOpCode.Pop, line: keyword.Line);
        var loop = new LoopContext { IsLoop = true };
        _loops.Push(loop);
        RequireBlock("Expected '{' after loop range.");
        loop.ContinueTarget = _code.Count;
        Emit(NeoOpCode.GetGlobal, variable.Text, keyword.Line);
        Emit(NeoOpCode.Constant, 1d, keyword.Line);
        Emit(NeoOpCode.Add, line: keyword.Line);
        Emit(NeoOpCode.SetGlobal, variable.Text, keyword.Line);
        Emit(NeoOpCode.Pop, line: keyword.Line);
        EmitLoop(start, keyword.Line);
        PatchJump(exitJump);
        Emit(NeoOpCode.Pop, line: keyword.Line);
        PatchLoop(loop, keyword.Line);
    }

    private void SwitchStatement(NeoToken keyword)
    {
        Consume(NeoTokenKind.LeftParen, "Expected '(' after switch.");
        Expression();
        Consume(NeoTokenKind.RightParen, "Expected ')' after switch value.");
        string switchName = $"$switch{_hiddenId++}";
        Emit(NeoOpCode.DefineGlobal, switchName, keyword.Line);
        Consume(NeoTokenKind.LeftBrace, "Expected '{' before switch cases.");
        var endJumps = new List<int>();
        var caseJumps = new List<int>();
        var switchLoop = new LoopContext();
        _loops.Push(switchLoop);
        int defaultStart = -1;

        while (!Check(NeoTokenKind.RightBrace) && !AtEnd)
        {
            if (Match(NeoTokenKind.Case))
            {
                Emit(NeoOpCode.GetGlobal, switchName, Previous.Line);
                Expression();
                Emit(NeoOpCode.Equal, line: Previous.Line);
                caseJumps.Add(EmitJump(NeoOpCode.JumpIfFalse, Previous.Line));
                Emit(NeoOpCode.Pop, line: Previous.Line);
                Consume(NeoTokenKind.Colon, "Expected ':' after case.");
                while (!Check(NeoTokenKind.Case) && !Check(NeoTokenKind.Default) && !Check(NeoTokenKind.RightBrace))
                {
                    Declaration();
                }
                endJumps.Add(EmitJump(NeoOpCode.Jump, keyword.Line));
                PatchJump(caseJumps[^1]);
                Emit(NeoOpCode.Pop, line: keyword.Line);
                caseJumps.RemoveAt(caseJumps.Count - 1);
            }
            else if (Match(NeoTokenKind.Default))
            {
                Consume(NeoTokenKind.Colon, "Expected ':' after default.");
                defaultStart = _code.Count;
                while (!Check(NeoTokenKind.Case) && !Check(NeoTokenKind.RightBrace))
                {
                    Declaration();
                }
                endJumps.Add(EmitJump(NeoOpCode.Jump, keyword.Line));
            }
            else
            {
                throw Error(Peek, "Expected 'case' or 'default'.");
            }
        }
        Consume(NeoTokenKind.RightBrace, "Expected '}' after switch.");
        int end = _code.Count;
        foreach (int jump in endJumps) PatchJump(jump, end);
        foreach (int jump in caseJumps) PatchJump(jump, defaultStart >= 0 ? defaultStart : end);
        _loops.Pop();
        foreach (int jump in switchLoop.Breaks) PatchJump(jump, end);
    }

    private void BreakStatement(NeoToken token)
    {
        if (_loops.Count == 0) throw Error(token, "'break' can only be used inside a loop or switch.");
        Consume(NeoTokenKind.Semicolon, "Expected ';' after break.");
        _loops.Peek().Breaks.Add(EmitJump(NeoOpCode.Jump, token.Line));
    }

    private void ContinueStatement(NeoToken token)
    {
        var loop = _loops.FirstOrDefault(context => context.IsLoop);
        if (loop is null) throw Error(token, "'continue' can only be used inside a loop.");
        Consume(NeoTokenKind.Semicolon, "Expected ';' after continue.");
        loop.Continues.Add(EmitJump(NeoOpCode.Jump, token.Line));
    }

    private void ReturnStatement(NeoToken token)
    {
        if (!_inFunction) throw Error(token, "'return' can only be used inside a function.");
        if (Match(NeoTokenKind.Semicolon)) Emit(NeoOpCode.Null, line: token.Line);
        else
        {
            Expression();
            Consume(NeoTokenKind.Semicolon, "Expected ';' after return value.");
        }
        Emit(NeoOpCode.Return, line: token.Line);
    }

    private void IdentifierStatement(NeoToken name, bool expectSemicolon = true)
    {
        if (Match(NeoTokenKind.Equal))
        {
            CheckAssignable(name);
            Expression();
            EmitSet(name);
            Emit(NeoOpCode.Pop, line: name.Line);
        }
        else if (Match(NeoTokenKind.PlusEqual, NeoTokenKind.MinusEqual))
        {
            CheckAssignable(name);
            var operation = Previous.Kind == NeoTokenKind.PlusEqual ? NeoOpCode.Add : NeoOpCode.Subtract;
            EmitGet(name);
            Expression();
            Emit(operation, line: name.Line);
            EmitSet(name);
            Emit(NeoOpCode.Pop, line: name.Line);
        }
        else if (Match(NeoTokenKind.PlusPlus, NeoTokenKind.MinusMinus))
        {
            CheckAssignable(name);
            EmitGet(name);
            Emit(NeoOpCode.Constant, 1d, name.Line);
            Emit(Previous.Kind == NeoTokenKind.PlusPlus ? NeoOpCode.Add : NeoOpCode.Subtract, line: name.Line);
            EmitSet(name);
            Emit(NeoOpCode.Pop, line: name.Line);
        }
        else
        {
            throw Error(Peek, "Expected assignment or increment after variable.");
        }
        if (expectSemicolon) Consume(NeoTokenKind.Semicolon, "Expected ';' after statement.");
    }

    private void CheckAssignable(NeoToken name)
    {
        if (_constants.Contains(name.Text)) throw Error(name, $"Cannot assign to constant '{name.Text}'.");
    }

    private void Block()
    {
        while (!Check(NeoTokenKind.RightBrace) && !AtEnd) Declaration();
        Consume(NeoTokenKind.RightBrace, "Expected '}' after block.");
    }

    private void RequireBlock(string message)
    {
        Consume(NeoTokenKind.LeftBrace, message);
        Block();
    }

    private void PatchLoop(LoopContext loop, int line)
    {
        int end = _code.Count;
        foreach (int jump in loop.Breaks) PatchJump(jump, end);
        int continueTarget = loop.ContinueTarget >= 0 ? loop.ContinueTarget : end;
        foreach (int jump in loop.Continues) PatchJump(jump, continueTarget);
        _loops.Pop();
        _ = line;
    }

    private void Expression() => Or();

    private void Or()
    {
        And();
        while (Match(NeoTokenKind.OrOr))
        {
            int line = Previous.Line;
            int jump = EmitJump(NeoOpCode.JumpIfTrue, line);
            Emit(NeoOpCode.Pop, line: line);
            And();
            PatchJump(jump);
        }
    }

    private void And()
    {
        Equality();
        while (Match(NeoTokenKind.AndAnd))
        {
            int line = Previous.Line;
            int jump = EmitJump(NeoOpCode.JumpIfFalse, line);
            Emit(NeoOpCode.Pop, line: line);
            Equality();
            PatchJump(jump);
        }
    }

    private void Equality()
    {
        Comparison();
        while (Match(NeoTokenKind.EqualEqual, NeoTokenKind.BangEqual))
        {
            var operation = Previous.Kind == NeoTokenKind.EqualEqual ? NeoOpCode.Equal : NeoOpCode.NotEqual;
            int line = Previous.Line;
            Comparison();
            Emit(operation, line: line);
        }
    }

    private void Comparison()
    {
        Term();
        while (Match(NeoTokenKind.Greater, NeoTokenKind.GreaterEqual, NeoTokenKind.Less, NeoTokenKind.LessEqual))
        {
            var operation = Previous.Kind switch
            {
                NeoTokenKind.Greater => NeoOpCode.Greater,
                NeoTokenKind.GreaterEqual => NeoOpCode.GreaterEqual,
                NeoTokenKind.Less => NeoOpCode.Less,
                _ => NeoOpCode.LessEqual
            };
            int line = Previous.Line;
            Term();
            Emit(operation, line: line);
        }
    }

    private void Term()
    {
        Factor();
        while (Match(NeoTokenKind.Plus, NeoTokenKind.Minus))
        {
            var operation = Previous.Kind == NeoTokenKind.Plus ? NeoOpCode.Add : NeoOpCode.Subtract;
            int line = Previous.Line;
            Factor();
            Emit(operation, line: line);
        }
    }

    private void Factor()
    {
        Unary();
        while (Match(NeoTokenKind.Star, NeoTokenKind.Slash, NeoTokenKind.Percent))
        {
            var operation = Previous.Kind switch
            {
                NeoTokenKind.Star => NeoOpCode.Multiply,
                NeoTokenKind.Slash => NeoOpCode.Divide,
                _ => NeoOpCode.Modulo
            };
            int line = Previous.Line;
            Unary();
            Emit(operation, line: line);
        }
    }

    private void Unary()
    {
        if (Match(NeoTokenKind.Bang, NeoTokenKind.Minus))
        {
            var operation = Previous.Kind == NeoTokenKind.Bang ? NeoOpCode.Not : NeoOpCode.Negate;
            int line = Previous.Line;
            Unary();
            Emit(operation, line: line);
            return;
        }
        Postfix();
    }

    private void Postfix()
    {
        Primary();
        while (true)
        {
            if (Match(NeoTokenKind.LeftBracket))
            {
                Expression();
                Consume(NeoTokenKind.RightBracket, "Expected ']' after index.");
                Emit(NeoOpCode.GetIndex, line: Previous.Line);
            }
            else if (Match(NeoTokenKind.Dot))
            {
                var property = Consume(NeoTokenKind.Identifier, "Expected property name.");
                Emit(NeoOpCode.GetProperty, property.Text, property.Line);
            }
            else
            {
                return;
            }
        }
    }

    private void Primary()
    {
        if (Match(NeoTokenKind.Number)) { Emit(NeoOpCode.Constant, Previous.Value, Previous.Line); return; }
        if (Match(NeoTokenKind.String)) { Emit(NeoOpCode.Constant, Previous.Value, Previous.Line); return; }
        if (Match(NeoTokenKind.True)) { Emit(NeoOpCode.True, line: Previous.Line); return; }
        if (Match(NeoTokenKind.False)) { Emit(NeoOpCode.False, line: Previous.Line); return; }
        if (Match(NeoTokenKind.Null)) { Emit(NeoOpCode.Null, line: Previous.Line); return; }
        if (Match(NeoTokenKind.LeftParen))
        {
            Expression();
            Consume(NeoTokenKind.RightParen, "Expected ')' after expression.");
            return;
        }
        if (Match(NeoTokenKind.LeftBracket))
        {
            int count = 0;
            if (!Check(NeoTokenKind.RightBracket))
            {
                do
                {
                    Expression();
                    count++;
                } while (Match(NeoTokenKind.Comma));
            }
            Consume(NeoTokenKind.RightBracket, "Expected ']' after array.");
            Emit(NeoOpCode.BuildArray, count, Previous.Line);
            return;
        }
        if (Match(NeoTokenKind.Identifier))
        {
            var name = Previous;
            if (Match(NeoTokenKind.LeftParen))
            {
                int count = 0;
                if (!Check(NeoTokenKind.RightParen))
                {
                    do
                    {
                        Expression();
                        count++;
                    } while (Match(NeoTokenKind.Comma));
                }
                Consume(NeoTokenKind.RightParen, "Expected ')' after arguments.");
                Emit(NeoOpCode.Call, new CallInfo(name.Text, count), name.Line);
            }
            else
            {
                EmitGet(name);
            }
            return;
        }
        throw Error(Peek, "Expected an expression.");
    }

    private void EmitGet(NeoToken name) =>
        Emit(_inFunction && _locals.Contains(name.Text) ? NeoOpCode.GetLocal : NeoOpCode.GetGlobal, name.Text, name.Line);

    private void EmitSet(NeoToken name) =>
        Emit(_inFunction && _locals.Contains(name.Text) ? NeoOpCode.SetLocal : NeoOpCode.SetGlobal, name.Text, name.Line);

    private int EmitJump(NeoOpCode code, int line)
    {
        int position = _code.Count;
        Emit(code, -1, line);
        return position;
    }

    private void PatchJump(int instruction, int? target = null)
    {
        var previous = _code[instruction];
        _code[instruction] = previous with { Operand = target ?? _code.Count };
    }

    private void EmitLoop(int target, int line) => Emit(NeoOpCode.Loop, target, line);
    private void Emit(NeoOpCode code, object? operand = null, int line = 1) => _code.Add(new NeoInstruction(code, operand, line));

    private bool Match(params NeoTokenKind[] kinds)
    {
        foreach (var kind in kinds)
        {
            if (!Check(kind)) continue;
            Advance();
            return true;
        }
        return false;
    }

    private NeoToken Consume(NeoTokenKind kind, string message)
    {
        if (Check(kind)) return Advance();
        throw Error(Peek, message);
    }

    private bool Check(NeoTokenKind kind) => !AtEnd && Peek.Kind == kind;
    private bool AtEnd => Peek.Kind == NeoTokenKind.End;
    private NeoToken Peek => _tokens[_current];
    private NeoToken Previous => _tokens[_current - 1];
    private NeoToken Advance() { if (!AtEnd) _current++; return Previous; }
    private NeoLanguageException Error(NeoToken token, string message) => new(message, token.Line);
    internal sealed record CallInfo(string Name, int ArgumentCount);
}

internal sealed class NeoVirtualMachine(NeoProgram program, StringBuilder output, string? sourcePath)
{
    private sealed class Frame(IReadOnlyList<NeoInstruction> code, string? functionName = null)
    {
        public IReadOnlyList<NeoInstruction> Code { get; } = code;
        public string? FunctionName { get; } = functionName;
        public Dictionary<string, object?> Locals { get; } = new(StringComparer.Ordinal);
        public int Ip { get; set; }
    }

    private readonly Dictionary<string, object?> _globals = new(StringComparer.Ordinal);
    private readonly HashSet<string> _constantGlobals = new(StringComparer.Ordinal);
    private readonly Stack<Frame> _frames = new();
    private readonly List<object?> _stack = [];
    private const int MaximumInstructions = 2_000_000;
    private const int MaximumCallDepth = 128;
    private const int MaximumOutputCharacters = 128 * 1024;

    public void Execute()
    {
        _frames.Push(new Frame(program.Main));
        int instructions = 0;
        while (_frames.Count > 0)
        {
            if (++instructions > MaximumInstructions)
                throw new InvalidOperationException("Neo++ instruction limit reached (2,000,000).");

            var frame = _frames.Peek();
            if (frame.Ip >= frame.Code.Count)
            {
                _frames.Pop();
                continue;
            }

            var instruction = frame.Code[frame.Ip++];
            try
            {
                ExecuteInstruction(frame, instruction);
            }
            catch (NeoLanguageException)
            {
                throw;
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or IndexOutOfRangeException or DivideByZeroException or KeyNotFoundException or FormatException)
            {
                throw new NeoLanguageException(exception.Message, instruction.Line);
            }
        }
    }

    private void ExecuteInstruction(Frame frame, NeoInstruction instruction)
    {
        switch (instruction.Code)
        {
            case NeoOpCode.Constant: _stack.Add(instruction.Operand); break;
            case NeoOpCode.Null: _stack.Add(null); break;
            case NeoOpCode.True: _stack.Add(true); break;
            case NeoOpCode.False: _stack.Add(false); break;
            case NeoOpCode.DefineGlobal:
            case NeoOpCode.DefineGlobalConst:
                _globals[(string)instruction.Operand!] = Pop();
                if (instruction.Code == NeoOpCode.DefineGlobalConst)
                    _constantGlobals.Add((string)instruction.Operand!);
                break;
            case NeoOpCode.GetGlobal:
                if (!_globals.TryGetValue((string)instruction.Operand!, out var global))
                    throw new InvalidOperationException($"Undefined variable '{instruction.Operand}'.");
                _stack.Add(global);
                break;
            case NeoOpCode.SetGlobal:
                if (_constantGlobals.Contains((string)instruction.Operand!))
                    throw new InvalidOperationException($"Cannot assign to constant '{instruction.Operand}'.");
                if (!_globals.ContainsKey((string)instruction.Operand!))
                    throw new InvalidOperationException($"Undefined variable '{instruction.Operand}'.");
                _globals[(string)instruction.Operand!] = Peek();
                break;
            case NeoOpCode.GetLocal:
                if (!frame.Locals.TryGetValue((string)instruction.Operand!, out var local))
                    throw new InvalidOperationException($"Undefined local '{instruction.Operand}'.");
                _stack.Add(local);
                break;
            case NeoOpCode.SetLocal:
                frame.Locals[(string)instruction.Operand!] = Peek();
                break;
            case NeoOpCode.Pop: Pop(); break;
            case NeoOpCode.Print: AppendOutput(Format(Pop()), instruction.Line); break;
            case NeoOpCode.Add: Binary((left, right) => left is string || right is string ? Format(left) + Format(right) : Number(left) + Number(right)); break;
            case NeoOpCode.Subtract: Binary((left, right) => Number(left) - Number(right)); break;
            case NeoOpCode.Multiply: Binary((left, right) => Number(left) * Number(right)); break;
            case NeoOpCode.Divide: Binary((left, right) => Number(left) / Number(right)); break;
            case NeoOpCode.Modulo: Binary((left, right) => Number(left) % Number(right)); break;
            case NeoOpCode.Negate: _stack.Add(-Number(Pop())); break;
            case NeoOpCode.Not: _stack.Add(!Truthy(Pop())); break;
            case NeoOpCode.Equal: Binary((left, right) => Equals(left, right)); break;
            case NeoOpCode.NotEqual: Binary((left, right) => !Equals(left, right)); break;
            case NeoOpCode.Greater: Binary((left, right) => Compare(left, right) > 0); break;
            case NeoOpCode.GreaterEqual: Binary((left, right) => Compare(left, right) >= 0); break;
            case NeoOpCode.Less: Binary((left, right) => Compare(left, right) < 0); break;
            case NeoOpCode.LessEqual: Binary((left, right) => Compare(left, right) <= 0); break;
            case NeoOpCode.And: Binary((left, right) => Truthy(left) && Truthy(right)); break;
            case NeoOpCode.Or: Binary((left, right) => Truthy(left) || Truthy(right)); break;
            case NeoOpCode.Jump: frame.Ip = (int)instruction.Operand!; break;
            case NeoOpCode.JumpIfFalse: if (!Truthy(Peek())) frame.Ip = (int)instruction.Operand!; break;
            case NeoOpCode.JumpIfTrue: if (Truthy(Peek())) frame.Ip = (int)instruction.Operand!; break;
            case NeoOpCode.Loop: frame.Ip = (int)instruction.Operand!; break;
            case NeoOpCode.BuildArray:
            {
                int count = (int)instruction.Operand!;
                var values = new object?[count];
                for (int i = count - 1; i >= 0; i--) values[i] = Pop();
                _stack.Add(values);
                break;
            }
            case NeoOpCode.GetIndex:
            {
                int index = checked((int)Number(Pop()));
                var target = Pop();
                if (target is object?[] array) _stack.Add(array[index]);
                else if (target is string text) _stack.Add(text[index].ToString());
                else throw new InvalidOperationException("Only arrays and strings can be indexed.");
                break;
            }
            case NeoOpCode.GetProperty:
            {
                var target = Pop();
                string property = (string)instruction.Operand!;
                if (property == "length" && target is object?[] array) _stack.Add((double)array.Length);
                else if (property == "length" && target is string text) _stack.Add((double)text.Length);
                else throw new InvalidOperationException($"Property '{property}' is not available on this value.");
                break;
            }
            case NeoOpCode.Call: Call((NeoCompiler.CallInfo)instruction.Operand!, instruction.Line); break;
            case NeoOpCode.Return:
            {
                var result = Pop();
                _frames.Pop();
                if (_frames.Count == 0) return;
                _stack.Add(result);
                break;
            }
            default: throw new InvalidOperationException($"Unknown NVM opcode {instruction.Code}.");
        }
    }

    private void Call(NeoCompiler.CallInfo call, int line)
    {
        var arguments = new object?[call.ArgumentCount];
        for (int i = arguments.Length - 1; i >= 0; i--) arguments[i] = Pop();

        if (call.Name == "len")
        {
            if (arguments.Length != 1) throw new NeoLanguageException("len() expects one argument.", line);
            _stack.Add(arguments[0] switch
            {
                object?[] array => (double)array.Length,
                string text => (double)text.Length,
                _ => throw new NeoLanguageException("len() expects an array or string.", line)
            });
            return;
        }

        if (call.Name == "contains")
        {
            if (arguments.Length != 2 || arguments.Any(argument => argument is not string))
                throw new NeoLanguageException("contains() expects two strings.", line);
            _stack.Add(((string)arguments[0]!).Contains((string)arguments[1]!, StringComparison.Ordinal));
            return;
        }

        if (call.Name == "java")
        {
            if (arguments.Length < 1 || arguments.Any(argument => argument is not string))
                throw new NeoLanguageException("java() expects a Java source path followed by optional string arguments.", line);
            string workingDirectory = sourcePath is null
                ? Environment.CurrentDirectory
                : Path.GetDirectoryName(Path.GetFullPath(sourcePath))!;
            var javaResult = NeoJavaBridge.RunSource((string)arguments[0]!, arguments.Skip(1).Cast<string>(), workingDirectory);
            if (javaResult.ExitCode != 0)
                throw new NeoLanguageException("Java bridge failed: " + javaResult.Error, line);
            _stack.Add(javaResult.Output.TrimEnd('\r', '\n'));
            return;
        }

        if (!program.Functions.TryGetValue(call.Name, out var function))
            throw new NeoLanguageException($"Unknown function '{call.Name}'.", line);
        if (function.Parameters.Count != arguments.Length)
            throw new NeoLanguageException($"Function '{call.Name}' expects {function.Parameters.Count} arguments.", line);
        if (_frames.Count >= MaximumCallDepth)
            throw new NeoLanguageException($"Maximum call depth ({MaximumCallDepth}) exceeded.", line);

        var frame = new Frame(function.Code, function.Name);
        for (int i = 0; i < arguments.Length; i++) frame.Locals[function.Parameters[i]] = arguments[i];
        _frames.Push(frame);
    }

    private void Binary(Func<object?, object?, object?> operation)
    {
        var right = Pop();
        var left = Pop();
        _stack.Add(operation(left, right));
    }

    private void AppendOutput(string value, int line)
    {
        if (output.Length + value.Length + Environment.NewLine.Length > MaximumOutputCharacters)
            throw new NeoLanguageException("Neo++ output limit reached (128 KB).", line);
        output.AppendLine(value);
    }

    private object? Pop()
    {
        if (_stack.Count == 0) throw new InvalidOperationException("NVM stack underflow.");
        int index = _stack.Count - 1;
        var value = _stack[index];
        _stack.RemoveAt(index);
        return value;
    }

    private object? Peek() => _stack.Count == 0 ? throw new InvalidOperationException("NVM stack underflow.") : _stack[^1];

    private static double Number(object? value) => value is double number
        ? number
        : throw new InvalidOperationException($"Expected a number, got {Format(value)}.");

    private static bool Truthy(object? value) => value switch
    {
        null => false,
        bool boolean => boolean,
        double number => number != 0,
        string text => text.Length > 0,
        _ => true
    };

    private static int Compare(object? left, object? right)
    {
        if (left is string leftText && right is string rightText)
            return string.Compare(leftText, rightText, StringComparison.Ordinal);
        return Number(left).CompareTo(Number(right));
    }

    private static string Format(object? value) => value switch
    {
        null => "null",
        bool boolean => boolean ? "true" : "false",
        double number => number.ToString("G", CultureInfo.InvariantCulture),
        object?[] array => "[" + string.Join(", ", array.Select(Format)) + "]",
        _ => value.ToString() ?? string.Empty
    };
}
