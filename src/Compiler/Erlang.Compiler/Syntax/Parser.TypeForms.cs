namespace Erlang.Compiler;

public sealed partial class Parser
{
    internal sealed record ReleaseSpec(
        (string Name, int Arity) Function,
        Term Form,
        string? Signature,
        int SourceIndex
    );

    internal static bool IsTypeDefinition(string name) => name is "type" or "opaque" or "nominal";

    private Term ReadTypeDefinition(string name)
    {
        Term variables = ReadTypeFormArguments(() =>
        {
            Token token = Current;
            if (token.PreprocessorLine == 0 && !token.HasExplicitFileLocation)
            {
                int line = Array.BinarySearch(sourceLineStarts, token.Start);
                token = token with { PreprocessorLine = line >= 0 ? line + 1 : ~line };
            }
            Term annotation = BeamDocumentationWriter.Annotation(token, source);

            return Term.Tuple(Term.A("var"), annotation, ReadTypeVariable());
        });
        Expect(NativeRecordTypeTokens.Annotation);
        Term body = ReadType();

        return Term.Tuple(Term.A(name), body, variables);
    }

    internal static ReleaseSpec ReadReleaseSpec(string source, IReadOnlyList<Token> tokens, int start)
    {
        var parser = new Parser(source, tokens as List<Token> ?? [.. tokens]) { position = start };
        bool parenthesized = parser.Take(ErlangSyntaxTokens.OpenParenthesis);
        string name = parser.Name();
        string? module = null;
        if (parser.Take(ErlangSyntaxTokens.ModuleQualifier))
        {
            module = name;
            name = parser.Name();
        }
        var clauses = new List<Term>();
        int? arity = null;
        string? signature = null;
        do
        {
            Term location = BeamDocumentationWriter.Annotation(parser.Current, source);
            Term arguments = parser.ReadTypeFormArguments(parser.ReadType);
            Term[] items = Cons.Items(arguments).ToArray();
            arity ??= items.Length;
            if (arity != items.Length)
            {
                throw new NotSupportedException(BeamEmissionFormat.Unsupported);
            }
            parser.Expect(ErlangSyntaxTokens.FunctionArrow);
            Term result = parser.ReadType();
            Term product = SpecType(location, "product", arguments);
            Term function = SpecType(location, "fun", Cons.From([product, result]));
            if (parser.Take(ErlangKeywords.When))
            {
                var constraints = new List<Term>();
                do
                {
                    Term annotation = BeamDocumentationWriter.Annotation(parser.Current, source);
                    Term variable = Term.Tuple(Term.A("var"), annotation, parser.ReadTypeVariable());
                    parser.Expect(NativeRecordTypeTokens.Annotation);
                    Term type = parser.ReadType();
                    Term subtype = Term.Tuple(Term.A("atom"), annotation, Term.A("is_subtype"));
                    constraints.Add(SpecType(annotation, "constraint", Cons.From([subtype, Cons.From([variable, type])])));
                }
                while (parser.Take(ErlangSyntaxTokens.Comma));
                function = SpecType(location, "bounded_fun", Cons.From([function, Cons.From(constraints)]));
            }

            string?[] names = items.Select(SpecArgumentName).ToArray();
            signature = clauses.Count == 0 && names.All(argument => argument is not null)
                ? name + "(" + string.Join(", ", names) + ")"
                : null;
            clauses.Add(function);
        }
        while (parser.Take(ErlangSyntaxTokens.Semicolon));
        if (parenthesized)
        {
            parser.Expect(ErlangSyntaxTokens.CloseParenthesis);
        }
        if (parser.Current.Kind != LexerTokenKinds.FormTerminator)
        {
            throw new NotSupportedException(BeamEmissionFormat.Unsupported);
        }

        Term key = module is null
            ? Term.Tuple(Term.A(name), Term.I(arity!.Value))
            : Term.Tuple(Term.A(module), Term.A(name), Term.I(arity!.Value));
        Term form = Term.Tuple(key, Cons.From(clauses));

        return new((name, arity.Value), form, signature, start);
    }

    private static Term SpecType(Term annotation, string name, Term arguments) =>
        Term.Tuple(Term.A("type"), annotation, Term.A(name), arguments);

    private static string? SpecArgumentName(Term argument)
    {
        if (argument is TupleTerm { Items: [Atom { Name: "ann_type" }, _, Cons annotated] })
        {
            argument = annotated.Head;
        }

        return argument is TupleTerm { Items: [Atom { Name: "var" }, _, Atom variable] } ? variable.Name : null;
    }

    private Term ReadTypeForm(string attribute)
    {
        if (attribute is ModuleAttributeNames.ExportType or "optional_callbacks")
        {
            Expect(ErlangSyntaxTokens.OpenParenthesis);
            int offset = Current.Start;
            Term entries = AttributeLiteral.Normalize(Expression(), offset);
            Expect(ErlangSyntaxTokens.CloseParenthesis);

            return Cons.From(SourceCallBindingBuilder.ReadSignatures(entries, offset)
                .Select(entry => Term.Tuple(Term.A(entry.Name), Term.I(entry.Arity))));
        }

        bool parenthesized = Take(ErlangSyntaxTokens.OpenParenthesis);
        string name = Name();
        if (attribute is "spec" or "callback")
        {
            if (Take(ErlangSyntaxTokens.ModuleQualifier))
            {
                if (attribute == "callback" || parsingModule is not null && name != parsingModule)
                {
                    throw Error(ParserDiagnostics.InvalidTypeFormQualifier);
                }

                name = Name();
            }

            var signatures = new List<Term>();
            do
            {
                Term parameters = ReadTypeFormArguments(ReadType);
                Expect(ErlangSyntaxTokens.FunctionArrow);
                Term result = ReadType();
                if (Take(ErlangKeywords.When))
                {
                    var constraints = new List<Term>();
                    do
                    {
                        constraints.Add(ReadTypeConstraint());
                    }
                    while (Take(ErlangSyntaxTokens.Comma));
                    signatures.Add(Term.Tuple(parameters, result, Cons.From(constraints)));
                }
                else
                {
                    signatures.Add(Term.Tuple(parameters, result));
                }
            }
            while (Take(ErlangSyntaxTokens.Semicolon));

            if (parenthesized)
            {
                Expect(ErlangSyntaxTokens.CloseParenthesis);
            }
            return Term.Tuple(Term.A(name), Cons.From(signatures));
        }

        Term definition = ReadTypeDefinition(name);
        if (parenthesized)
        {
            Expect(ErlangSyntaxTokens.CloseParenthesis);
        }
        return definition;
    }

    private Term ReadTypeConstraint()
    {
        Term variable = ReadTypeVariable();
        Expect(NativeRecordTypeTokens.Annotation);

        return Term.Tuple(variable, ReadType());
    }

    private Term ReadTypeVariable()
    {
        if (Current.Kind != LexerTokenKinds.Variable)
        {
            throw Error(ParserDiagnostics.ExpectedToken(LexerTokenKinds.Variable, Current.Text));
        }

        return Term.A(tokens[position++].Text);
    }

    private Term ReadTypeFormArguments(Func<Term> read)
    {
        var arguments = new List<Term>();
        Expect(ErlangSyntaxTokens.OpenParenthesis);
        if (!Take(ErlangSyntaxTokens.CloseParenthesis))
        {
            do
            {
                arguments.Add(read());
            }
            while (Take(ErlangSyntaxTokens.Comma));
            Expect(ErlangSyntaxTokens.CloseParenthesis);
        }

        return Cons.From(arguments);
    }

    private Term ReadType()
    {
        var parser = new NativeRecordTypeParser(
            tokens,
            source,
            position,
            allowLocalTypes: true
        );
        Term type = parser.Parse();
        position = parser.Position;

        return type;
    }
}
