package com.neojava.neo;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;

public class NeoInterpreter {
    private final Map<String, Object> variables = new HashMap<>();
    private final Map<String, FunctionDef> functions = new HashMap<>();
    private final List<String> output = new ArrayList<>();

    public List<String> runFromFile(Path path) throws IOException {
        String source = Files.readString(path);
        return run(source);
    }

    public List<String> run(String source) {
        output.clear();
        Lexer lexer = new Lexer(source);
        Parser parser = new Parser(lexer.tokenize());
        List<Stmt> statements = parser.parseProgram();
        for (Stmt stmt : statements) {
            stmt.execute(this);
        }
        return new ArrayList<>(output);
    }

    public void printValue(Object value) {
        if (value == null) {
            output.add("null");
            return;
        }
        if (value instanceof Double number) {
            if (Math.rint(number) == number) {
                output.add(Long.toString(Math.round(number)));
            } else {
                output.add(String.valueOf(number));
            }
            return;
        }
        output.add(String.valueOf(value));
    }

    public Object getVariable(String name) {
        if (!variables.containsKey(name)) {
            throw new IllegalArgumentException("Unknown variable: " + name);
        }
        return variables.get(name);
    }

    public void setVariable(String name, Object value) {
        variables.put(name, value);
    }

    public void defineFunction(String name, FunctionDef fn) {
        functions.put(name, fn);
    }

    public FunctionDef getFunction(String name) {
        return functions.get(name);
    }

    public boolean hasFunction(String name) {
        return functions.containsKey(name);
    }

    public List<String> getOutput() {
        return new ArrayList<>(output);
    }

    public static final class FunctionDef {
        public final List<String> parameters;
        public final List<Stmt> body;

        public FunctionDef(List<String> parameters, List<Stmt> body) {
            this.parameters = parameters;
            this.body = body;
        }
    }

    interface Stmt {
        void execute(NeoInterpreter interpreter);
    }

    interface Expr {
        Object evaluate(NeoInterpreter interpreter);
    }

    static class PrintStmt implements Stmt {
        private final Expr expression;

        PrintStmt(Expr expression) {
            this.expression = expression;
        }

        @Override
        public void execute(NeoInterpreter interpreter) {
            interpreter.printValue(expression.evaluate(interpreter));
        }
    }

    static class LetStmt implements Stmt {
        private final String name;
        private final Expr expression;

        LetStmt(String name, Expr expression) {
            this.name = name;
            this.expression = expression;
        }

        @Override
        public void execute(NeoInterpreter interpreter) {
            interpreter.setVariable(name, expression.evaluate(interpreter));
        }
    }

    static class AssignmentStmt implements Stmt {
        private final String name;
        private final Expr expression;

        AssignmentStmt(String name, Expr expression) {
            this.name = name;
            this.expression = expression;
        }

        @Override
        public void execute(NeoInterpreter interpreter) {
            interpreter.setVariable(name, expression.evaluate(interpreter));
        }
    }

    static class BreakStmt implements Stmt {
        @Override
        public void execute(NeoInterpreter interpreter) {
            throw new BreakSignal();
        }
    }

    static class ContinueStmt implements Stmt {
        @Override
        public void execute(NeoInterpreter interpreter) {
            throw new ContinueSignal();
        }
    }

    static class BlockStmt implements Stmt {
        private final List<Stmt> statements;

        BlockStmt(List<Stmt> statements) {
            this.statements = statements;
        }

        @Override
        public void execute(NeoInterpreter interpreter) {
            for (Stmt stmt : statements) {
                stmt.execute(interpreter);
            }
        }
    }

    static class IfStmt implements Stmt {
        private final Expr condition;
        private final Stmt thenBranch;
        private final Stmt elseBranch;

        IfStmt(Expr condition, Stmt thenBranch, Stmt elseBranch) {
            this.condition = condition;
            this.thenBranch = thenBranch;
            this.elseBranch = elseBranch;
        }

        @Override
        public void execute(NeoInterpreter interpreter) {
            Object result = condition.evaluate(interpreter);
            boolean isTrue = isTruthy(result);
            if (isTrue) {
                thenBranch.execute(interpreter);
            } else if (elseBranch != null) {
                elseBranch.execute(interpreter);
            }
        }
    }

    static class LoopStmt implements Stmt {
        private final String variable;
        private final Expr start;
        private final Expr end;
        private final Stmt body;

        LoopStmt(String variable, Expr start, Expr end, Stmt body) {
            this.variable = variable;
            this.start = start;
            this.end = end;
            this.body = body;
        }

        @Override
        public void execute(NeoInterpreter interpreter) {
            int from = toInt(start.evaluate(interpreter));
            int to = toInt(end.evaluate(interpreter));
            int step = from <= to ? 1 : -1;
            for (int i = from; step > 0 ? i <= to : i >= to; i += step) {
                interpreter.setVariable(variable, i);
                try {
                    body.execute(interpreter);
                } catch (ContinueSignal ignored) {
                    continue;
                } catch (BreakSignal ignored) {
                    break;
                }
            }
        }
    }

    static class WhileStmt implements Stmt {
        private final Expr condition;
        private final Stmt body;

        WhileStmt(Expr condition, Stmt body) {
            this.condition = condition;
            this.body = body;
        }

        @Override
        public void execute(NeoInterpreter interpreter) {
            while (isTruthy(condition.evaluate(interpreter))) {
                try {
                    body.execute(interpreter);
                } catch (ContinueSignal ignored) {
                    continue;
                } catch (BreakSignal ignored) {
                    break;
                }
            }
        }
    }

    static class DoWhileStmt implements Stmt {
        private final Expr condition;
        private final Stmt body;

        DoWhileStmt(Expr condition, Stmt body) {
            this.condition = condition;
            this.body = body;
        }

        @Override
        public void execute(NeoInterpreter interpreter) {
            do {
                try {
                    body.execute(interpreter);
                } catch (ContinueSignal ignored) {
                    continue;
                } catch (BreakSignal ignored) {
                    break;
                }
            } while (isTruthy(condition.evaluate(interpreter)));
        }
    }

    static class ForStmt implements Stmt {
        private final Stmt initializer;
        private final Expr condition;
        private final Stmt update;
        private final Stmt body;

        ForStmt(Stmt initializer, Expr condition, Stmt update, Stmt body) {
            this.initializer = initializer;
            this.condition = condition;
            this.update = update;
            this.body = body;
        }

        @Override
        public void execute(NeoInterpreter interpreter) {
            if (initializer != null) {
                initializer.execute(interpreter);
            }
            try {
                while (isTruthy(condition.evaluate(interpreter))) {
                    try {
                        body.execute(interpreter);
                    } catch (ContinueSignal ignored) {
                        // continue to update and condition check
                    } catch (BreakSignal ignored) {
                        break;
                    }
                    if (update != null) {
                        update.execute(interpreter);
                    }
                }
            } catch (BreakSignal ignored) {
                // handled in loop body
            }
        }
    }

    static class SwitchStmt implements Stmt {
        private final Expr expression;
        private final List<CaseClause> cases;

        SwitchStmt(Expr expression, List<CaseClause> cases) {
            this.expression = expression;
            this.cases = cases;
        }

        @Override
        public void execute(NeoInterpreter interpreter) {
            Object value = expression.evaluate(interpreter);
            boolean matchedCase = false;
            try {
                for (CaseClause clause : cases) {
                    if (clause.value == null) {
                        if (!matchedCase) {
                            clause.body.execute(interpreter);
                        }
                        continue;
                    }
                    Object caseValue = clause.value.evaluate(interpreter);
                    if (Objects.equals(value, caseValue)) {
                        matchedCase = true;
                        clause.body.execute(interpreter);
                        break;
                    }
                }
            } catch (BreakSignal ignored) {
                // stop switch
            }
        }
    }

    static class CaseClause {
        private final Expr value;
        private final Stmt body;

        CaseClause(Expr value, Stmt body) {
            this.value = value;
            this.body = body;
        }
    }

    static class FunctionStmt implements Stmt {
        private final String name;
        private final List<String> params;
        private final Stmt body;

        FunctionStmt(String name, List<String> params, Stmt body) {
            this.name = name;
            this.params = params;
            this.body = body;
        }

        @Override
        public void execute(NeoInterpreter interpreter) {
            interpreter.defineFunction(name, new FunctionDef(params, List.of(body)));
        }
    }

    static class ReturnStmt implements Stmt {
        private final Expr expr;

        ReturnStmt(Expr expr) {
            this.expr = expr;
        }

        @Override
        public void execute(NeoInterpreter interpreter) {
            throw new NeoReturnException(expr.evaluate(interpreter));
        }
    }

    static class ExprStmt implements Stmt {
        private final Expr expression;

        ExprStmt(Expr expression) {
            this.expression = expression;
        }

        @Override
        public void execute(NeoInterpreter interpreter) {
            expression.evaluate(interpreter);
        }
    }

    static class LiteralExpr implements Expr {
        private final Object value;

        LiteralExpr(Object value) {
            this.value = value;
        }

        @Override
        public Object evaluate(NeoInterpreter interpreter) {
            return value;
        }
    }

    static class VariableExpr implements Expr {
        private final String name;

        VariableExpr(String name) {
            this.name = name;
        }

        @Override
        public Object evaluate(NeoInterpreter interpreter) {
            return interpreter.getVariable(name);
        }
    }

    static class UnaryExpr implements Expr {
        private final String operator;
        private final Expr right;

        UnaryExpr(String operator, Expr right) {
            this.operator = operator;
            this.right = right;
        }

        @Override
        public Object evaluate(NeoInterpreter interpreter) {
            Object value = right.evaluate(interpreter);
            switch (operator) {
                case "-":
                    return -(toDouble(value));
                case "!":
                    return !isTruthy(value);
                default:
                    throw new IllegalArgumentException("Unknown unary operator: " + operator);
            }
        }
    }

    static class BinaryExpr implements Expr {
        private final Expr left;
        private final String operator;
        private final Expr right;

        BinaryExpr(Expr left, String operator, Expr right) {
            this.left = left;
            this.operator = operator;
            this.right = right;
        }

        @Override
        public Object evaluate(NeoInterpreter interpreter) {
            Object leftValue = left.evaluate(interpreter);
            Object rightValue = right.evaluate(interpreter);

            switch (operator) {
                case "+":
                    if (leftValue instanceof String || rightValue instanceof String) {
                        return String.valueOf(leftValue) + String.valueOf(rightValue);
                    }
                    return toDouble(leftValue) + toDouble(rightValue);
                case "-":
                    return toDouble(leftValue) - toDouble(rightValue);
                case "*":
                    return toDouble(leftValue) * toDouble(rightValue);
                case "/":
                    return toDouble(leftValue) / toDouble(rightValue);
                case "%":
                    return toDouble(leftValue) % toDouble(rightValue);
                case "==":
                    return Objects.equals(leftValue, rightValue);
                case "!=":
                    return !Objects.equals(leftValue, rightValue);
                case ">":
                    return compare(leftValue, rightValue) > 0;
                case ">=":
                    return compare(leftValue, rightValue) >= 0;
                case "<":
                    return compare(leftValue, rightValue) < 0;
                case "<=":
                    return compare(leftValue, rightValue) <= 0;
                case "&&":
                    return isTruthy(leftValue) && isTruthy(rightValue);
                case "||":
                    return isTruthy(leftValue) || isTruthy(rightValue);
                default:
                    throw new IllegalArgumentException("Unknown operator: " + operator);
            }
        }
    }

    static class ArrayLiteralExpr implements Expr {
        private final List<Expr> items;

        ArrayLiteralExpr(List<Expr> items) {
            this.items = items;
        }

        @Override
        public Object evaluate(NeoInterpreter interpreter) {
            List<Object> result = new ArrayList<>();
            for (Expr expr : items) {
                result.add(expr.evaluate(interpreter));
            }
            return result;
        }
    }

    static class IndexExpr implements Expr {
        private final Expr target;
        private final Expr index;

        IndexExpr(Expr target, Expr index) {
            this.target = target;
            this.index = index;
        }

        @Override
        public Object evaluate(NeoInterpreter interpreter) {
            Object value = target.evaluate(interpreter);
            int idx = toInt(index.evaluate(interpreter));
            if (value instanceof List<?> list) {
                return list.get(idx);
            }
            if (value instanceof String text) {
                return String.valueOf(text.charAt(idx));
            }
            throw new IllegalArgumentException("Indexing is only supported for arrays and strings");
        }
    }

    static class DotExpr implements Expr {
        private final Expr target;
        private final String name;

        DotExpr(Expr target, String name) {
            this.target = target;
            this.name = name;
        }

        @Override
        public Object evaluate(NeoInterpreter interpreter) {
            Object value = target.evaluate(interpreter);
            if (value instanceof List<?> list && "length".equals(name)) {
                return list.size();
            }
            if (value instanceof String text && "length".equals(name)) {
                return text.length();
            }
            throw new IllegalArgumentException("Unsupported property: " + name);
        }
    }

    static class CallExpr implements Expr {
        private final String name;
        private final List<Expr> arguments;

        CallExpr(String name, List<Expr> arguments) {
            this.name = name;
            this.arguments = arguments;
        }

        @Override
        public Object evaluate(NeoInterpreter interpreter) {
            FunctionDef function = interpreter.getFunction(name);
            if (function == null) {
                throw new IllegalArgumentException("Unknown function: " + name);
            }
            if (function.parameters.size() != arguments.size()) {
                throw new IllegalArgumentException("Function " + name + " expects " + function.parameters.size() + " arguments");
            }
            Map<String, Object> localContext = new HashMap<>(interpreter.variables);
            for (int i = 0; i < arguments.size(); i++) {
                localContext.put(function.parameters.get(i), arguments.get(i).evaluate(interpreter));
            }
            NeoInterpreter scope = new NeoInterpreter();
            scope.variables.putAll(localContext);
            scope.functions.putAll(interpreter.functions);
            try {
                for (Stmt stmt : function.body) {
                    stmt.execute(scope);
                }
            } catch (NeoReturnException ex) {
                return ex.value;
            }
            return null;
        }
    }

    static class NeoReturnException extends RuntimeException {
        private final Object value;

        NeoReturnException(Object value) {
            this.value = value;
        }
    }

    static class BreakSignal extends RuntimeException {
    }

    static class ContinueSignal extends RuntimeException {
    }

    static class Parser {
        private final List<Token> tokens;
        private int index = 0;

        Parser(List<Token> tokens) {
            this.tokens = tokens;
        }

        List<Stmt> parseProgram() {
            List<Stmt> statements = new ArrayList<>();
            while (!isAtEnd()) {
                if (check("}")) {
                    advance();
                    break;
                }
                Stmt stmt = parseStatement();
                if (stmt != null) {
                    statements.add(stmt);
                }
            }
            return statements;
        }

        private Stmt parseStatement() {
            if (match("let")) {
                String name = consume("IDENT", "Expected variable name").value;
                consume("=", "Expected '=' after variable name");
                return new LetStmt(name, parseExpression());
            }
            if (check("IDENT") && peekNext().type.equals("=")) {
                String name = consume("IDENT", "Expected variable name").value;
                consume("=", "Expected '=' after variable name");
                return new AssignmentStmt(name, parseExpression());
            }
            if (match("print")) {
                return new PrintStmt(parseExpression());
            }
            if (match("if")) {
                Expr condition = parseExpression();
                Stmt thenBranch = parseBlock();
                Stmt elseBranch = null;
                if (match("else")) {
                    elseBranch = parseBlock();
                }
                return new IfStmt(condition, thenBranch, elseBranch);
            }
            if (match("while")) {
                Expr condition = parseExpression();
                Stmt body = parseBlock();
                return new WhileStmt(condition, body);
            }
            if (match("do")) {
                Stmt body = parseBlock();
                consume("while", "Expected 'while' after do-block");
                Expr condition = parseExpression();
                return new DoWhileStmt(condition, body);
            }
            if (match("for")) {
                consume("(", "Expected '(' after for");
                Stmt initializer = null;
                if (match("let")) {
                    String name = consume("IDENT", "Expected variable name").value;
                    consume("=", "Expected '=' after variable name");
                    initializer = new LetStmt(name, parseExpression());
                } else if (check("IDENT") && peekNext().type.equals("=")) {
                    String name = consume("IDENT", "Expected variable name").value;
                    consume("=", "Expected '=' after variable name");
                    initializer = new AssignmentStmt(name, parseExpression());
                }
                consume(";", "Expected ';' after initializer");
                Expr condition = parseExpression();
                consume(";", "Expected ';' after condition");
                Stmt update = null;
                if (check("IDENT") && peekNext().type.equals("=")) {
                    String name = consume("IDENT", "Expected variable name").value;
                    consume("=", "Expected '=' after variable name");
                    update = new AssignmentStmt(name, parseExpression());
                }
                consume(")", "Expected ')' after for headers");
                Stmt body = parseBlock();
                return new ForStmt(initializer, condition, update, body);
            }
            if (match("switch")) {
                consume("(", "Expected '(' after switch");
                Expr expression = parseExpression();
                consume(")", "Expected ')' after switch expression");
                consume("{", "Expected '{' after switch");
                List<CaseClause> cases = new ArrayList<>();
                while (!check("}")) {
                    if (match("case")) {
                        Expr caseValue = parseExpression();
                        consume(":", "Expected ':' after case value");
                        List<Stmt> block = new ArrayList<>();
                        while (!check("}") && !check("case") && !check("default")) {
                            block.add(parseStatement());
                        }
                        cases.add(new CaseClause(caseValue, new BlockStmt(block)));
                    } else if (match("default")) {
                        consume(":", "Expected ':' after default");
                        List<Stmt> block = new ArrayList<>();
                        while (!check("}") && !check("case") && !check("default")) {
                            block.add(parseStatement());
                        }
                        cases.add(new CaseClause(null, new BlockStmt(block)));
                    } else {
                        throw new IllegalArgumentException("Unexpected token in switch: " + peek());
                    }
                }
                consume("}", "Expected '}' after switch");
                return new SwitchStmt(expression, cases);
            }
            if (match("loop")) {
                String variable = consume("IDENT", "Expected loop variable").value;
                consume("from", "Expected 'from' keyword");
                Expr start = parseExpression();
                consume("to", "Expected 'to' keyword");
                Expr end = parseExpression();
                Stmt body = parseBlock();
                return new LoopStmt(variable, start, end, body);
            }
            if (match("break")) {
                return new BreakStmt();
            }
            if (match("continue")) {
                return new ContinueStmt();
            }
            if (match("fn")) {
                String name = consume("IDENT", "Expected function name").value;
                consume("(", "Expected '(' after function name");
                List<String> params = new ArrayList<>();
                if (!check(")")) {
                    do {
                        params.add(consume("IDENT", "Expected parameter name").value);
                    } while (match(","));
                }
                consume(")", "Expected ')' after parameters");
                Stmt body = parseBlock();
                return new FunctionStmt(name, params, body);
            }
            if (match("return")) {
                return new ReturnStmt(parseExpression());
            }
            if (check("{")) {
                return parseBlock();
            }
            return new ExprStmt(parseExpression());
        }

        private Stmt parseBlock() {
            consume("{", "Expected '{'");
            List<Stmt> statements = new ArrayList<>();
            while (!check("}")) {
                if (isAtEnd()) {
                    throw new IllegalArgumentException("Unclosed block");
                }
                statements.add(parseStatement());
            }
            consume("}", "Expected '}'");
            return new BlockStmt(statements);
        }

        private Expr parseExpression() {
            return parseOr();
        }

        private Expr parseOr() {
            Expr expr = parseAnd();
            while (match("||")) {
                String operator = previous().value;
                Expr right = parseAnd();
                expr = new BinaryExpr(expr, operator, right);
            }
            return expr;
        }

        private Expr parseAnd() {
            Expr expr = parseEquality();
            while (match("&&")) {
                String operator = previous().value;
                Expr right = parseEquality();
                expr = new BinaryExpr(expr, operator, right);
            }
            return expr;
        }

        private Expr parseEquality() {
            Expr expr = parseComparison();
            while (match("==", "!=")) {
                String operator = previous().value;
                Expr right = parseComparison();
                expr = new BinaryExpr(expr, operator, right);
            }
            return expr;
        }

        private Expr parseComparison() {
            Expr expr = parseAdditive();
            while (match("<", ">", "<=", ">=")) {
                String operator = previous().value;
                Expr right = parseAdditive();
                expr = new BinaryExpr(expr, operator, right);
            }
            return expr;
        }

        private Expr parseAdditive() {
            Expr expr = parseMultiplicative();
            while (match("+", "-")) {
                String operator = previous().value;
                Expr right = parseMultiplicative();
                expr = new BinaryExpr(expr, operator, right);
            }
            return expr;
        }

        private Expr parseMultiplicative() {
            Expr expr = parseUnary();
            while (match("*", "/", "%")) {
                String operator = previous().value;
                Expr right = parseUnary();
                expr = new BinaryExpr(expr, operator, right);
            }
            return expr;
        }

        private Expr parseUnary() {
            if (match("-", "!")) {
                String operator = previous().value;
                return new UnaryExpr(operator, parseUnary());
            }
            return parsePostfix();
        }

        private Expr parsePostfix() {
            Expr expr = parsePrimary();
            while (true) {
                if (match("[")) {
                    Expr index = parseExpression();
                    consume("]", "Expected ']'");
                    expr = new IndexExpr(expr, index);
                } else if (match(".")) {
                    String name = consume("IDENT", "Expected property name").value;
                    expr = new DotExpr(expr, name);
                } else if (match("(")) {
                    List<Expr> args = new ArrayList<>();
                    if (!check(")")) {
                        do {
                            args.add(parseExpression());
                        } while (match(","));
                    }
                    consume(")", "Expected ')' after arguments");
                    if (expr instanceof VariableExpr var) {
                        expr = new CallExpr(var.name, args);
                    } else {
                        throw new IllegalArgumentException("Only function calls are supported");
                    }
                } else {
                    break;
                }
            }
            return expr;
        }

        private Expr parsePrimary() {
            if (match("NUMBER")) {
                return new LiteralExpr(Double.parseDouble(previous().value));
            }
            if (match("STRING")) {
                return new LiteralExpr(previous().value);
            }
            if (match("true")) {
                return new LiteralExpr(true);
            }
            if (match("false")) {
                return new LiteralExpr(false);
            }
            if (match("IDENT")) {
                String name = previous().value;
                return new VariableExpr(name);
            }
            if (match("(")) {
                Expr expr = parseExpression();
                consume(")", "Expected ')'");
                return expr;
            }
            if (match("[")) {
                List<Expr> items = new ArrayList<>();
                if (!check("]")) {
                    do {
                        items.add(parseExpression());
                    } while (match(","));
                }
                consume("]", "Expected ']'");
                return new ArrayLiteralExpr(items);
            }
            throw new IllegalArgumentException("Unexpected token: " + peek());
        }

        private boolean match(String... types) {
            for (String type : types) {
                if (check(type)) {
                    advance();
                    return true;
                }
            }
            return false;
        }

        private boolean check(String type) {
            if (isAtEnd()) {
                return false;
            }
            return tokens.get(index).type.equals(type);
        }

        private Token advance() {
            if (!isAtEnd()) {
                index++;
            }
            return previous();
        }

        private boolean isAtEnd() {
            return index >= tokens.size();
        }

        private Token peek() {
            return tokens.get(index);
        }

        private Token peekNext() {
            if (index + 1 >= tokens.size()) {
                return new Token("EOF", "EOF");
            }
            return tokens.get(index + 1);
        }

        private Token previous() {
            return tokens.get(index - 1);
        }

        private Token consume(String type, String message) {
            if (!check(type)) {
                throw new IllegalArgumentException(message + ". Found: " + (isAtEnd() ? "EOF" : peek()));
            }
            return advance();
        }
    }

    static class Lexer {
        private final String source;
        private final List<Token> tokens = new ArrayList<>();
        private int index = 0;

        Lexer(String source) {
            this.source = source;
        }

        List<Token> tokenize() {
            StringBuilder builder = new StringBuilder();
            while (index < source.length()) {
                char ch = source.charAt(index);
                if (Character.isWhitespace(ch)) {
                    index++;
                    continue;
                }
                if (ch == '#') {
                    while (index < source.length() && source.charAt(index) != '\n') {
                        index++;
                    }
                    continue;
                }
                if (ch == '"') {
                    index++;
                    while (index < source.length() && source.charAt(index) != '"') {
                        builder.append(source.charAt(index));
                        index++;
                    }
                    if (index >= source.length()) {
                        throw new IllegalArgumentException("Unterminated string literal");
                    }
                    index++;
                    tokens.add(new Token("STRING", builder.toString()));
                    builder.setLength(0);
                    continue;
                }
                if (Character.isDigit(ch) || (ch == '.' && index + 1 < source.length() && Character.isDigit(source.charAt(index + 1)))) {
                    while (index < source.length() && (Character.isDigit(source.charAt(index)) || source.charAt(index) == '.')) {
                        builder.append(source.charAt(index));
                        index++;
                    }
                    tokens.add(new Token("NUMBER", builder.toString()));
                    builder.setLength(0);
                    continue;
                }
                if (Character.isLetter(ch) || ch == '_') {
                    while (index < source.length() && (Character.isLetterOrDigit(source.charAt(index)) || source.charAt(index) == '_')) {
                        builder.append(source.charAt(index));
                        index++;
                    }
                    String word = builder.toString();
                    String type = switch (word) {
                        case "let", "print", "if", "else", "while", "do", "for", "loop", "from", "to", "fn", "return", "break", "continue", "switch", "case", "default", "true", "false", "and", "or", "not" -> word;
                        default -> "IDENT";
                    };
                    tokens.add(new Token(type, word));
                    builder.setLength(0);
                    continue;
                }
                if (index + 1 < source.length()) {
                    String two = source.substring(index, index + 2);
                    if ("==".equals(two) || "!=".equals(two) || ">=".equals(two) || "<=".equals(two) || "&&".equals(two) || "||".equals(two)) {
                        tokens.add(new Token(two, two));
                        index += 2;
                        continue;
                    }
                }
                if ("+-*/%<>=(){}[];,:".indexOf(ch) >= 0) {
                    tokens.add(new Token(String.valueOf(ch), String.valueOf(ch)));
                    index++;
                    continue;
                }
                if (ch == '=') {
                    tokens.add(new Token("=", "="));
                    index++;
                    continue;
                }
                throw new IllegalArgumentException("Unsupported character: " + ch);
            }
            return tokens;
        }
    }

    private static boolean isTruthy(Object value) {
        if (value == null) {
            return false;
        }
        if (value instanceof Boolean bool) {
            return bool;
        }
        if (value instanceof Number number) {
            return number.doubleValue() != 0;
        }
        if (value instanceof String text) {
            return !text.isEmpty();
        }
        return true;
    }

    private static int compare(Object left, Object right) {
        double leftValue = toDouble(left);
        double rightValue = toDouble(right);
        return Double.compare(leftValue, rightValue);
    }

    private static double toDouble(Object value) {
        if (value instanceof Number number) {
            return number.doubleValue();
        }
        if (value instanceof String text) {
            return Double.parseDouble(text);
        }
        throw new IllegalArgumentException("Expected number but got: " + value);
    }

    private static int toInt(Object value) {
        if (value instanceof Number number) {
            return number.intValue();
        }
        throw new IllegalArgumentException("Expected integer but got: " + value);
    }
}
