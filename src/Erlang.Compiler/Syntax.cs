using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

public sealed class CompileException(string code, string message, int offset) : Exception(message)
{
    public string Code { get; } = code;
    public int Offset { get; } = offset;
}
public sealed record Token(string Kind, string Text, int Start, int End);
public static class Lexer
{
    private static readonly HashSet<string> Keywords = ["after", "begin", "case", "try", "cond", "catch", "andalso", "orelse", "end", "fun", "if", "let", "of", "receive", "when", "bnot", "not", "div", "rem", "band", "and", "bor", "bxor", "bsl", "bsr", "or", "xor", "maybe", "else"];
    public static List<Token> Scan(string text, bool blockPrefix = false)
    {
        var tokens = new List<Token>(); int i = 0, depth = 0;
        string[] symbols = ["=:=", "=/=", "->", "==", "/=", "=<", ">=", "++", "--", "<<", ">>", "=>", ":="];
        while (i < text.Length)
        {
            if (char.IsWhiteSpace(text[i])) { i++; continue; }
            if (text[i] == '%') { while (i < text.Length && text[i] != '\n') i++; continue; }
            int start = i; char c = text[i++];
            if (c is '\'' or '"')
            {
                var value = new StringBuilder(); bool closed = false;
                while (i < text.Length) { char ch = text[i++]; if (ch == c) { closed = true; break; } if (ch == '\\') { if (i == text.Length) break; ch = text[i++]; if (ch is 'x' or '^' || char.IsDigit(ch)) throw new CompileException("ERL003", "Hex, control and octal escapes are not implemented yet", i - 2); ch = ch switch { 'n' => '\n', 'r' => '\r', 't' => '\t', 'b' => '\b', 'f' => '\f', 'v' => '\v', 'e' => '\x1b', 's' => ' ', _ => ch }; } value.Append(ch); }
                if (!closed) throw new CompileException("ERL001", "Unterminated quoted literal", start);
                string literal = value.ToString();
                for (int offset = 0; offset < literal.Length;)
                {
                    if (!Rune.TryGetRuneAt(literal, offset, out var rune) || rune.Value is 0xfffe or 0xffff)
                        throw new CompileException("ERL001", "Illegal Unicode character in quoted literal", start);
                    offset += rune.Utf16SequenceLength;
                }
                tokens.Add(new(c == '\'' ? "quoted_atom" : "string", literal, start, i)); continue;
            }
            if (char.IsLetter(c) || c == '_')
            { while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] is '_' or '@')) i++; string name = text[start..i]; tokens.Add(new(char.IsUpper(c) || c == '_' ? "variable" : Keywords.Contains(name) ? "keyword" : "atom", name, start, i)); if (blockPrefix) { if (name is "receive" or "case" or "fun") depth++; else if (name == "end" && --depth == 0) break; } continue; }
            if (char.IsDigit(c))
            {
                while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '_')) i++;
                bool floating = false;
                if (i + 1 < text.Length && text[i] == '.' && char.IsDigit(text[i + 1])) { floating = true; i++; while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '_')) i++; }
                if (floating && i < text.Length && text[i] is 'e' or 'E') { i++; if (i < text.Length && text[i] is '+' or '-') i++; while (i < text.Length && char.IsDigit(text[i])) i++; }
                tokens.Add(new(floating ? "float" : "integer", text[start..i].Replace("_", ""), start, i)); continue;
            }
            var symbol = symbols.FirstOrDefault(s => text.AsSpan(start).StartsWith(s, StringComparison.Ordinal));
            if (symbol is not null) i = start + symbol.Length;
            tokens.Add(new("symbol", symbol ?? c.ToString(), start, i));
        }
        tokens.Add(new("eof", "", i, i)); return tokens;
    }
}
public abstract record Expr
{
    public sealed record Literal(Term Value) : Expr;
    public sealed record Variable(string Name) : Expr;
    public sealed record Tuple(IReadOnlyList<Expr> Items) : Expr;
    public sealed record List(IReadOnlyList<Expr> Items, Expr? Tail = null) : Expr;
    public sealed record Map(Expr? Base, IReadOnlyList<MapField> Fields) : Expr;
    public sealed record Bits(IReadOnlyList<BitSegment> Segments) : Expr;
    public sealed record Call(string? Module, string Function, IReadOnlyList<Expr> Arguments) : Expr;
    public sealed record Apply(Expr Function, IReadOnlyList<Expr> Arguments) : Expr;
    public sealed record Unary(string Operator, Expr Operand) : Expr;
    public sealed record Binary(string Operator, Expr Left, Expr Right) : Expr;
    public sealed record Match(Pattern Pattern, Expr Value) : Expr;
    public sealed record Sequence(IReadOnlyList<Expr> Items) : Expr;
    public sealed record Case(Expr Value, IReadOnlyList<Clause> Clauses) : Expr;
    public sealed record Receive(IReadOnlyList<Clause> Clauses, Expr? Timeout, Expr? After) : Expr;
    public sealed record Fun(IReadOnlyList<Clause> Clauses) : Expr;
    public sealed record GuardAlternatives(IReadOnlyList<Expr> Items) : Expr;
}
public sealed record MapField(Expr Key, Expr Value, bool Exact);
public sealed record BitSegment(Expr Value, Expr? Size, string Type = "integer", int Unit = 1, string Endian = "big", bool Signed = false);
public sealed record MapPatternField(Expr Key, Pattern Value);
public sealed record MapPattern(IReadOnlyList<MapPatternField> Fields) : Pattern
{
    protected override bool MatchCore(Term value, Dictionary<string, Term> bindings, ProcessContext? context = null, Dictionary<string, Term>? keyScope = null)
    {
        if (value is not MapTerm map) return false;
        // Resolve all keys before any value pattern binds variables.
        var keys = new Term[Fields.Count];
        try { for (int i = 0; i < keys.Length; i++) keys[i] = Execution.PatternKey(Fields[i].Key, keyScope ?? bindings, context); }
        catch (ErlangException) { return false; }
        for (int i = 0; i < keys.Length; i++)
        {
            if (!map.TryGet(keys[i], out var item) || !Fields[i].Value.Match(item!, bindings, context, keyScope)) return false;
        }
        return true;
    }
}
public sealed record Clause(IReadOnlyList<Pattern> Patterns, Expr? Guard, Expr Body);
public sealed record FunctionDefinition(string Name, int Arity, IReadOnlyList<Clause> Clauses);
public sealed record ModuleDefinition(string Name, IReadOnlyList<(string Name, int Arity)> Exports, IReadOnlyList<FunctionDefinition> Functions)
{
    public void Register(ModuleRegistry registry)
    { foreach (var export in Exports) registry.Register(Name, export.Name, export.Arity, (c, a) => Execution.InvokeAsync(this, export.Name, c, a)); }
}
public sealed class Parser
{
    private readonly List<Token> tokens; private int position;
    public Parser(string text, bool blockPrefix = false) => tokens = Lexer.Scan(text, blockPrefix);
    private Token Current => tokens[position];
    public int EndOffset => position == 0 ? 0 : tokens[position - 1].End;
    private bool Is(string value) => Current.Kind != "quoted_atom" && Current.Text == value;
    private bool Take(string value) { if (!Is(value)) return false; position++; return true; }
    private Token Expect(string value) { if (!Is(value)) throw Error($"Expected '{value}', found '{Current.Text}'"); return tokens[position++]; }
    private CompileException Error(string message) => new("ERL002", message, Current.Start);
    private string Name() { if (Current.Kind is not ("atom" or "quoted_atom")) throw Error("Expected atom"); return tokens[position++].Text; }
    public Expr ParseExpression(bool requireEnd = true)
    { var e = Expression(); if (requireEnd && Current.Kind != "eof") throw Error("Unexpected trailing token"); return e; }
    public ModuleDefinition ParseModule()
    {
        string? module = null; var exports = new List<(string, int)>(); var functions = new List<FunctionDefinition>();
        while (Current.Kind != "eof")
        {
            if (Take("-"))
            {
                string attr = Name(); Expect("(");
                if (attr == "module") { module = Name(); Expect(")"); Expect("."); }
                else if (attr == "export")
                { Expect("["); if (!Take("]")) { do { string name = Name(); Expect("/"); if (Current.Kind != "integer") throw Error("Expected arity"); int arity = int.Parse(tokens[position++].Text, CultureInfo.InvariantCulture); exports.Add((name, arity)); } while (Take(",")); Expect("]"); } Expect(")"); Expect("."); }
                else throw new CompileException("ERL003", $"Attribute '{attr}' is not supported yet", Current.Start);
                continue;
            }
            string fname = Name(); Expect("("); var patterns = PatternArguments(); var clauses = new List<Clause>(); int count = patterns.Count;
            clauses.Add(ParseClause(patterns));
            while (Take(";")) { if (Name() != fname) throw Error("Function clauses must have the same name"); Expect("("); patterns = PatternArguments(); if (patterns.Count != count) throw Error("Function clauses must have the same arity"); clauses.Add(ParseClause(patterns)); }
            Expect("."); functions.Add(new(fname, count, clauses));
        }
        if (module is null) throw Error("Missing -module attribute");
        var result = new ModuleDefinition(module, exports, functions); Semantics.Validate(result); return result;
    }
    private List<Pattern> PatternArguments()
    { var args = new List<Pattern>(); if (!Take(")")) { do { args.Add(ToPattern(Expression(2))); } while (Take(",")); Expect(")"); } return args; }
    private Clause ParseClause(IReadOnlyList<Pattern> patterns)
    {
        Expr? guard = null;
        if (Take("when"))
        {
            var alternatives = new List<Expr>();
            do
            { Expr conjunction = Expression(); while (Take(",")) conjunction = new Expr.Binary("andalso", conjunction, Expression()); alternatives.Add(conjunction); } while (Take(";"));
            guard = alternatives.Count == 1 ? alternatives[0] : new Expr.GuardAlternatives(alternatives);
        }
        Expect("->"); return new(patterns, guard, Body());
    }
    private Expr Body() { var body = new List<Expr> { Expression() }; while (Take(",")) body.Add(Expression()); return body.Count == 1 ? body[0] : new Expr.Sequence(body); }
    private List<Clause> Clauses()
    {
        var result = new List<Clause>();
        if (Is("end") || Is("after")) return result;
        do { result.Add(ParseClause([ToPattern(Expression(2))])); } while (Take(";")); return result;
    }
    private static int Precedence(string op) => op switch { "=" => 1, "!" => 2, "orelse" => 3, "andalso" => 4, "==" or "/=" or "=:=" or "=/=" or "<" or ">" or "=<" or ">=" => 5, "++" or "--" => 6, "+" or "-" => 7, "*" or "/" or "div" or "rem" => 8, _ => 0 };
    private Expr Expression(int minimum = 1)
    {
        Expr left = Primary();
        while (true)
        {
            int p = Current.Kind == "quoted_atom" ? 0 : Precedence(Current.Text); if (p < minimum || p == 0) break; string op = tokens[position++].Text;
            var right = Expression(op is "=" or "!" or "++" or "--" ? p : p + 1);
            left = op == "=" ? new Expr.Match(ToPattern(left), right) : new Expr.Binary(op, left, right);
        }
        return left;
    }
    private Expr Primary(bool bitSegment = false)
    {
        Expr result;
        if (Take("receive"))
        { var clauses = Clauses(); Expr? timeout = null, after = null; if (Take("after")) { timeout = Expression(); Expect("->"); after = Body(); } Expect("end"); return new Expr.Receive(clauses, timeout, after); }
        if (Take("case")) { var value = Expression(); Expect("of"); var clauses = Clauses(); if (clauses.Count == 0) throw Error("case needs a clause"); Expect("end"); return new Expr.Case(value, clauses); }
        if (Take("fun"))
        { var clauses = new List<Clause>(); do { Expect("("); clauses.Add(ParseClause(PatternArguments())); } while (Take(";")); Expect("end"); return new Expr.Fun(clauses); }
        if (Current.Kind != "quoted_atom" && Current.Text is "+" or "-" or "not") { string op = tokens[position++].Text; return new Expr.Unary(op, bitSegment ? Primary(true) : Expression(9)); }
        if (Take("(")) { result = Expression(); Expect(")"); }
        else if (Take("<<")) result = ParseBits();
        else if (Take("#")) result = ParseMap(null);
        else if (Take("{")) { var items = new List<Expr>(); if (!Take("}")) { do { items.Add(Expression()); } while (Take(",")); Expect("}"); } result = new Expr.Tuple(items); }
        else if (Take("["))
        { var items = new List<Expr>(); Expr? tail = null; if (!Take("]")) { do { items.Add(Expression()); } while (Take(",")); if (Take("|")) tail = Expression(); Expect("]"); } result = new Expr.List(items, tail); }
        else
        {
            var token = Current; position++;
            result = token.Kind switch { "integer" => new Expr.Literal(new Integer(BigInteger.Parse(token.Text, CultureInfo.InvariantCulture))), "float" => new Expr.Literal(new FloatTerm(double.Parse(token.Text, CultureInfo.InvariantCulture))), "string" => new Expr.Literal(Term.String(token.Text)), "variable" => new Expr.Variable(token.Text), "atom" or "quoted_atom" => new Expr.Literal(Term.A(token.Text)), _ => throw new CompileException("ERL003", $"Unsupported expression '{token.Text}'", token.Start) };
        }
        while (true)
        {
            if (bitSegment) break;
            if (Take("#")) result = ParseMap(result);
            else if (Take(":"))
            { if (result is not Expr.Literal { Value: Atom module }) throw Error("Dynamic module calls are not supported yet"); string name = Name(); Expect("("); result = new Expr.Call(module.Name, name, Arguments()); }
            else if (Take("("))
            { var args = Arguments(); result = result is Expr.Literal { Value: Atom fn } ? new Expr.Call(null, fn.Name, args) : new Expr.Apply(result, args); }
            else break;
        }
        return result;
    }
    private Expr ParseBits()
    {
        var segments = new List<BitSegment>();
        if (Take(">>")) return new Expr.Bits(segments);
        do
        {
            var value = Primary(true); Expr? size = null;
            if (Take(":"))
            {
                if (Current.Text is "+" or "-" or "not" or "bnot") throw Error("Unary bit segment sizes must be parenthesized");
                size = Primary(true);
            }
            string type = "integer", endian = "big"; int? unit = null; bool signed = false;
            var categories = new Dictionary<string, string>();
            void Merge(string category, string setting)
            { if (categories.TryGetValue(category, out var previous) && previous != setting) throw Error($"Conflicting bit segment {category} specifiers"); categories[category] = setting; }
            if (Take("/"))
            {
                do
                {
                    string spec = Name(); string category;
                    switch (spec)
                    {
                        case "integer": case "binary": case "float": case "utf8": case "utf16": case "utf32":
                            category = "type"; type = spec; break;
                        case "bytes": case "bitstring": case "bits":
                            category = "type"; type = "binary"; int aliasUnit = spec == "bytes" ? 8 : 1;
                            Merge("unit", aliasUnit.ToString(CultureInfo.InvariantCulture)); unit = aliasUnit; break;
                        case "big": case "little": case "native": category = "endian"; endian = spec; break;
                        case "signed": case "unsigned": category = "sign"; signed = spec == "signed"; break;
                        case "unit":
                            category = "unit"; Expect(":");
                            if (Current.Kind != "integer" || !int.TryParse(Current.Text, out var parsed) || parsed is < 1 or > 256) throw Error("Bit segment unit must be an integer from 1 through 256");
                            unit = parsed; position++; break;
                        default: throw new CompileException("ERL003", $"Bit segment specifier '{spec}' is not supported yet", Current.Start);
                    }
                    Merge(category, category == "type" ? type : category == "unit" ? unit!.Value.ToString(CultureInfo.InvariantCulture) : spec);
                } while (Take("-"));
            }
            int defaultUnit = type is "binary" or "bytes" ? 8 : 1;
            if ((type is "integer" or "float") && size is null && unit is not null) throw Error("An explicit numeric segment unit requires a size");
            if (BitUnicode.IsUtf(type) && (size is not null || unit is not null)) throw Error("UTF segments must not specify a size or unit");
            if (value is Expr.Literal { Value: Cons or Nil } && BitUnicode.IsUtf(type))
            {
                foreach (var item in Cons.Items(((Expr.Literal)value).Value)) segments.Add(new(new Expr.Literal(item), null, type, 1, endian, signed));
            }
            else if (value is Expr.Literal { Value: Cons or Nil } && categories.Count == 0 && size is null)
            {
                foreach (var item in Cons.Items(((Expr.Literal)value).Value)) segments.Add(new(new Expr.Literal(item), null));
            }
            else if (value is Expr.Literal { Value: Cons or Nil }) throw new CompileException("ERL003", "String segment modifiers are not supported yet", Current.Start);
            else segments.Add(new(value, size, type, unit ?? defaultUnit, endian, signed));
        } while (Take(","));
        Expect(">>"); return new Expr.Bits(segments);
    }
    private Expr ParseMap(Expr? mapBase)
    {
        Expect("{"); var fields = new List<MapField>();
        if (!Take("}"))
        {
            do
            {
                var key = Expression(); bool exact;
                if (Take(":=")) exact = true;
                else if (Take("=>")) exact = false;
                else throw Error("Expected '=>' or ':=' in map field");
                fields.Add(new(key, Expression(), exact));
            } while (Take(","));
            Expect("}");
        }
        return new Expr.Map(mapBase, fields);
    }
    private List<Expr> Arguments() { var args = new List<Expr>(); if (!Take(")")) { do { args.Add(Expression()); } while (Take(",")); Expect(")"); } return args; }
    public static Pattern ToPattern(Expr e) => e switch
    { Expr.Bits bits => BitPattern.FromExpression(bits), Expr.Map { Base: null } m when m.Fields.All(f => f.Exact) => new MapPattern(m.Fields.Select(f => new MapPatternField(f.Key, ToPattern(f.Value))).ToArray()), Expr.Literal l => new Pattern.Literal(l.Value), Expr.Variable v => new Pattern.Variable(v.Name), Expr.Tuple t => new Pattern.Tuple(t.Items.Select(ToPattern).ToArray()), Expr.List l => new Pattern.List(l.Items.Select(ToPattern).ToArray(), l.Tail is null ? null : ToPattern(l.Tail)), Expr.Unary { Operator: "-", Operand: Expr.Literal { Value: Integer i } } => new Pattern.Literal(new Integer(-i.Value)), Expr.Unary { Operator: "-", Operand: Expr.Literal { Value: FloatTerm f } } => new Pattern.Literal(new FloatTerm(-f.Value)), _ => throw new CompileException("ERL004", "Invalid or unsupported pattern", 0) };
}
