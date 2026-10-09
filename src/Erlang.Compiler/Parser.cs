using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

public sealed class Parser
{
    private readonly List<Token> tokens; private int position;

    public Parser(string text, bool blockPrefix = false) => tokens = Lexer.Scan(text, blockPrefix);

    private Token Current => tokens[position];
    public int EndOffset => position == 0 ? 0 : tokens[position - 1].End;

    private bool Is(string value) => Current.Kind != LexerTokenKinds.QuotedAtom && Current.Text == value;

    private bool Take(string value)
    {
        if (!Is(value))
            return false;
        position++;

        return true;
    }

    private Token Expect(string value)
    {
        if (!Is(value))
            throw Error(ParserDiagnostics.ExpectedToken(value, Current.Text));

        return tokens[position++];
    }

    private CompileException Error(string message) => new(CompilerDiagnosticCodes.Syntax, message, Current.Start);

    private string Name()
    {
        if (Current.Kind is not (LexerTokenKinds.Atom or LexerTokenKinds.QuotedAtom))
            throw Error(ParserDiagnostics.ExpectedAtom);

        return tokens[position++].Text;
    }

    public Expr ParseExpression(bool requireEnd = true)
    {
        var e = Expression();
        if (requireEnd && Current.Kind != LexerTokenKinds.EndOfInput)
            throw Error(ParserDiagnostics.UnexpectedTrailingToken);

        return e;
    }

    public ModuleDefinition ParseModule()
    {
        string? module = null;
        var exports = new List<(string, int)>();
        var functions = new List<FunctionDefinition>();
        while (Current.Kind != LexerTokenKinds.EndOfInput)
        {
            if (Take("-"))
            {
                string attr = Name();
                Expect("(");
                if (attr == "module")
                {
                    module = Name();
                    Expect(")");
                    Expect(".");
                }
                else if (attr == "export")
                {
                    Expect("[");
                    if (!Take("]"))
                    {
                        do
                        {
                            string name = Name();
                            Expect("/");
                            if (Current.Kind != LexerTokenKinds.Integer)
                                throw Error(ParserDiagnostics.ExpectedArity);
                            int arity = int.Parse(tokens[position++].Text, CultureInfo.InvariantCulture);
                            exports.Add((name, arity));
                        } while (Take(","));
                        Expect("]");
                    }
                    Expect(")");
                    Expect(".");
                }
                else
                    throw new CompileException(CompilerDiagnosticCodes.UnsupportedSyntax, ParserDiagnostics.UnsupportedAttribute(attr), Current.Start);
                continue;
            }
            string fname = Name();
            Expect("(");
            var patterns = PatternArguments();
            var clauses = new List<Clause>();
            int count = patterns.Count;
            clauses.Add(ParseClause(patterns));
            while (Take(";"))
            {
                if (Name() != fname)
                    throw Error(ParserDiagnostics.ClauseNameMismatch);
                Expect("(");
                patterns = PatternArguments();
                if (patterns.Count != count)
                    throw Error(ParserDiagnostics.ClauseArityMismatch);
                clauses.Add(ParseClause(patterns));
            }
            Expect(".");
            functions.Add(new(fname, count, clauses));
        }
        if (module is null)
            throw Error(ParserDiagnostics.MissingModuleAttribute);
        var result = new ModuleDefinition(module, exports, functions);
        Semantics.Validate(result);

        return result;
    }

    private List<Pattern> PatternArguments()
    {
        var args = new List<Pattern>();
        if (!Take(")"))
        {
            do
            {
                args.Add(ToPattern(Expression(2)));
            } while (Take(","));
            Expect(")");
        }

        return args;
    }

    private Clause ParseClause(IReadOnlyList<Pattern> patterns)
    {
        Expr? guard = null;
        if (Take("when"))
        {
            var alternatives = new List<Expr>();
            do
            {
                Expr conjunction = Expression();
                while (Take(","))
                    conjunction = new Expr.Binary("andalso", conjunction, Expression());
                alternatives.Add(conjunction);
            } while (Take(";"));
            guard = alternatives.Count == 1 ? alternatives[0] : new Expr.GuardAlternatives(alternatives);
        }
        Expect("->");

        return new(patterns, guard, Body());
    }

    private Expr Body()
    {
        var body = new List<Expr> { Expression() };
        while (Take(","))
            body.Add(Expression());

        return body.Count == 1 ? body[0] : new Expr.Sequence(body);
    }

    private List<Clause> Clauses()
    {
        var result = new List<Clause>();
        if (Is("end") || Is("after"))
            return result;
        do
        {
            result.Add(ParseClause([ToPattern(Expression(2))]));
        } while (Take(";"));

        return result;
    }

    private static int Precedence(string op) => op switch
    {
        "=" => 1,
        "!" => 2,
        "orelse" => 3,
        "andalso" => 4,
        "==" or "/=" or "=:=" or "=/=" or "<" or ">" or "=<" or ">=" => 5,
        "++" or "--" => 6,
        "+" or "-" => 7,
        "*" or "/" or "div" or "rem" => 8,
        _ => 0
    };

    private Expr Expression(int minimum = 1)
    {
        Expr left = Primary();
        while (true)
        {
            int p = Current.Kind == LexerTokenKinds.QuotedAtom ? 0 : Precedence(Current.Text);
            if (p < minimum || p == 0)
                break;
            string op = tokens[position++].Text;
            var right = Expression(op is "=" or "!" or "++" or "--" ? p : p + 1);
            left = op == "=" ? new Expr.Match(ToPattern(left), right) : new Expr.Binary(op, left, right);
        }

        return left;
    }

    private Expr Primary(bool bitSegment = false)
    {
        Expr result;
        if (Take("receive"))
        {
            var clauses = Clauses();
            Expr? timeout = null, after = null;
            if (Take("after"))
            {
                timeout = Expression();
                Expect("->");
                after = Body();
            }
            Expect("end");

            return new Expr.Receive(clauses, timeout, after);
        }
        if (Take("case"))
        {
            var value = Expression();
            Expect("of");
            var clauses = Clauses();
            if (clauses.Count == 0)
                throw Error(ParserDiagnostics.EmptyCase);
            Expect("end");

            return new Expr.Case(value, clauses);
        }
        if (Take("fun"))
        {
            var clauses = new List<Clause>();
            do
            {
                Expect("(");
                clauses.Add(ParseClause(PatternArguments()));
            } while (Take(";"));
            Expect("end");

            return new Expr.Fun(clauses);
        }
        if (Current.Kind != LexerTokenKinds.QuotedAtom && Current.Text is "+" or "-" or "not")
        {
            string op = tokens[position++].Text;

            return new Expr.Unary(op, bitSegment ? Primary(true) : Expression(9));
        }
        if (Take("("))
        {
            result = Expression();
            Expect(")");
        }
        else if (Take("<<"))
            result = ParseBits();
        else if (Take("#"))
            result = ParseMap(null);
        else if (Take("{"))
        {
            var items = new List<Expr>();
            if (!Take("}"))
            {
                do
                {
                    items.Add(Expression());
                } while (Take(","));
                Expect("}");
            }
            result = new Expr.Tuple(items);
        }
        else if (Take("["))
        {
            var items = new List<Expr>();
            Expr? tail = null;
            if (!Take("]"))
            {
                do
                {
                    items.Add(Expression());
                } while (Take(","));
                if (Take("|"))
                    tail = Expression();
                Expect("]");
            }
            result = new Expr.List(items, tail);
        }
        else
        {
            var token = Current;
            position++;
            result = token.Kind switch
            {
                LexerTokenKinds.Integer => new Expr.Literal(new Integer(BigInteger.Parse(token.Text, CultureInfo.InvariantCulture))),
                LexerTokenKinds.Float => new Expr.Literal(new FloatTerm(double.Parse(token.Text, CultureInfo.InvariantCulture))),
                LexerTokenKinds.String => new Expr.Literal(Term.String(token.Text)),
                LexerTokenKinds.Variable => new Expr.Variable(token.Text),
                LexerTokenKinds.Atom or LexerTokenKinds.QuotedAtom => new Expr.Literal(Term.A(token.Text)),
                _ => throw new CompileException(CompilerDiagnosticCodes.UnsupportedSyntax, ParserDiagnostics.UnsupportedExpression(token.Text), token.Start)
            };
        }
        while (true)
        {
            if (bitSegment)
                break;
            if (Take("#"))
                result = ParseMap(result);
            else if (Take(":"))
            {
                if (result is not Expr.Literal { Value: Atom module })
                    throw Error(ParserDiagnostics.DynamicModuleCall);
                string name = Name();
                Expect("(");
                result = new Expr.Call(module.Name, name, Arguments());
            }
            else if (Take("("))
            {
                var args = Arguments();
                result = result is Expr.Literal { Value: Atom fn } ? new Expr.Call(null, fn.Name, args) : new Expr.Apply(result, args);
            }
            else
                break;
        }

        return result;
    }

    private Expr ParseBits()
    {
        var segments = new List<BitSegment>();
        if (Take(">>"))
            return new Expr.Bits(segments);
        do
        {
            var value = Primary(true);
            Expr? size = null;
            if (Take(":"))
            {
                if (Current.Text is "+" or "-" or "not" or "bnot")
                    throw Error(ParserDiagnostics.UnaryBitSize);
                size = Primary(true);
            }
            string type = BitSegmentTypes.Integer, endian = BitByteOrders.Big;
            int? unit = null;
            bool signed = false;
            var categories = new Dictionary<string, string>();

            void Merge(string category, string setting)
            {
                if (categories.TryGetValue(category, out var previous) && previous != setting)
                    throw Error(ParserDiagnostics.ConflictingBitSpecifier(category));
                categories[category] = setting;
            }
            if (Take("/"))
            {
                do
                {
                    string spec = Name();
                    string category;
                    switch (spec)
                    {
                        case BitSegmentTypes.Integer:
                        case BitSegmentTypes.Binary:
                        case BitSegmentTypes.Float:
                        case BitSegmentTypes.Utf8:
                        case BitSegmentTypes.Utf16:
                        case BitSegmentTypes.Utf32:
                            category = BitSpecifierCategories.Type;
                            type = spec;
                            break;
                        case BitSegmentAliases.Bytes:
                        case BitSegmentAliases.Bitstring:
                        case BitSegmentAliases.Bits:
                            category = BitSpecifierCategories.Type;
                            type = BitSegmentTypes.Binary;
                            int aliasUnit = spec == BitSegmentAliases.Bytes ? BitSyntaxDefaults.BinaryUnit : 1;
                            Merge(BitSpecifierCategories.Unit, aliasUnit.ToString(CultureInfo.InvariantCulture));
                            unit = aliasUnit;
                            break;
                        case BitByteOrders.Big:
                        case BitByteOrders.Little:
                        case BitByteOrders.Native:
                            category = BitSpecifierCategories.Endian;
                            endian = spec;
                            break;
                        case BitSignSpecifiers.Signed:
                        case BitSignSpecifiers.Unsigned:
                            category = BitSpecifierCategories.Sign;
                            signed = spec == BitSignSpecifiers.Signed;
                            break;
                        case BitUnitSpecifier.Name:
                            category = BitSpecifierCategories.Unit;
                            Expect(":");
                            if (Current.Kind != LexerTokenKinds.Integer || !int.TryParse(Current.Text, out var parsed) || parsed is < BitUnitSpecifier.Minimum or > BitUnitSpecifier.Maximum)
                                throw Error(ParserDiagnostics.InvalidBitUnit);
                            unit = parsed;
                            position++;
                            break;
                        default:
                            throw new CompileException(CompilerDiagnosticCodes.UnsupportedSyntax, ParserDiagnostics.UnsupportedBitSpecifier(spec), Current.Start);
                    }
                    Merge(
                        category,
                        category == BitSpecifierCategories.Type ? type : category == BitSpecifierCategories.Unit ? unit!.Value.ToString(CultureInfo.InvariantCulture) : spec
                    );
                } while (Take("-"));
            }
            int defaultUnit = type is BitSegmentTypes.Binary or BitSegmentAliases.Bytes ? BitSyntaxDefaults.BinaryUnit : 1;
            if ((type is BitSegmentTypes.Integer or BitSegmentTypes.Float) && size is null && unit is not null)
                throw Error(ParserDiagnostics.NumericUnitRequiresSize);
            if (BitUnicode.IsUtf(type) && (size is not null || unit is not null))
                throw Error(ParserDiagnostics.UtfSizeOrUnit);
            if (value is Expr.Literal { Value: Cons or Nil } && BitUnicode.IsUtf(type))
            {
                foreach (var item in Cons.Items(((Expr.Literal)value).Value))
                    segments.Add(new(
                        new Expr.Literal(item),
                        null,
                        type,
                        1,
                        endian,
                        signed
                    ));
            }
            else if (value is Expr.Literal { Value: Cons or Nil } && categories.Count == 0 && size is null)
            {
                foreach (var item in Cons.Items(((Expr.Literal)value).Value))
                    segments.Add(new(new Expr.Literal(item), null));
            }
            else if (value is Expr.Literal { Value: Cons or Nil })
                segments.Add(new(
                    value,
                    size,
                    type,
                    unit ?? defaultUnit,
                    endian,
                    signed,
                    IsStringLiteral: true
                ));
            else
                segments.Add(new(
                    value,
                    size,
                    type,
                    unit ?? defaultUnit,
                    endian,
                    signed
                ));
        } while (Take(","));
        Expect(">>");

        return new Expr.Bits(segments);
    }

    private Expr ParseMap(Expr? mapBase)
    {
        Expect("{");
        var fields = new List<MapField>();
        if (!Take("}"))
        {
            do
            {
                var key = Expression();
                bool exact;
                if (Take(":="))
                    exact = true;
                else if (Take("=>"))
                    exact = false;
                else
                    throw Error(ParserDiagnostics.ExpectedMapFieldOperator);
                fields.Add(new(key, Expression(), exact));
            } while (Take(","));
            Expect("}");
        }

        return new Expr.Map(mapBase, fields);
    }

    private List<Expr> Arguments()
    {
        var args = new List<Expr>();
        if (!Take(")"))
        {
            do
            {
                args.Add(Expression());
            } while (Take(","));
            Expect(")");
        }

        return args;
    }

    public static Pattern ToPattern(Expr e) => e switch
    {
        Expr.Bits bits => BitPattern.FromExpression(bits),
        Expr.Map { Base: null } m when m.Fields.All(f => f.Exact) => new MapPattern(m.Fields.Select(f => new MapPatternField(f.Key, ToPattern(f.Value))).ToArray()),
        Expr.Literal l => new Pattern.Literal(l.Value),
        Expr.Variable v => new Pattern.Variable(v.Name),
        Expr.Tuple t => new Pattern.Tuple(t.Items.Select(ToPattern).ToArray()),
        Expr.List l => new Pattern.List(l.Items.Select(ToPattern).ToArray(), l.Tail is null ? null : ToPattern(l.Tail)),
        Expr.Unary { Operator: "-", Operand: Expr.Literal { Value: Integer i } } => new Pattern.Literal(new Integer(-i.Value)),
        Expr.Unary { Operator: "-", Operand: Expr.Literal { Value: FloatTerm f } } => new Pattern.Literal(new FloatTerm(-f.Value)),
        _ => throw new CompileException(CompilerDiagnosticCodes.InvalidPattern, ParserDiagnostics.InvalidPattern, 0)
    };
}
