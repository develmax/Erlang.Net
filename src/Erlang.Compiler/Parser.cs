// Modified: try/maybe productions adapted to the existing Pratt AST from OTP-29.1.1 erl_parse.yrl.
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
            if (Take(ErlangSyntaxTokens.AttributeIntroducer))
            {
                string attr = Name();
                Expect(ErlangSyntaxTokens.OpenParenthesis);
                if (attr == ModuleAttributeNames.Module)
                {
                    module = Name();
                    Expect(ErlangSyntaxTokens.CloseParenthesis);
                    Expect(ErlangSyntaxTokens.FormTerminator);
                }
                else if (attr == ModuleAttributeNames.Export)
                {
                    Expect(ErlangSyntaxTokens.OpenList);
                    if (!Take(ErlangSyntaxTokens.CloseList))
                    {
                        do
                        {
                            string name = Name();
                            Expect(ErlangSyntaxTokens.AritySeparator);
                            if (Current.Kind != LexerTokenKinds.Integer)
                                throw Error(ParserDiagnostics.ExpectedArity);
                            int arity = int.Parse(tokens[position++].Text, CultureInfo.InvariantCulture);
                            exports.Add((name, arity));
                        } while (Take(ErlangSyntaxTokens.Comma));
                        Expect(ErlangSyntaxTokens.CloseList);
                    }
                    Expect(ErlangSyntaxTokens.CloseParenthesis);
                    Expect(ErlangSyntaxTokens.FormTerminator);
                }
                else
                    throw new CompileException(CompilerDiagnosticCodes.UnsupportedSyntax, ParserDiagnostics.UnsupportedAttribute(attr), Current.Start);
                continue;
            }
            string fname = Name();
            Expect(ErlangSyntaxTokens.OpenParenthesis);
            var patterns = PatternArguments();
            var clauses = new List<Clause>();
            int count = patterns.Count;
            clauses.Add(ParseClause(patterns));
            while (Take(ErlangSyntaxTokens.Semicolon))
            {
                if (Name() != fname)
                    throw Error(ParserDiagnostics.ClauseNameMismatch);
                Expect(ErlangSyntaxTokens.OpenParenthesis);
                patterns = PatternArguments();
                if (patterns.Count != count)
                    throw Error(ParserDiagnostics.ClauseArityMismatch);
                clauses.Add(ParseClause(patterns));
            }
            Expect(ErlangSyntaxTokens.FormTerminator);
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
        if (!Take(ErlangSyntaxTokens.CloseParenthesis))
        {
            do
            {
                args.Add(ToPattern(Expression()));
            } while (Take(ErlangSyntaxTokens.Comma));
            Expect(ErlangSyntaxTokens.CloseParenthesis);
        }

        return args;
    }

    private Clause ParseClause(IReadOnlyList<Pattern> patterns)
    {
        Expr? guard = null;
        if (Take(ErlangKeywords.When))
            guard = Guard();
        Expect(ErlangSyntaxTokens.FunctionArrow);

        return new(patterns, guard, Body());
    }

    private Expr Guard()
    {
        var alternatives = new List<Expr>();
        do
        {
            Expr conjunction = Expression();
            while (Take(ErlangSyntaxTokens.Comma))
                conjunction = new Expr.Binary(ErlangOperators.AndAlso, conjunction, Expression());
            alternatives.Add(conjunction);
        } while (Take(ErlangSyntaxTokens.Semicolon));

        return alternatives.Count == 1 ? alternatives[0] : new Expr.GuardAlternatives(alternatives);
    }

    private Expr Body()
    {
        var body = new List<Expr> { Expression() };
        while (Take(ErlangSyntaxTokens.Comma))
            body.Add(Expression());

        return body.Count == 1 ? body[0] : new Expr.Sequence(body);
    }

    private List<Clause> Clauses()
    {
        var result = new List<Clause>();
        if (Is(ErlangKeywords.End) || Is(ErlangKeywords.After))
            return result;
        do
        {
            result.Add(ParseClause([ToPattern(Expression())]));
        } while (Take(ErlangSyntaxTokens.Semicolon));

        return result;
    }

    private static int Precedence(string op) => op switch
    {
        ErlangOperators.Match or ErlangOperators.Send => OperatorPrecedence.MatchAndSend,
        ErlangOperators.OrElse => OperatorPrecedence.OrElse,
        ErlangOperators.AndAlso => OperatorPrecedence.AndAlso,
        ErlangOperators.NumericEqual or ErlangOperators.NumericNotEqual or ErlangOperators.ExactEqual or ErlangOperators.ExactNotEqual or ErlangOperators.Less or ErlangOperators.Greater or ErlangOperators.LessOrEqual or ErlangOperators.GreaterOrEqual => OperatorPrecedence.Comparison,
        ErlangOperators.Append or ErlangOperators.SubtractList => OperatorPrecedence.List,
        ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.BitwiseOr or ErlangOperators.BitwiseXor or ErlangOperators.ShiftLeft or ErlangOperators.ShiftRight or ErlangOperators.Or or ErlangOperators.Xor => OperatorPrecedence.Additive,
        ErlangOperators.Multiply or ErlangOperators.Divide or ErlangOperators.IntegerDivide or ErlangOperators.Remainder or ErlangOperators.BitwiseAnd or ErlangOperators.And => OperatorPrecedence.Multiplicative,
        _ => OperatorPrecedence.None
    };

    private Expr Expression(int minimum = 1, bool stopQualifier = false)
    {
        Expr left = Primary(stopQualifier: stopQualifier);
        while (true)
        {
            int p = Current.Kind == LexerTokenKinds.QuotedAtom ? 0 : Precedence(Current.Text);
            if (p < minimum || p == 0)
                break;
            string op = tokens[position++].Text;
            var right = Expression(
                op is ErlangOperators.Match or ErlangOperators.Send or ErlangOperators.Append or ErlangOperators.SubtractList or ErlangOperators.AndAlso or ErlangOperators.OrElse ? p : p + 1,
                stopQualifier
            );
            left = op == ErlangOperators.Match ? new Expr.Match(ToPattern(left), right) : new Expr.Binary(op, left, right);
            if (p == OperatorPrecedence.Comparison && Current.Kind != LexerTokenKinds.QuotedAtom && Precedence(Current.Text) == OperatorPrecedence.Comparison)
                throw Error(ParserDiagnostics.ChainedComparison);
        }

        return left;
    }

    private Expr Primary(bool bitSegment = false, bool stopQualifier = false)
    {
        Expr result;
        if (Take(ErlangKeywords.Maybe))
        {
            if (Is(ErlangKeywords.Else) || Is(ErlangKeywords.End))
                throw Error(ParserDiagnostics.EmptyMaybe);
            var items = new List<Expr>();
            do
            {
                var item = Expression();
                if (Take(ErlangSyntaxTokens.ConditionalMatch))
                    item = new Expr.MaybeMatch(ToPattern(item), Expression());
                items.Add(item);
            } while (Take(ErlangSyntaxTokens.Comma));
            var clauses = new List<Clause>();
            if (Take(ErlangKeywords.Else))
            {
                if (Is(ErlangKeywords.End))
                    throw Error(ParserDiagnostics.EmptyMaybeElse);
                clauses = Clauses();
            }
            Expect(ErlangKeywords.End);

            return new Expr.Maybe(items, clauses);
        }
        if (Take(ErlangKeywords.Try))
        {
            var body = Body();
            var clauses = new List<Clause>();
            if (Take(ErlangKeywords.Of))
            {
                if (Is(ErlangKeywords.Catch) || Is(ErlangKeywords.After) || Is(ErlangKeywords.End))
                    throw Error(ParserDiagnostics.EmptyTryOf);
                clauses = Clauses();
            }
            var catches = new List<Clause>();
            if (Take(ErlangKeywords.Catch))
            {
                do
                {
                    var first = ToPattern(Expression(stopQualifier: true));
                    Pattern exceptionClass = new Pattern.Literal(Term.A(ErlangExceptionClasses.Throw));
                    Pattern reason = first;
                    Pattern stack = new Pattern.Variable(VariableScopeNames.Wildcard);
                    if (Take(ErlangSyntaxTokens.ModuleQualifier))
                    {
                        if (first is not (Pattern.Variable or Pattern.Literal { Value: Atom }))
                            throw Error(ParserDiagnostics.InvalidExceptionClass);
                        exceptionClass = first;
                        reason = ToPattern(Expression(stopQualifier: true));
                        if (Take(ErlangSyntaxTokens.ModuleQualifier))
                        {
                            if (Current.Kind != LexerTokenKinds.Variable)
                                throw Error(ParserDiagnostics.ExpectedStackVariable);
                            stack = new Pattern.Variable(tokens[position++].Text);
                        }
                    }
                    catches.Add(ParseClause([new Pattern.Tuple([exceptionClass, reason, stack])]));
                } while (Take(ErlangSyntaxTokens.Semicolon));
            }
            Expr? after = null;
            if (Take(ErlangKeywords.After))
                after = Body();
            if (catches.Count == 0 && after is null)
                throw Error(ParserDiagnostics.TryNeedsHandler);
            Expect(ErlangKeywords.End);

            return new Expr.Try(
                body,
                clauses,
                catches,
                after
            );
        }
        if (Take(ErlangKeywords.Catch))
            return new Expr.Catch(Expression());
        if (Take(ErlangKeywords.Begin))
        {
            if (Is(ErlangKeywords.End))
                throw Error(ParserDiagnostics.EmptyBlock);
            var body = Body();
            Expect(ErlangKeywords.End);

            return new Expr.Block(body);
        }
        if (Take(ErlangKeywords.If))
        {
            if (Is(ErlangKeywords.End))
                throw Error(ParserDiagnostics.EmptyIf);
            var clauses = new List<Clause>();
            do
            {
                var guard = Guard();
                Expect(ErlangSyntaxTokens.FunctionArrow);
                clauses.Add(new Clause([], guard, Body()));
            } while (Take(ErlangSyntaxTokens.Semicolon));
            Expect(ErlangKeywords.End);

            return new Expr.If(clauses);
        }
        if (Take(ErlangKeywords.Receive))
        {
            var clauses = Clauses();
            Expr? timeout = null, after = null;
            if (Take(ErlangKeywords.After))
            {
                timeout = Expression();
                Expect(ErlangSyntaxTokens.FunctionArrow);
                after = Body();
            }
            Expect(ErlangKeywords.End);

            return new Expr.Receive(clauses, timeout, after);
        }
        if (Take(ErlangKeywords.Case))
        {
            var value = Expression();
            Expect(ErlangKeywords.Of);
            var clauses = Clauses();
            if (clauses.Count == 0)
                throw Error(ParserDiagnostics.EmptyCase);
            Expect(ErlangKeywords.End);

            return new Expr.Case(value, clauses);
        }
        if (Take(ErlangKeywords.Fun))
        {
            var clauses = new List<Clause>();
            do
            {
                Expect(ErlangSyntaxTokens.OpenParenthesis);
                clauses.Add(ParseClause(PatternArguments()));
            } while (Take(ErlangSyntaxTokens.Semicolon));
            Expect(ErlangKeywords.End);

            return new Expr.Fun(clauses);
        }
        if (Current.Kind != LexerTokenKinds.QuotedAtom && Current.Text is ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Not or ErlangOperators.BitwiseNot)
        {
            string op = tokens[position++].Text;

            return new Expr.Unary(op, bitSegment ? Primary(true) : Expression(OperatorPrecedence.Prefix, stopQualifier));
        }
        if (Take(ErlangSyntaxTokens.OpenParenthesis))
        {
            result = Expression();
            Expect(ErlangSyntaxTokens.CloseParenthesis);
        }
        else if (Take(ErlangSyntaxTokens.BinaryOpen))
            result = ParseBits();
        else if (Take(ErlangSyntaxTokens.MapPrefix))
            result = ParseMap(null);
        else if (Take(ErlangSyntaxTokens.OpenTuple))
        {
            var items = new List<Expr>();
            if (!Take(ErlangSyntaxTokens.CloseTuple))
            {
                do
                {
                    items.Add(Expression());
                } while (Take(ErlangSyntaxTokens.Comma));
                Expect(ErlangSyntaxTokens.CloseTuple);
            }
            result = new Expr.Tuple(items);
        }
        else if (Take(ErlangSyntaxTokens.OpenList))
        {
            var items = new List<Expr>();
            Expr? tail = null;
            if (!Take(ErlangSyntaxTokens.CloseList))
            {
                do
                {
                    items.Add(Expression());
                } while (Take(ErlangSyntaxTokens.Comma));
                if (Take(ErlangSyntaxTokens.ComprehensionSeparator))
                {
                    var qualifiers = ComprehensionQualifiers(ErlangSyntaxTokens.CloseList);

                    return new Expr.ListComprehension(items, qualifiers);
                }
                if (Take(ErlangSyntaxTokens.ListTail))
                    tail = Expression();
                Expect(ErlangSyntaxTokens.CloseList);
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
            if (Take(ErlangSyntaxTokens.MapPrefix))
                result = ParseMap(result);
            else if (!stopQualifier && Take(ErlangSyntaxTokens.ModuleQualifier))
            {
                if (result is not Expr.Literal { Value: Atom module })
                    throw Error(ParserDiagnostics.DynamicModuleCall);
                string name = Name();
                Expect(ErlangSyntaxTokens.OpenParenthesis);
                result = new Expr.Call(module.Name, name, Arguments());
            }
            else if (Take(ErlangSyntaxTokens.OpenParenthesis))
            {
                var args = Arguments();
                result = result is Expr.Literal { Value: Atom fn } ? new Expr.Call(null, fn.Name, args) : new Expr.Apply(result, args);
            }
            else
                break;
        }

        return result;
    }

    private IReadOnlyList<ComprehensionQualifier> ComprehensionQualifiers(string close)
    {
        var qualifiers = new List<ComprehensionQualifier>();
        do
        {
            var qualifier = Expression();
            if (Take(ErlangSyntaxTokens.MapExactField))
            {
                var value = Expression();
                bool strictMap = Take(ErlangSyntaxTokens.StrictListGenerator);
                if (!strictMap)
                    Expect(ErlangSyntaxTokens.ListGenerator);
                qualifiers.Add(new ComprehensionQualifier.MapGenerator(ToPattern(new Expr.Tuple([qualifier, value])), Expression(), strictMap));
                continue;
            }
            bool strictBinary = Take(ErlangSyntaxTokens.StrictBinaryGenerator);
            if (strictBinary || Take(ErlangSyntaxTokens.BinaryGenerator))
            {
                if (qualifier is not Expr.Bits bits)
                    throw Error(ParserDiagnostics.BinaryGeneratorPattern);
                var pattern = BitPattern.FromExpression(bits);
                if (pattern.Segments.Count == 0)
                    throw Error(BitPatternDiagnostics.EmptyGeneratorPattern);
                if (pattern.Segments.Any(s => s.Specification.Type == BitSegmentTypes.Binary && (s.Specification.Size is null || s.Specification.Size is Expr.Literal { Value: Atom { Name: BitSizeAtoms.All } })))
                    throw new CompileException(CompilerDiagnosticCodes.InvalidPattern, BitPatternDiagnostics.UnsizedGeneratorField, 0);
                qualifiers.Add(new ComprehensionQualifier.BinaryGenerator(pattern, Expression(), strictBinary));
            }
            else
            {
                bool strict = Take(ErlangSyntaxTokens.StrictListGenerator);
                if (strict || Take(ErlangSyntaxTokens.ListGenerator))
                    qualifiers.Add(new ComprehensionQualifier.Generator(ToPattern(qualifier), Expression(), strict));
                else
                {
                    if (qualifier is Expr.Match)
                        throw Error(ParserDiagnostics.ComprehensionAssignment);
                    qualifiers.Add(new ComprehensionQualifier.Filter(qualifier));
                }
            }
        } while (Take(ErlangSyntaxTokens.Comma));
        Expect(close);

        return qualifiers;
    }

    private Expr ParseBits()
    {
        var segments = new List<BitSegment>();
        if (Take(ErlangSyntaxTokens.BinaryClose))
            return new Expr.Bits(segments);
        do
        {
            var value = Primary(true);
            if (segments.Count == 0 && Take(ErlangSyntaxTokens.ComprehensionSeparator))
                return new Expr.BinaryComprehension(value, ComprehensionQualifiers(ErlangSyntaxTokens.BinaryClose));
            Expr? size = null;
            if (Take(BitSyntaxTokens.SizeSeparator))
            {
                if (Current.Text is ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Not or ErlangOperators.BitwiseNot)
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
            if (Take(BitSyntaxTokens.SpecifierIntroducer))
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
                            Expect(BitSyntaxTokens.UnitSeparator);
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
                } while (Take(BitSyntaxTokens.SpecifierSeparator));
            }
            int defaultUnit = type is BitSegmentTypes.Binary or BitSegmentAliases.Bytes ? BitSyntaxDefaults.BinaryUnit : 1;
            if ((type is BitSegmentTypes.Integer or BitSegmentTypes.Float) && size is null && unit is not null)
                throw Error(ParserDiagnostics.NumericUnitRequiresSize);
            if (BitUnicode.IsUtf(type) && (size is not null || unit is not null))
                throw Error(ParserDiagnostics.UtfSizeOrUnit);
            if (size is Expr.Literal { Value: Atom { Name: BitSizeAtoms.All } } && type != BitSegmentTypes.Binary)
                throw Error(ParserDiagnostics.NonBinaryAllSize);
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
        } while (Take(ErlangSyntaxTokens.Comma));
        Expect(ErlangSyntaxTokens.BinaryClose);

        return new Expr.Bits(segments);
    }

    private Expr ParseMap(Expr? mapBase)
    {
        Expect(ErlangSyntaxTokens.OpenTuple);
        var fields = new List<MapField>();
        if (!Take(ErlangSyntaxTokens.CloseTuple))
        {
            do
            {
                var key = Expression();
                bool exact;
                if (Take(ErlangSyntaxTokens.MapExactField))
                    exact = true;
                else if (Take(ErlangSyntaxTokens.MapAssociation))
                    exact = false;
                else
                    throw Error(ParserDiagnostics.ExpectedMapFieldOperator);
                fields.Add(new(key, Expression(), exact));
                if (mapBase is null && Take(ErlangSyntaxTokens.ComprehensionSeparator))
                    return new Expr.MapComprehension(fields, ComprehensionQualifiers(ErlangSyntaxTokens.CloseTuple));
            } while (Take(ErlangSyntaxTokens.Comma));
            Expect(ErlangSyntaxTokens.CloseTuple);
        }

        return new Expr.Map(mapBase, fields);
    }

    private List<Expr> Arguments()
    {
        var args = new List<Expr>();
        if (!Take(ErlangSyntaxTokens.CloseParenthesis))
        {
            do
            {
                args.Add(Expression());
            } while (Take(ErlangSyntaxTokens.Comma));
            Expect(ErlangSyntaxTokens.CloseParenthesis);
        }

        return args;
    }

    public static Pattern ToPattern(Expr e) => e switch
    {
        Expr.Match match => new AliasPattern(match.Pattern, ToPattern(match.Value)),
        Expr.Binary { Operator: ErlangOperators.Append } prefix => PrefixPattern(prefix.Left, prefix.Right),
        Expr.Bits bits => BitPattern.FromExpression(bits),
        Expr.Map { Base: null } m when m.Fields.All(f => f.Exact) => new MapPattern(m.Fields.Select(f => new MapPatternField(f.Key, ToPattern(f.Value))).ToArray()),
        Expr.Literal l => new Pattern.Literal(l.Value),
        Expr.Variable v => new Pattern.Variable(v.Name),
        Expr.Tuple t => new Pattern.Tuple(t.Items.Select(ToPattern).ToArray()),
        Expr.List l => new Pattern.List(l.Items.Select(ToPattern).ToArray(), l.Tail is null ? null : ToPattern(l.Tail)),
        Expr.Unary { Operator: ErlangOperators.Minus, Operand: Expr.Literal { Value: Integer i } } => new Pattern.Literal(new Integer(-i.Value)),
        Expr.Unary { Operator: ErlangOperators.Minus, Operand: Expr.Literal { Value: FloatTerm f } } => new Pattern.Literal(new FloatTerm(-f.Value)),
        _ => throw new CompileException(CompilerDiagnosticCodes.InvalidPattern, ParserDiagnostics.InvalidPattern, 0)
    };

    private static Pattern PrefixPattern(Expr prefix, Expr tail)
    {
        var items = new List<Pattern>();
        if (!PrefixItems(prefix, items))
            throw new CompileException(CompilerDiagnosticCodes.InvalidPattern, ParserDiagnostics.InvalidPattern, 0);
        var suffix = ToPattern(tail);

        return items.Count == 0 ? suffix : new Pattern.List(items, suffix);
    }

    private static bool PrefixItems(Expr expression, List<Pattern> items)
    {
        if (expression is Expr.Literal literal)
        {
            Term value = literal.Value;
            while (value is Cons { Head: Integer } cell)
            {
                items.Add(new Pattern.Literal(cell.Head));
                value = cell.Tail;
            }

            return value is Nil;
        }
        if (expression is not Expr.List list)
            return false;
        foreach (var item in list.Items)
        {
            if (item is not Expr.Literal { Value: Integer } integer)
                return false;
            items.Add(new Pattern.Literal(integer.Value));
        }

        return list.Tail is null || PrefixItems(list.Tail, items);
    }
}
