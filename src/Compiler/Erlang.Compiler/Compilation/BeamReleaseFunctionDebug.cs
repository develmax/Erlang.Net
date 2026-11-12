namespace Erlang.Compiler;

internal static class BeamReleaseFunctionDebug
{
    internal static Term AbstractAnnotation(Token token, string source)
    {
        Term location = BeamDocumentationWriter.Annotation(token, source);
        if (token is { Kind: LexerTokenKinds.QuotedAtom, Text: ErlangKeywords.Maybe or ErlangKeywords.Else })
        {
            string quoted = "'" + token.Text + "'";
            if (source.AsSpan(token.Start, token.End - token.Start).SequenceEqual(quoted))
            {
                return Term.List(
                    Term.Tuple(Term.A("text"), Term.String(quoted)),
                    Term.Tuple(Term.A("location"), location));
            }
        }

        return location;
    }

    internal static Term WriteRecord(
        IReadOnlyList<Token> tokens,
        int start,
        string source,
        Parser parser
    )
    {
        int cursor = start;
        // Record fields are inside braces. Function types within them do not open expression blocks.
        var arguments = BeamMacroArguments.Read(tokens, ref cursor, trackExpressionBlocks: false);
        if (arguments is not [[var name], var fields]
            || name.Kind is not (LexerTokenKinds.Atom or LexerTokenKinds.QuotedAtom))
        {
            throw Unsupported();
        }
        Token eof = tokens[cursor] with { Kind = LexerTokenKinds.EndOfInput, Text = string.Empty };
        if (fields.Any(token => token.Kind == LexerTokenKinds.Symbol && token.Text == NativeRecordTypeTokens.Annotation))
        {
            return Term.Tuple(Term.A(name.Text), WriteTypedRecordFields(fields, source, parser, name.Text));
        }
        if (parser.ParseDebugExpression([.. fields, eof]) is not Expr.Tuple tuple)
        {
            throw Unsupported();
        }
        return Term.Tuple(Term.A(name.Text), new ExpressionWriter(fields, 0, source, parser).WriteRecordFields(tuple));
    }

    private static Term WriteTypedRecordFields(IReadOnlyList<Token> tokens, string source, Parser parser, string name)
    {
        if (tokens[0].Text != ErlangSyntaxTokens.OpenTuple)
        {
            throw Unsupported();
        }

        var result = new List<Term>();
        int index = 1;
        while (index < tokens.Count)
        {
            Token field = tokens[index++];
            if (field.Kind is not (LexerTokenKinds.Atom or LexerTokenKinds.QuotedAtom))
            {
                throw Unsupported();
            }
            Term location = BeamDocumentationWriter.Annotation(field, source);
            Term fieldName = Term.Tuple(Term.A("atom"), location, Term.A(field.Text));
            Term definition = Term.Tuple(Term.A("record_field"), location, fieldName);
            if (tokens[index].Text == ErlangOperators.Match)
            {
                Expr value = parser.FindDebugRecord(name)?.Fields.FirstOrDefault(item => item.Name == field.Text)?.Value
                    ?? throw Unsupported();
                var writer = new ExpressionWriter(tokens, index + 1, source, parser);
                definition = Term.Tuple(Term.A("record_field"), location, fieldName, writer.Write(value));
                index = writer.Position;
            }
            if (tokens[index].Text == NativeRecordTypeTokens.Annotation)
            {
                var reader = new NativeRecordTypeParser(tokens, source, index + 1, allowLocalTypes: true);
                definition = Term.Tuple(Term.A("typed_record_field"), definition, reader.Parse());
                index = reader.Position;
            }
            result.Add(definition);
            Token delimiter = tokens[index++];
            if (delimiter.Text == ErlangSyntaxTokens.CloseTuple && index == tokens.Count)
            {
                return Cons.From(result);
            }
            if (delimiter.Text != ErlangSyntaxTokens.Comma)
            {
                break;
            }
        }

        throw Unsupported();
    }

    internal static Term Write(
        FunctionDefinition function,
        IReadOnlyList<Token> tokens,
        int start,
        string source,
        Parser parser
    )
    {
        var clauses = new List<Term>();
        int cursor = start;
        foreach (Clause clause in function.Clauses)
        {
            Token head = tokens[cursor++];
            var arguments = BeamMacroArguments.Read(tokens, ref cursor);
            if (arguments.Count != clause.Patterns.Count)
            {
                throw Unsupported();
            }
            var patterns = new List<Term>();
            for (int index = 0; index < arguments.Count; index++)
            {
                var reader = new ExpressionWriter(arguments[index], 0, source, parser);
                patterns.Add(reader.Write(PatternExpression(clause.Patterns[index])));
            }

            var body = new ExpressionWriter(tokens, cursor + 1, source, parser);
            Term guards = body.WriteOptionalGuard(clause.Guard);
            Term expressions = body.WriteClauseBody(clause.Body);
            clauses.Add(Term.Tuple(
                Term.A("clause"),
                BeamDocumentationWriter.Annotation(head, source),
                Cons.From(patterns),
                guards,
                expressions
            ));
            cursor = body.Position + 1;
        }

        return Term.Tuple(
            Term.A("function"),
            BeamDocumentationWriter.Annotation(tokens[start], source),
            Term.A(function.Name),
            Term.I(function.Arity),
            Cons.From(clauses)
        );
    }

    private static Expr PatternExpression(Pattern pattern) => pattern switch
    {
        Pattern.Variable variable => new Expr.Variable(variable.Name),
        Pattern.Literal literal => PatternArithmetic.Source(literal),
        Pattern.List list => Parser.DebugPrefixPattern(list)
            ?? new Expr.List(list.Items.Select(PatternExpression).ToArray(), list.Tail is null ? null : PatternExpression(list.Tail)),
        Pattern.Tuple tuple => new Expr.Tuple(tuple.Items.Select(PatternExpression).ToArray()),
        AliasPattern alias => new Expr.Match(alias.Left, PatternExpression(alias.Right)),
        MapPattern map => new Expr.Map(null, map.Fields.Select(field =>
            new MapField(field.Key, PatternExpression(field.Value), true)).ToArray()),
        BitPattern bits => new Expr.Bits(bits.Segments.Select(segment =>
            segment.Specification with { Value = PatternExpression(segment.Value) }).ToArray()),
        _ => throw Unsupported()
    };

    private sealed class ExpressionWriter(IReadOnlyList<Token> tokens, int start, string source, Parser parser)
    {
        private int cursor = start;

        internal int Position => cursor;

        internal Term WriteRecordFields(Expr.Tuple record)
        {
            Take(ErlangSyntaxTokens.OpenTuple);
            var fields = new List<Term>();
            foreach (Expr field in record.Items)
            {
                if (fields.Count != 0)
                {
                    Take(ErlangSyntaxTokens.Comma);
                }
                Term location = Annotation(tokens[cursor]);
                if (field is Expr.Match { Pattern: Pattern.Literal { Value: Atom name }, Value: var value })
                {
                    Term atom = Write(new Expr.Literal(name));
                    Take(ErlangOperators.Match);
                    fields.Add(Term.Tuple(Term.A("record_field"), location, atom, Write(value)));
                }
                else if (field is Expr.Literal { Value: Atom })
                {
                    fields.Add(Term.Tuple(Term.A("record_field"), location, Write(field)));
                }
                else
                {
                    throw Unsupported();
                }
            }
            Take(ErlangSyntaxTokens.CloseTuple);
            return Cons.From(fields);
        }

        internal Term WriteClauseBody(Expr body)
        {
            Take(ErlangSyntaxTokens.FunctionArrow);
            return WriteBody(body);
        }

        private Term WriteBody(Expr body) => body is Expr.Sequence sequence
            ? Cons.From(WriteItems(sequence.Items))
            : Cons.From([Write(body)]);

        internal Term WriteOptionalGuard(Expr? guard)
        {
            if (guard is null)
            {
                return Nil.Value;
            }
            TakeKeyword(ErlangKeywords.When);
            return WriteGuards(guard);
        }

        private Term WriteGuards(Expr guard)
        {
            var alternatives = guard is Expr.GuardAlternatives list ? list.Items : [guard];
            var forms = new List<Term>();
            foreach (Expr alternative in alternatives)
            {
                if (forms.Count != 0)
                {
                    Take(ErlangSyntaxTokens.Semicolon);
                }
                var terms = new List<Term>();
                WriteGuardTerms(alternative, terms);
                forms.Add(Cons.From(terms));
            }
            return Cons.From(forms);
        }

        private void WriteGuardTerms(Expr guard, List<Term> terms)
        {
            if (guard is Expr.Binary { Operator: ErlangOperators.AndAlso, SourceLine: 0 } conjunction)
            {
                WriteGuardTerms(conjunction.Left, terms);
                Take(ErlangSyntaxTokens.Comma);
                terms.Add(Write(conjunction.Right));
                return;
            }
            terms.Add(Write(guard));
        }

        internal IReadOnlyList<Term> WriteItems(IReadOnlyList<Expr> items)
        {
            var result = new List<Term>();
            for (int index = 0; index < items.Count; index++)
            {
                if (index != 0)
                {
                    Take(ErlangSyntaxTokens.Comma);
                }
                result.Add(Write(items[index]));
            }

            return result;
        }

        internal Term Write(Expr expression)
        {
            Token token = tokens[cursor];
            Term location = Annotation(token);
            if (token is { Kind: LexerTokenKinds.Symbol, Text: ErlangSyntaxTokens.OpenParenthesis }
                && !ContinuesAfterGroup(expression, TokenAfterParentheses()))
            {
                Take(ErlangSyntaxTokens.OpenParenthesis);
                Term inner = Write(expression);
                Take(ErlangSyntaxTokens.CloseParenthesis);
                return inner;
            }
            switch (expression)
            {
                case Expr.Literal when token is { Kind: LexerTokenKinds.Atom, Text: RecordAtoms.RecordInfo }
                    && cursor + 1 < tokens.Count && tokens[cursor + 1].Text == ErlangSyntaxTokens.OpenParenthesis:
                    cursor++;
                    Take(ErlangSyntaxTokens.OpenParenthesis);
                    Term kind = Write(new Expr.Literal(Term.A(tokens[cursor].Text)));
                    Take(ErlangSyntaxTokens.Comma);
                    Term recordName = Write(new Expr.Literal(Term.A(tokens[cursor].Text)));
                    Take(ErlangSyntaxTokens.CloseParenthesis);
                    return Term.Tuple(Term.A("call"), location,
                        Term.Tuple(Term.A("atom"), location, Term.A(RecordAtoms.RecordInfo)), Cons.From([kind, recordName]));
                case Expr.Variable variable when token.Kind == LexerTokenKinds.Variable:
                    cursor++;
                    return Term.Tuple(Term.A("var"), location, Term.A(variable.Name));
                case Expr.Literal { Value: Atom atom } when token.Kind is LexerTokenKinds.Atom or LexerTokenKinds.QuotedAtom:
                    cursor++;
                    return Term.Tuple(Term.A("atom"), location, atom);
                case Expr.Literal { Value: Integer integer } when token.Kind == LexerTokenKinds.Integer:
                    cursor++;
                    return Term.Tuple(Term.A(token.IsCharacterLiteral ? "char" : "integer"), location, integer);
                case Expr.Literal { Value: FloatTerm floating } when token.Kind == LexerTokenKinds.Float:
                    cursor++;
                    return Term.Tuple(Term.A("float"), location, floating);
                case Expr.Literal { Value: Integer } when token.Text == ErlangSyntaxTokens.MapPrefix:
                    Take(ErlangSyntaxTokens.MapPrefix);
                    string indexedRecord = tokens[cursor].Text;
                    TakeToken(indexedRecord);
                    Take(ErlangSyntaxTokens.RecordSelectionSeparator);
                    Term indexedField = Write(new Expr.Literal(Term.A(tokens[cursor].Text)));
                    return Term.Tuple(Term.A("record_index"), location, Term.A(indexedRecord), indexedField);
                case Expr.Literal { Value: Integer integer } when token.Text == ErlangOperators.Minus && integer.Value < 0:
                    Take(ErlangOperators.Minus);
                    return Term.Tuple(Term.A("op"), location, Term.A(ErlangOperators.Minus), Write(new Expr.Literal(new Integer(-integer.Value))));
                case Expr.Binary binary:
                    Term leftOperand = Write(binary.Left);
                    Term operatorLocation = Annotation(tokens[cursor]);
                    TakeToken(binary.Operator);
                    return Term.Tuple(Term.A("op"), operatorLocation, Term.A(binary.Operator), leftOperand, Write(binary.Right));
                case Expr.Unary unary:
                    TakeToken(unary.Operator);
                    return Term.Tuple(Term.A("op"), location, Term.A(unary.Operator), Write(unary.Operand));
                case Expr.Catch caught:
                    TakeKeyword(ErlangKeywords.Catch);
                    return Term.Tuple(Term.A(ErlangKeywords.Catch), location, Write(caught.Operand));
                case Expr.Literal when token.IsStringSigil:
                    cursor++;
                    return WriteSigil(token, location);
                case Expr.Literal literal when token.Kind == LexerTokenKinds.String:
                    StringLiteralTokens.Read(tokens, ref cursor);
                    return Term.Tuple(Term.A("string"), location, literal.Value);
                case Expr.RecordConstruction record:
                    return WriteRecordFields(RecordExpressions.Create(record), location, record.Wildcard);
                case Expr.RecordUpdate update:
                    Term original = Write(update.Value);
                    Term updateLocation = Annotation(tokens[cursor]);
                    Take(ErlangSyntaxTokens.MapPrefix);
                    TakeToken(update.Name);
                    Term updatedFields = ReadRecordFields(update.Name, update.Arity,
                        index => update.Fields.SingleOrDefault(field => field.Index == index)?.Value);
                    return Term.Tuple(Term.A("record"), updateLocation, original, Term.A(update.Name), updatedFields);
                case Expr.RecordAccess access:
                    Term target = Write(access.Value);
                    Term accessLocation = Annotation(tokens[cursor]);
                    Take(ErlangSyntaxTokens.MapPrefix);
                    TakeToken(access.Name);
                    Take(ErlangSyntaxTokens.RecordSelectionSeparator);
                    Term accessField = Write(new Expr.Literal(Term.A(tokens[cursor].Text)));
                    return Term.Tuple(Term.A("record_field"), accessLocation, target, Term.A(access.Name), accessField);
                case Expr.Tuple tuple when token.Text == ErlangSyntaxTokens.MapPrefix:
                    return WriteRecordFields(tuple, location);
                case Expr.Tuple tuple:
                    Take(ErlangSyntaxTokens.OpenTuple);
                    var members = WriteItems(tuple.Items);
                    Take(ErlangSyntaxTokens.CloseTuple);
                    return Term.Tuple(Term.A("tuple"), location, Cons.From(members));
                case Expr.List list when MatchSpecSourceTransform.OriginalCall(list) is { } matchSpecCall:
                    return BeamMatchSpecAbstractForms.Write(Write(matchSpecCall));
                case Expr.List list:
                    return WriteList(list, location);
                case Expr.Map map:
                    return WriteMap(map, location);
                case Expr.Match match:
                    Term left = Write(PatternExpression(match.Pattern));
                    Take(ErlangOperators.Match);
                    return Term.Tuple(Term.A("match"), location, left, Write(match.Value));
                case Expr.Fun fun:
                    return WriteFun(fun, location);
                case Expr.FunctionReference reference:
                    TakeKeyword(ErlangKeywords.Fun);
                    Write(new Expr.Literal(Term.A(reference.Name)));
                    Take(ErlangSyntaxTokens.AritySeparator);
                    Write(new Expr.Literal(Term.I(reference.Arity)));
                    return Term.Tuple(Term.A("fun"), location,
                        Term.Tuple(Term.A("function"), Term.A(reference.Name), Term.I(reference.Arity)));
                case Expr.Call
                {
                    Module: ErlangModuleNames.Module, Function: FunctionReferenceLoweringNames.MakeFunction,
                    Arguments: [var moduleName, var functionName, var arity]
                } when token is { Kind: LexerTokenKinds.Keyword, Text: ErlangKeywords.Fun }:
                    TakeKeyword(ErlangKeywords.Fun);
                    Term moduleForm = Write(moduleName);
                    Take(ErlangSyntaxTokens.ModuleQualifier);
                    Term functionForm = Write(functionName);
                    Take(ErlangSyntaxTokens.AritySeparator);
                    return Term.Tuple(Term.A("fun"), location,
                        Term.Tuple(Term.A("function"), moduleForm, functionForm, Write(arity)));
                case Expr.Call { DynamicRemote: true, Arguments: [var remoteModule, var remoteFunction, Expr.List { Tail: null } remoteArguments] }:
                    return WriteRemote(remoteModule, remoteFunction, remoteArguments.Items, location);
                case Expr.Call { Module: ErlangModuleNames.Module, Function: RecordPredicateNames.IsRecord, Arguments.Count: 3 } record:
                    return WriteRecordPredicateCall(record, location);
                case Expr.Call { Module: { } module, DynamicRemote: false } call:
                    return WriteRemote(new Expr.Literal(Term.A(module)), new Expr.Literal(Term.A(call.Function)), call.Arguments, location);
                case Expr.Call { Module: null } call:
                    return WriteCall(Write(new Expr.Literal(Term.A(call.Function))), call.Arguments, location);
                case Expr.Apply apply:
                    Term callee = Write(apply.Function);
                    return WriteCall(callee, apply.Arguments, AnnotationEdge(callee, first: true));
                case Expr.Case @case:
                    return WriteCase(@case, location);
                case Expr.Receive receive:
                    return WriteReceive(receive, location);
                case Expr.Maybe conditionalMatch:
                    return WriteMaybe(conditionalMatch, location);
                case Expr.MaybeMatch match:
                    Term tested = Write(PatternExpression(match.Pattern));
                    Term matchLocation = Annotation(tokens[cursor]);
                    Take(ErlangSyntaxTokens.ConditionalMatch);
                    return Term.Tuple(Term.A("maybe_match"), matchLocation, tested, Write(match.Value));
                case Expr.Block block:
                    TakeKeyword(ErlangKeywords.Begin);
                    Term expressions = WriteBody(block.Body);
                    TakeKeyword(ErlangKeywords.End);
                    return Term.Tuple(Term.A("block"), location, expressions);
                case Expr.If conditional:
                    return WriteIf(conditional, location);
                case Expr.Try protectedBody:
                    return WriteTry(protectedBody, location);
                case Expr.Bits bits:
                    return WriteBits(bits, location);
                case Expr.ListComprehension comprehension:
                    return WriteListComprehension(comprehension, location);
                case Expr.BinaryComprehension comprehension:
                    return WriteBinaryComprehension(comprehension, location);
                case Expr.MapComprehension comprehension:
                    return WriteMapComprehension(comprehension, location);
                default:
                    throw Unsupported();
            }
        }

        private static Term WriteSigil(Token token, Term location)
        {
            var coordinates = ((TupleTerm)location).Items;
            var column = (Integer)coordinates[1];
            Term delimiter = Term.Tuple(coordinates[0], new Integer(column.Value + token.SigilDelimiterOffset));
            Term text = Term.Tuple(Term.A("string"), delimiter, Term.String(token.Text));
            if (token.Kind == LexerTokenKinds.String)
            {
                return text;
            }

            Term segment = Term.Tuple(Term.A("bin_element"), delimiter, text, Term.A("default"), Term.List(Term.A("utf8")));

            return Term.Tuple(Term.A("bin"), location, Term.List(segment));
        }

        private Term WriteCase(Expr.Case expression, Term location)
        {
            TakeKeyword(ErlangKeywords.Case);
            Term value = Write(expression.Value);
            TakeKeyword(ErlangKeywords.Of);
            Term clauses = WritePatternClauses(expression.Clauses);
            TakeKeyword(ErlangKeywords.End);

            return Term.Tuple(Term.A("case"), location, value, clauses);
        }

        private Term WriteReceive(Expr.Receive expression, Term location)
        {
            TakeKeyword(ErlangKeywords.Receive);
            Term clauses = WritePatternClauses(expression.Clauses);
            Term result;
            if (expression.Timeout is { } timeout && expression.After is { } after)
            {
                TakeKeyword(ErlangKeywords.After);
                Term delay = Write(timeout);
                result = Term.Tuple(Term.A(ErlangKeywords.Receive), location, clauses, delay, WriteClauseBody(after));
            }
            else
            {
                result = Term.Tuple(Term.A(ErlangKeywords.Receive), location, clauses);
            }

            TakeKeyword(ErlangKeywords.End);

            return result;
        }

        private Term WriteMaybe(Expr.Maybe expression, Term location)
        {
            TakeKeyword(ErlangKeywords.Maybe);
            Term body = Cons.From(WriteItems(expression.Items));
            Term result;
            if (expression.Clauses.Count == 0)
            {
                result = Term.Tuple(Term.A(ErlangKeywords.Maybe), location, body);
            }
            else
            {
                Term elseLocation = Annotation(tokens[cursor]);
                TakeKeyword(ErlangKeywords.Else);
                Term clauses = WritePatternClauses(expression.Clauses);
                result = Term.Tuple(Term.A(ErlangKeywords.Maybe), location, body,
                    Term.Tuple(Term.A(ErlangKeywords.Else), elseLocation, clauses));
            }

            TakeKeyword(ErlangKeywords.End);

            return result;
        }

        private Term WritePatternClauses(IReadOnlyList<Clause> source)
        {
            var clauses = new List<Term>();
            for (int index = 0; index < source.Count; index++)
            {
                if (index != 0)
                {
                    Take(ErlangSyntaxTokens.Semicolon);
                }
                Clause clause = source[index];
                if (clause.Patterns is not [var pattern])
                {
                    throw Unsupported();
                }
                Term clauseLocation = Annotation(tokens[cursor]);
                Term input = Write(PatternExpression(pattern));
                Term guards = WriteOptionalGuard(clause.Guard);
                clauses.Add(Term.Tuple(
                    Term.A("clause"),
                    clauseLocation,
                    Cons.From([input]),
                    guards,
                    WriteClauseBody(clause.Body)
                ));
            }
            return Cons.From(clauses);
        }

        private Term WriteIf(Expr.If expression, Term location)
        {
            TakeKeyword(ErlangKeywords.If);
            var clauses = new List<Term>();
            foreach (Clause clause in expression.Clauses)
            {
                if (clauses.Count != 0)
                {
                    Take(ErlangSyntaxTokens.Semicolon);
                }
                Term start = Annotation(tokens[cursor]);
                Term guards = WriteGuards(clause.Guard ?? throw Unsupported());
                clauses.Add(Term.Tuple(Term.A("clause"), start, Nil.Value, guards, WriteClauseBody(clause.Body)));
            }
            TakeKeyword(ErlangKeywords.End);
            return Term.Tuple(Term.A("if"), location, Cons.From(clauses));
        }

        private Term WriteTry(Expr.Try expression, Term location)
        {
            TakeKeyword(ErlangKeywords.Try);
            Term body = WriteBody(expression.Body);
            var success = new List<Term>();
            if (expression.Clauses.Count != 0)
            {
                TakeKeyword(ErlangKeywords.Of);
                foreach (Clause clause in expression.Clauses)
                {
                    if (success.Count != 0)
                    {
                        Take(ErlangSyntaxTokens.Semicolon);
                    }
                    Term start = Annotation(tokens[cursor]);
                    Term input = Write(PatternExpression(clause.Patterns.Single()));
                    Term guards = WriteOptionalGuard(clause.Guard);
                    success.Add(Term.Tuple(Term.A("clause"), start, Cons.From([input]), guards, WriteClauseBody(clause.Body)));
                }
            }
            var handlers = new List<Term>();
            if (expression.Catches.Count != 0)
            {
                TakeKeyword(ErlangKeywords.Catch);
            }
            foreach (Clause clause in expression.Catches)
            {
                if (clause.Patterns is not [Pattern.Tuple { Items: [var exceptionClass, var reason, var stack] }])
                {
                    throw Unsupported();
                }
                if (handlers.Count != 0)
                {
                    Take(ErlangSyntaxTokens.Semicolon);
                }
                Term start = Annotation(tokens[cursor]);
                Term classForm;
                Term reasonForm;
                if (tokens[cursor + 1].Text == ErlangSyntaxTokens.ModuleQualifier)
                {
                    classForm = Write(PatternExpression(exceptionClass));
                    Take(ErlangSyntaxTokens.ModuleQualifier);
                    reasonForm = Write(PatternExpression(reason));
                }
                else
                {
                    reasonForm = Write(PatternExpression(reason));
                    start = AnnotationEdge(reasonForm, first: true);
                    classForm = Term.Tuple(Term.A("atom"), start, Term.A(ErlangExceptionClasses.Throw));
                }
                Term stackForm;
                if (stack is Pattern.Variable { Name: VariableScopeNames.Wildcard }
                    && tokens[cursor].Text != ErlangSyntaxTokens.ModuleQualifier)
                {
                    stackForm = Term.Tuple(Term.A("var"), AnnotationEdge(reasonForm, first: false), Term.A(VariableScopeNames.Wildcard));
                }
                else
                {
                    Take(ErlangSyntaxTokens.ModuleQualifier);
                    stackForm = Write(PatternExpression(stack));
                }
                Term input = Term.Tuple(Term.A("tuple"), start, Cons.From([classForm, reasonForm, stackForm]));
                Term guards = WriteOptionalGuard(clause.Guard);
                handlers.Add(Term.Tuple(Term.A("clause"), start, Cons.From([input]), guards, WriteClauseBody(clause.Body)));
            }
            Term after = Nil.Value;
            if (expression.After is { } cleanup)
            {
                TakeKeyword(ErlangKeywords.After);
                after = WriteBody(cleanup);
            }
            TakeKeyword(ErlangKeywords.End);
            return Term.Tuple(Term.A("try"), location, body, Cons.From(success), Cons.From(handlers), after);
        }

        private static Term AnnotationEdge(Term form, bool first)
        {
            Term edge = ((TupleTerm)form).Items[1];
            Visit(form);

            return edge;

            void Visit(Term node)
            {
                if (node is TupleTerm tuple)
                {
                    if (tuple.Items is [Atom, TupleTerm { Items: [Integer, Integer] } annotation, ..])
                    {
                        int order = TermOrder.Compare(annotation, edge, exact: true);
                        if (first ? order < 0 : order > 0)
                        {
                            edge = annotation;
                        }
                    }
                    foreach (Term item in tuple.Items)
                    {
                        Visit(item);
                    }
                }
                else if (node is Cons)
                {
                    foreach (Term item in Cons.Items(node))
                    {
                        Visit(item);
                    }
                }
            }
        }

        private Term WriteFun(Expr.Fun fun, Term location)
        {
            TakeKeyword(ErlangKeywords.Fun);
            string? name = tokens[cursor].Kind == LexerTokenKinds.Variable ? tokens[cursor].Text : null;
            var clauses = new List<Term>();
            foreach (Clause clause in fun.Clauses)
            {
                if (clauses.Count != 0)
                {
                    Take(ErlangSyntaxTokens.Semicolon);
                }
                Term start = Annotation(tokens[cursor]);
                if (name is not null)
                {
                    cursor++;
                }
                Take(ErlangSyntaxTokens.OpenParenthesis);
                var patterns = WriteItems(clause.Patterns.Select(PatternExpression).ToArray());
                Take(ErlangSyntaxTokens.CloseParenthesis);
                Term guards = WriteOptionalGuard(clause.Guard);
                Term body = WriteClauseBody(clause.Body);
                clauses.Add(Term.Tuple(
                    Term.A("clause"),
                    start,
                    Cons.From(patterns),
                    guards,
                    body
                ));
            }
            TakeKeyword(ErlangKeywords.End);

            return name is null
                ? Term.Tuple(Term.A("fun"), location, Term.Tuple(Term.A("clauses"), Cons.From(clauses)))
                : Term.Tuple(Term.A("named_fun"), location, Term.A(name), Cons.From(clauses));
        }

        private Term WriteBits(Expr.Bits bits, Term location)
        {
            Take(ErlangSyntaxTokens.BinaryOpen);
            var elements = new List<Term>();
            int segmentIndex = 0;
            while (tokens[cursor].Text != ErlangSyntaxTokens.BinaryClose)
            {
                if (elements.Count != 0)
                {
                    Take(ErlangSyntaxTokens.Comma);
                }
                Term start = Annotation(FirstUngroupedToken());
                Term value;
                BitSegment? segment = null;
                if (tokens[cursor].Kind == LexerTokenKinds.String)
                {
                    int end = cursor;
                    string text = StringLiteralTokens.Read(tokens, ref end);
                    if (tokens[end].Text is ErlangSyntaxTokens.Comma or ErlangSyntaxTokens.BinaryClose
                        || HasUnicodeStringType(end))
                    {
                        Term characters = Term.String(text);
                        int count = Cons.Items(characters).Count();
                        if (segmentIndex + count > bits.Segments.Count
                            || !bits.Segments.Skip(segmentIndex).Take(count).All(segment => segment.SourceStringLiteral))
                        {
                            throw Unsupported();
                        }

                        segmentIndex += count;
                        cursor = end;
                        value = Term.Tuple(Term.A("string"), start, characters);
                    }
                    else
                    {
                        segment = ReadSegment();
                        value = Write(segment.Value);
                    }
                }
                else
                {
                    segment = ReadSegment();
                    value = Write(segment.Value);
                }

                Term size = Term.A("default");
                if (segment?.Size is { } segmentSize)
                {
                    Take(ErlangSyntaxTokens.ModuleQualifier);
                    size = Write(segmentSize);
                }
                Term types = Term.A("default");
                if (tokens[cursor].Text == ErlangOperators.Divide)
                {
                    Take(ErlangOperators.Divide);
                    var options = new List<Term>();
                    while (true)
                    {
                        Token option = tokens[cursor++];
                        if (option.Text == "unit")
                        {
                            if (segment is null)
                            {
                                throw Unsupported();
                            }

                            Take(ErlangSyntaxTokens.ModuleQualifier);
                            TakeToken(segment.Unit.ToString(System.Globalization.CultureInfo.InvariantCulture));
                            options.Add(Term.Tuple(Term.A("unit"), Term.I(segment.Unit)));
                        }
                        else
                        {
                            options.Add(Term.A(option.Text));
                        }
                        if (tokens[cursor].Text != ErlangOperators.Minus)
                        {
                            break;
                        }
                        Take(ErlangOperators.Minus);
                    }
                    types = Cons.From(options);
                }
                elements.Add(Term.Tuple(Term.A("bin_element"), start, value, size, types));
            }
            Take(ErlangSyntaxTokens.BinaryClose);
            return Term.Tuple(Term.A("bin"), location, Cons.From(elements));

            BitSegment ReadSegment() => segmentIndex < bits.Segments.Count
                ? bits.Segments[segmentIndex++]
                : throw Unsupported();

            bool HasUnicodeStringType(int end) =>
                tokens[end].Text == ErlangOperators.Divide
                && (BitUnicode.IsUtf(tokens[end + 1].Text)
                    || tokens[end + 1].Text is BitByteOrders.Big or BitByteOrders.Little or BitByteOrders.Native
                    && tokens[end + 2].Text == ErlangOperators.Minus && BitUnicode.IsUtf(tokens[end + 3].Text));
        }

        private Term WriteListComprehension(Expr.ListComprehension expression, Term location)
        {
            Take(ErlangSyntaxTokens.OpenList);
            var items = WriteItems(expression.Items);
            Term body = items.Count == 1 ? items[0] : Cons.From(items);

            return WriteComprehension("lc", location, body, expression.Qualifiers, ErlangSyntaxTokens.CloseList);
        }

        private Term WriteBinaryComprehension(Expr.BinaryComprehension expression, Term location)
        {
            Take(ErlangSyntaxTokens.BinaryOpen);
            Term body = Write(expression.Body);

            return WriteComprehension("bc", location, body, expression.Qualifiers, ErlangSyntaxTokens.BinaryClose);
        }

        private Term WriteMapComprehension(Expr.MapComprehension expression, Term location)
        {
            Take(ErlangSyntaxTokens.MapPrefix);
            Take(ErlangSyntaxTokens.OpenTuple);
            Term[] fields = WriteMapFields(expression.Fields);
            Term body = fields.Length == 1 ? fields[0] : Cons.From(fields);

            return WriteComprehension("mc", location, body, expression.Qualifiers, ErlangSyntaxTokens.CloseTuple);
        }

        private Term WriteComprehension(
            string kind,
            Term location,
            Term body,
            IReadOnlyList<ComprehensionQualifier> qualifiers,
            string close
        )
        {
            Take(ErlangSyntaxTokens.ComprehensionSeparator);
            var forms = new List<Term>();
            foreach (var qualifier in qualifiers)
            {
                if (forms.Count != 0)
                {
                    Take(ErlangSyntaxTokens.Comma);
                }

                forms.Add(WriteQualifier(qualifier));
            }
            Take(close);

            return Term.Tuple(Term.A(kind), location, body, Cons.From(forms));
        }

        private Term WriteQualifier(ComprehensionQualifier qualifier)
        {
            switch (qualifier)
            {
                case ComprehensionQualifier.Filter filter:
                    return Write(filter.Expression);
                case ComprehensionQualifier.Generator generator:
                    return WriteGenerator(
                        generator.Strict ? "generate_strict" : "generate",
                        generator.Strict ? ErlangSyntaxTokens.StrictListGenerator : ErlangSyntaxTokens.ListGenerator,
                        Write(PatternExpression(generator.Pattern)),
                        generator.Source
                    );
                case ComprehensionQualifier.BinaryGenerator generator:
                    return WriteGenerator(
                        generator.Strict ? "b_generate_strict" : "b_generate",
                        generator.Strict ? ErlangSyntaxTokens.StrictBinaryGenerator : ErlangSyntaxTokens.BinaryGenerator,
                        Write(PatternExpression(generator.Pattern)),
                        generator.Source
                    );
                case ComprehensionQualifier.MapGenerator { Pattern: Pattern.Tuple { Items: [var key, var value] } } generator:
                    return WriteGenerator(
                        generator.Strict ? "m_generate_strict" : "m_generate",
                        generator.Strict ? ErlangSyntaxTokens.StrictListGenerator : ErlangSyntaxTokens.ListGenerator,
                        WriteMapField(new MapField(PatternExpression(key), PatternExpression(value), true)),
                        generator.Source
                    );
                case ComprehensionQualifier.Zip zip:
                    var forms = new List<Term>();
                    foreach (var generator in zip.Generators)
                    {
                        if (forms.Count != 0)
                        {
                            Take(ErlangSyntaxTokens.ZipGeneratorSeparator);
                        }

                        forms.Add(WriteQualifier(generator));
                    }
                    Term location = tokens[cursor].Text == ErlangSyntaxTokens.Comma
                        ? Annotation(tokens[cursor])
                        : ((TupleTerm)forms[0]).Items[1];

                    return Term.Tuple(Term.A("zip"), location, Cons.From(forms));
                default:
                    throw Unsupported();
            }
        }

        private Term WriteGenerator(string kind, string operation, Term pattern, Expr sourceExpression)
        {
            Term location = Annotation(tokens[cursor]);
            Take(operation);

            return Term.Tuple(Term.A(kind), location, pattern, Write(sourceExpression));
        }

        private Term WriteMap(Expr.Map map, Term location)
        {
            Term? baseMap = null;
            if (map.Base is { } value)
            {
                baseMap = Write(value);
                location = Annotation(tokens[cursor]);
            }

            Take(ErlangSyntaxTokens.MapPrefix);
            Take(ErlangSyntaxTokens.OpenTuple);
            Term entries = Cons.From(WriteMapFields(map.Fields));
            Take(ErlangSyntaxTokens.CloseTuple);

            return baseMap is null
                ? Term.Tuple(Term.A("map"), location, entries)
                : Term.Tuple(Term.A("map"), location, baseMap, entries);
        }

        private Term[] WriteMapFields(IReadOnlyList<MapField> fields)
        {
            var forms = new Term[fields.Count];
            for (int index = 0; index < fields.Count; index++)
            {
                if (index != 0)
                {
                    Take(ErlangSyntaxTokens.Comma);
                }

                forms[index] = WriteMapField(fields[index]);
            }

            return forms;
        }

        private Term WriteMapField(MapField field)
        {
            Term key = Write(field.Key);
            Term location = Annotation(tokens[cursor]);
            Take(field.Exact ? ErlangSyntaxTokens.MapExactField : ErlangSyntaxTokens.MapAssociation);

            return Term.Tuple(
                Term.A(field.Exact ? "map_field_exact" : "map_field_assoc"),
                location,
                key,
                Write(field.Value)
            );
        }

        private Term WriteRecordFields(Expr.Tuple tuple, Term location, Expr? wildcard = null)
        {
            Take(ErlangSyntaxTokens.MapPrefix);
            Token name = tokens[cursor++];
            return Term.Tuple(Term.A("record"), location, Term.A(name.Text),
                ReadRecordFields(name.Text, tuple.Items.Count, index => tuple.Items[index], wildcard));
        }

        private Term ReadRecordFields(
            string name,
            int arity,
            Func<int, Expr?> readField,
            Expr? wildcard = null
        )
        {
            if (parser.FindDebugRecord(name) is not { } record || arity != record.Fields.Count + 1)
            {
                throw Unsupported();
            }

            Take(ErlangSyntaxTokens.OpenTuple);
            var fields = new List<Term>();
            while (tokens[cursor].Text != ErlangSyntaxTokens.CloseTuple)
            {
                if (fields.Count != 0)
                {
                    Take(ErlangSyntaxTokens.Comma);
                }

                Token field = tokens[cursor++];
                if (wildcard is not null && field is { Kind: LexerTokenKinds.Variable, Text: VariableScopeNames.Wildcard })
                {
                    Term fieldLocation = Annotation(field);
                    Term fieldName = Term.Tuple(Term.A("var"), fieldLocation, Term.A(VariableScopeNames.Wildcard));
                    Take(ErlangOperators.Match);
                    fields.Add(Term.Tuple(Term.A("record_field"), fieldLocation, fieldName, Write(wildcard)));
                    continue;
                }

                if (field.Kind is not (LexerTokenKinds.Atom or LexerTokenKinds.QuotedAtom))
                {
                    throw Unsupported();
                }

                int index = 0;
                while (index < record.Fields.Count && record.Fields[index].Name != field.Text)
                {
                    index++;
                }
                if (index == record.Fields.Count || readField(index + 1) is not { } value)
                {
                    throw Unsupported();
                }

                Term annotation = Annotation(field);
                Take(ErlangOperators.Match);
                fields.Add(Term.Tuple(Term.A("record_field"), annotation,
                    Term.Tuple(Term.A("atom"), annotation, Term.A(field.Text)), Write(value)));
            }

            Take(ErlangSyntaxTokens.CloseTuple);

            return Cons.From(fields);
        }

        private Term WriteList(Expr.List list, Term location)
        {
            if (tokens[cursor].Kind == LexerTokenKinds.String && list.Tail is { } suffix)
            {
                string text = StringLiteralTokens.Read(tokens, ref cursor);
                Term appendLocation = Annotation(tokens[cursor]);
                Take(ErlangOperators.Append);
                return Term.Tuple(Term.A("op"), appendLocation, Term.A(ErlangOperators.Append),
                    Term.Tuple(Term.A("string"), location, Term.String(text)), Write(suffix));
            }

            Take(ErlangSyntaxTokens.OpenList);
            if (list.Items.Count == 0)
            {
                Take(ErlangSyntaxTokens.CloseList);

                return Term.Tuple(Term.A("nil"), location);
            }
            var items = new List<(Term Location, Term Value)>();
            for (int index = 0; index < list.Items.Count; index++)
            {
                if (index != 0)
                {
                    Take(ErlangSyntaxTokens.Comma);
                }
                Term itemLocation = index == 0 ? location : Annotation(FirstUngroupedToken());
                items.Add((itemLocation, Write(list.Items[index])));
            }

            Term tail;
            if (list.Tail is null)
            {
                tail = Term.Tuple(Term.A("nil"), Annotation(tokens[cursor]));
            }
            else
            {
                Take(ErlangSyntaxTokens.ListTail);
                tail = Write(list.Tail);
            }
            Take(ErlangSyntaxTokens.CloseList);
            for (int index = items.Count - 1; index >= 0; index--)
            {
                tail = Term.Tuple(Term.A("cons"), items[index].Location, items[index].Value, tail);
            }

            return tail;
        }

        private Term WriteRecordPredicateCall(Expr.Call call, Term location)
        {
            Term function;
            if (tokens[cursor].Text == ErlangModuleNames.Module)
            {
                Term module = Write(new Expr.Literal(Term.A(ErlangModuleNames.Module)));
                Term remote = Annotation(tokens[cursor]);
                Take(ErlangSyntaxTokens.ModuleQualifier);
                function = Term.Tuple(Term.A("remote"), remote, module,
                    Write(new Expr.Literal(Term.A(RecordPredicateNames.IsRecord))));
            }
            else
            {
                function = Write(new Expr.Literal(Term.A(RecordPredicateNames.IsRecord)));
            }
            Take(ErlangSyntaxTokens.OpenParenthesis);
            Term value = Write(call.Arguments[0]);
            Take(ErlangSyntaxTokens.Comma);
            Term record = Write(call.Arguments[1]);
            Term arguments = Term.List(value, record);
            if (tokens[cursor].Text == ErlangSyntaxTokens.Comma)
            {
                Take(ErlangSyntaxTokens.Comma);
                arguments = Term.List(value, record, Write(call.Arguments[2]));
            }
            Take(ErlangSyntaxTokens.CloseParenthesis);
            return Term.Tuple(Term.A("call"), location, function, arguments);
        }

        private Term WriteRemote(Expr module, Expr function, IReadOnlyList<Expr> arguments, Term location)
        {
            if (tokens[cursor].Text == ErlangSyntaxTokens.OpenParenthesis)
            {
                int first = cursor;
                while (tokens[first].Text == ErlangSyntaxTokens.OpenParenthesis)
                {
                    first++;
                }
                location = Annotation(tokens[first]);
            }
            Term moduleForm = Write(module);
            Term remoteLocation = Annotation(tokens[cursor]);
            Take(ErlangSyntaxTokens.ModuleQualifier);
            Term remote = Term.Tuple(Term.A("remote"), remoteLocation, moduleForm, Write(function));
            return WriteCall(remote, arguments, location);
        }

        private Term WriteCall(Term function, IReadOnlyList<Expr> arguments, Term location)
        {
            Take(ErlangSyntaxTokens.OpenParenthesis);
            var forms = WriteItems(arguments);
            Take(ErlangSyntaxTokens.CloseParenthesis);
            return Term.Tuple(Term.A("call"), location, function, Cons.From(forms));
        }

        private bool ContinuesAfterGroup(Expr expression, Token? next) => expression switch
        {
            Expr.Binary binary => MatchesDebugOperator(binary, binary.Operator, next) || ContinuesAfterGroup(binary.Left, next),
            Expr.Call { DynamicRemote: true, Arguments: [var module, _, _] } =>
                next?.Text == ErlangSyntaxTokens.ModuleQualifier || ContinuesAfterGroup(module, next),
            Expr.Map { Base: { } map } => MatchesDebugOperator(expression, ErlangSyntaxTokens.MapPrefix, next) || ContinuesAfterGroup(map, next),
            Expr.RecordUpdate record => MatchesDebugOperator(record, ErlangSyntaxTokens.MapPrefix, next) || ContinuesAfterGroup(record.Value, next),
            Expr.RecordAccess record => MatchesDebugOperator(record, ErlangSyntaxTokens.MapPrefix, next) || ContinuesAfterGroup(record.Value, next),
            Expr.Apply apply => MatchesDebugOperator(apply, ErlangSyntaxTokens.OpenParenthesis, next) || ContinuesAfterGroup(apply.Function, next),
            Expr.Match match => MatchesDebugOperator(match, ErlangOperators.Match, next) || ContinuesAfterGroup(PatternExpression(match.Pattern), next),
            _ => false
        };

        private bool MatchesDebugOperator(Expr expression, string operation, Token? token) =>
            parser.FindDebugOperator(expression) is { } expected ? ReferenceEquals(expected, token) : token?.Text == operation;

        private Token? TokenAfterParentheses()
        {
            int depth = 0;
            for (int index = cursor; index + 1 < tokens.Count; index++)
            {
                Token token = tokens[index];
                if (token.Kind != LexerTokenKinds.Symbol)
                {
                    continue;
                }
                if (token.Text == ErlangSyntaxTokens.OpenParenthesis)
                {
                    depth++;
                }
                else if (token.Text == ErlangSyntaxTokens.CloseParenthesis && --depth == 0)
                {
                    return tokens[index + 1];
                }
            }

            return null;
        }
        private Token FirstUngroupedToken()
        {
            int index = cursor;
            while (tokens[index] is { Kind: LexerTokenKinds.Symbol, Text: ErlangSyntaxTokens.OpenParenthesis })
            {
                index++;
            }

            return tokens[index];
        }

        private Term Annotation(Token token) => AbstractAnnotation(token, source);

        private void TakeToken(string text)
        {
            if (cursor >= tokens.Count || tokens[cursor].Text != text)
            {
                throw Unsupported();
            }
            cursor++;
        }

        private void Take(string text)
        {
            if (cursor >= tokens.Count || tokens[cursor].Kind != LexerTokenKinds.Symbol || tokens[cursor].Text != text)
            {
                string found = cursor < tokens.Count ? tokens[cursor].Text : "end of input";
                throw new NotSupportedException($"Expected debug token '{text}', found '{found}'.");
            }
            cursor++;
        }

        private void TakeKeyword(string text)
        {
            if (cursor >= tokens.Count || tokens[cursor].Kind != LexerTokenKinds.Keyword || tokens[cursor].Text != text)
            {
                throw Unsupported();
            }
            cursor++;
        }
    }

    private static NotSupportedException Unsupported() => new(BeamEmissionFormat.Unsupported);
}
