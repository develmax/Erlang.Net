using System.Globalization;
using System.Numerics;

namespace Erlang.Compiler;

internal sealed class NativeRecordTypeParser(IReadOnlyList<Token> tokens, string source, int start)
{
    public int Position { get; private set; } = start;
    private Token Current => tokens[Position];

    public Term Parse() => Union();

    private bool Take(string text)
    {
        if (Current.Text != text || Current.Kind is not (LexerTokenKinds.Symbol or LexerTokenKinds.Keyword))
            return false;
        Position++;

        return true;
    }

    private void Expect(string text)
    {
        if (!Take(text))
            throw new CompileException(CompilerDiagnosticCodes.Syntax, ParserDiagnostics.ExpectedToken(text, Current.Text), Current.Start);
    }

    private Term Annotation(int offset)
    {
        int line = 1;
        int column = 1;
        for (int index = 0; index < offset; index++)
        {
            if (source[index] == '\n')
            {
                line++;
                column = 1;
            }
            else if (!char.IsLowSurrogate(source[index]))
                column++;
        }

        return Term.Tuple(Term.I(line), Term.I(column));
    }

    private Term Node(string kind, int offset, params Term[] values) => new TupleTerm(new[] { Term.A(kind), Annotation(offset) }.Concat(values));

    private Term Type(string name, int offset, params Term[] values) => Node(
        NativeRecordTypeNames.Type,
        offset,
        Term.A(name),
        Cons.From(values)
    );

    private Term Union()
    {
        int offset = Current.Start;
        Term first = Arithmetic();
        if (Take(NativeRecordTypeTokens.Annotation))
        {
            if (first is not TupleTerm { Items: var annotationItems } || !annotationItems[0].Equals(Term.A(NativeRecordTypeNames.Variable)))
                throw new CompileException(CompilerDiagnosticCodes.Syntax, ParserDiagnostics.ExpectedAtom, offset);

            return Node(NativeRecordTypeNames.Annotated, offset, Cons.From(new[] { first, Union() }));
        }
        if (Take(NativeRecordTypeTokens.Range))
            first = Type(
                NativeRecordTypeNames.Range,
                offset,
                first,
                Arithmetic()
            );
        if (!Take(ErlangSyntaxTokens.ListTail))
            return first;
        var members = new List<Term> { first };
        do
        {
            Term member = Union();
            if (member is TupleTerm { Items.Count: 4 } union && union.Items[0].Equals(Term.A(NativeRecordTypeNames.Type)) && union.Items[2].Equals(Term.A(NativeRecordTypeNames.Union)))
                members.AddRange(Cons.Items(union.Items[3]));
            else
                members.Add(member);
        }
        while (Take(ErlangSyntaxTokens.ListTail));

        return Type(NativeRecordTypeNames.Union, offset, members.ToArray());
    }

    private Term Arithmetic(int minimum = 0)
    {
        int offset = Current.Start;
        Term value;
        if (Current.Text is ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.BitwiseNot)
        {
            string operation = tokens[Position++].Text;
            value = Node(
                NativeRecordTypeNames.Operator,
                offset,
                Term.A(operation),
                Arithmetic(3)
            );
        }
        else
            value = Primary();
        while (Priority(Current.Text) is var priority && priority >= minimum && priority > 0)
        {
            string operation = tokens[Position++].Text;
            value = Node(
                NativeRecordTypeNames.Operator,
                offset,
                Term.A(operation),
                value,
                Arithmetic(priority + 1)
            );
        }

        return value;
    }

    private static int Priority(string operation) => operation switch
    {
        ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.BitwiseOr or ErlangOperators.BitwiseXor or ErlangOperators.ShiftLeft or ErlangOperators.ShiftRight => 1,
        ErlangOperators.Multiply or ErlangOperators.Divide or ErlangOperators.IntegerDivide or ErlangOperators.Remainder or ErlangOperators.BitwiseAnd => 2,
        _ => 0
    };

    private Term Primary()
    {
        int offset = Current.Start;
        if (Take(ErlangSyntaxTokens.OpenParenthesis))
        {
            Term inner = Union();
            Expect(ErlangSyntaxTokens.CloseParenthesis);

            return inner;
        }
        if (Take(ErlangSyntaxTokens.OpenTuple))
            return Type(NativeRecordTypeNames.Tuple, offset, Arguments(ErlangSyntaxTokens.CloseTuple));
        if (Take(ErlangSyntaxTokens.OpenList))
        {
            if (Take(ErlangSyntaxTokens.CloseList))
                return Type(NativeRecordTypeNames.Nil, offset);
            Term element = Union();
            bool nonempty = Take(ErlangSyntaxTokens.Comma);
            if (nonempty)
                Expect(NativeRecordTypeTokens.Ellipsis);
            Expect(ErlangSyntaxTokens.CloseList);

            return Type(nonempty ? NativeRecordTypeNames.NonemptyList : NativeRecordTypeNames.List, offset, element);
        }
        if (Take(ErlangSyntaxTokens.BinaryOpen))
        {
            Term zero = Node(NativeRecordTypeNames.Integer, offset, Term.I(0));
            Term basis = zero;
            Term unit = zero;
            if (!Take(ErlangSyntaxTokens.BinaryClose))
            {
                ExpectWildcard();
                Expect(ErlangSyntaxTokens.ModuleQualifier);
                if (Current.Kind == LexerTokenKinds.Variable)
                {
                    ExpectWildcard();
                    Expect(ErlangOperators.Multiply);
                    unit = Arithmetic();
                }
                else
                    basis = Arithmetic();
                if (Take(ErlangSyntaxTokens.Comma))
                {
                    ExpectWildcard();
                    Expect(ErlangSyntaxTokens.ModuleQualifier);
                    ExpectWildcard();
                    Expect(ErlangOperators.Multiply);
                    unit = Arithmetic();
                }
                Expect(ErlangSyntaxTokens.BinaryClose);
            }

            return Type(
                NativeRecordTypeNames.Binary,
                offset,
                basis,
                unit
            );
        }
        if (Take(ErlangSyntaxTokens.MapPrefix))
        {
            if (Current.Kind is LexerTokenKinds.Atom or LexerTokenKinds.QuotedAtom)
            {
                Token name = tokens[Position++];
                Term identifier = Node(
                    name.Kind == LexerTokenKinds.Variable ? NativeRecordTypeNames.Variable : NativeRecordTypeNames.Atom,
                    name.Start,
                    Term.A(name.Text)
                );
                if (Take(ErlangSyntaxTokens.ModuleQualifier))
                {
                    Token qualified = tokens[Position++];
                    identifier = Node(
                        NativeRecordTypeNames.Tuple,
                        offset,
                        Cons.From(new[] { identifier, Node(
                            qualified.Kind == LexerTokenKinds.Variable ? NativeRecordTypeNames.Variable : NativeRecordTypeNames.Atom,
                            qualified.Start,
                            Term.A(qualified.Text)
                        ) })
                    );
                }
                Expect(ErlangSyntaxTokens.OpenTuple);
                var members = new List<Term> { identifier };
                if (!Take(ErlangSyntaxTokens.CloseTuple))
                {
                    do
                    {
                        Token field = tokens[Position++];
                        if (field.Kind is not (LexerTokenKinds.Atom or LexerTokenKinds.QuotedAtom))
                            throw new CompileException(CompilerDiagnosticCodes.Syntax, ParserDiagnostics.ExpectedAtom, field.Start);
                        Expect(NativeRecordTypeTokens.Annotation);
                        members.Add(Type(
                            NativeRecordTypeNames.Field,
                            field.Start,
                            Node(NativeRecordTypeNames.Atom, field.Start, Term.A(field.Text)),
                            Union()
                        ));
                    } while (Take(ErlangSyntaxTokens.Comma));
                    Expect(ErlangSyntaxTokens.CloseTuple);
                }

                return Type(NativeRecordTypeNames.Record, offset, members.ToArray());
            }
            Expect(ErlangSyntaxTokens.OpenTuple);
            var fields = new List<Term>();
            if (!Take(ErlangSyntaxTokens.CloseTuple))
            {
                do
                {
                    int fieldOffset = Current.Start;
                    Term key = Union();
                    bool exact = Take(ErlangSyntaxTokens.MapExactField);
                    if (!exact)
                        Expect(ErlangSyntaxTokens.MapAssociation);
                    fields.Add(Type(
                        exact ? NativeRecordTypeNames.MapExact : NativeRecordTypeNames.MapAssociation,
                        fieldOffset,
                        key,
                        Union()
                    ));
                } while (Take(ErlangSyntaxTokens.Comma));
                Expect(ErlangSyntaxTokens.CloseTuple);
            }

            return Type(NativeRecordTypeNames.Map, offset, fields.ToArray());
        }
        if (Take(ErlangKeywords.Fun))
        {
            Expect(ErlangSyntaxTokens.OpenParenthesis);
            if (Take(ErlangSyntaxTokens.CloseParenthesis))
                return Type(NativeRecordTypeNames.Function, offset);
            Expect(ErlangSyntaxTokens.OpenParenthesis);
            Term parameters;
            if (Take(NativeRecordTypeTokens.Ellipsis))
            {
                Expect(ErlangSyntaxTokens.CloseParenthesis);
                parameters = Node(NativeRecordTypeNames.Type, offset, Term.A(NativeRecordTypeNames.Any));
            }
            else
                parameters = Type(NativeRecordTypeNames.Product, offset, Arguments(ErlangSyntaxTokens.CloseParenthesis));
            Expect(ErlangSyntaxTokens.FunctionArrow);
            Term result = Union();
            Expect(ErlangSyntaxTokens.CloseParenthesis);

            return Type(
                NativeRecordTypeNames.Function,
                offset,
                parameters,
                result
            );
        }
        Token token = tokens[Position++];
        if (token.Kind == LexerTokenKinds.Variable)
            return Node(NativeRecordTypeNames.Variable, offset, Term.A(token.Text));
        if (token.Kind == LexerTokenKinds.Integer)
            return Node(NativeRecordTypeNames.Integer, offset, new Integer(BigInteger.Parse(token.Text, CultureInfo.InvariantCulture)));
        if (token.Kind is not (LexerTokenKinds.Atom or LexerTokenKinds.QuotedAtom))
            throw new CompileException(CompilerDiagnosticCodes.Syntax, ParserDiagnostics.ExpectedAtom, offset);
        if (Take(ErlangSyntaxTokens.ModuleQualifier))
        {
            Token name = tokens[Position++];
            if (name.Kind is not (LexerTokenKinds.Atom or LexerTokenKinds.QuotedAtom))
                throw new CompileException(CompilerDiagnosticCodes.Syntax, ParserDiagnostics.ExpectedAtom, name.Start);
            Expect(ErlangSyntaxTokens.OpenParenthesis);

            return Node(
                NativeRecordTypeNames.Remote,
                offset,
                Cons.From(new[] { Node(NativeRecordTypeNames.Atom, offset, Term.A(token.Text)), Node(NativeRecordTypeNames.Atom, name.Start, Term.A(name.Text)), Cons.From(Arguments(ErlangSyntaxTokens.CloseParenthesis)) })
            );
        }
        if (!Take(ErlangSyntaxTokens.OpenParenthesis))
            return Node(NativeRecordTypeNames.Atom, offset, Term.A(token.Text));
        Term[] arguments = Arguments(ErlangSyntaxTokens.CloseParenthesis);
        NativeRecordTypeLint.Validate(token.Text, arguments.Length, offset);

        return arguments.Length == 0 && token.Text is NativeRecordTypeNames.Map or NativeRecordTypeNames.Tuple
            ? Node(
                NativeRecordTypeNames.Type,
                offset,
                Term.A(token.Text),
                Term.A(NativeRecordTypeNames.Any)
            )
            : Type(token.Text, offset, arguments);
    }

    private void ExpectWildcard()
    {
        if (Current.Kind != LexerTokenKinds.Variable || Current.Text != VariableScopeNames.Wildcard)
            throw new CompileException(CompilerDiagnosticCodes.Syntax, ParserDiagnostics.ExpectedToken(VariableScopeNames.Wildcard, Current.Text), Current.Start);
        Position++;
    }

    private Term[] Arguments(string end)
    {
        var arguments = new List<Term>();
        if (!Take(end))
        {
            do
                arguments.Add(Union());
            while (Take(ErlangSyntaxTokens.Comma));
            Expect(end);
        }

        return arguments.ToArray();
    }
}
