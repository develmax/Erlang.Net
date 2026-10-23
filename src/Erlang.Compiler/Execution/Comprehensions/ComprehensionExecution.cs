// Modified: CLR comprehension subset of OTP-29.1.1 erl_eval/erl_lint/eval_bits.
// Fresh generator bindings, original key/size scopes and filter error distinctions.
namespace Erlang.Compiler;

internal static class ComprehensionExecution
{
    public static ValueTask<Term> Evaluate(
        Expr.ListComprehension expression,
        Dictionary<string, Term> incoming,
        ProcessContext context,
        ModuleDefinition? module,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate,
        bool tail = false,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>>? evaluateTail = null
    )
        => Evaluate(
            expression.Items,
            expression.Qualifiers,
            false,
            null,
            incoming,
            context,
            module,
            evaluate,
            module is not null && expression.Source is { } source
                ? new CompiledComprehensionFrames(
                    source,
                    module,
                    context,
                    tail
                ) : null,
            evaluateTail
        );

    public static ValueTask<Term> Evaluate(
        Expr.BinaryComprehension expression,
        Dictionary<string, Term> incoming,
        ProcessContext context,
        ModuleDefinition? module,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate
    ) => Evaluate(
        new[] { expression.Body },
        expression.Qualifiers,
        true,
        null,
        incoming,
        context,
        module,
        evaluate
    );

    public static ValueTask<Term> Evaluate(
        Expr.MapComprehension expression,
        Dictionary<string, Term> incoming,
        ProcessContext context,
        ModuleDefinition? module,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate
    ) => Evaluate(
        [],
        expression.Qualifiers,
        false,
        expression.Fields,
        incoming,
        context,
        module,
        evaluate
    );

    private static async ValueTask<Term> Evaluate(
        IReadOnlyList<Expr> items,
        IReadOnlyList<ComprehensionQualifier> qualifiers,
        bool binary,
        IReadOnlyList<MapField>? fields,
        Dictionary<string, Term> incoming,
        ProcessContext context,
        ModuleDefinition? module,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate,
        CompiledComprehensionFrames? frames = null,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>>? evaluateTail = null
    )
    {
        var evaluateOriginal = evaluate;
        if (frames is not null)
        {
            evaluate = (expression, scope) => frames.Run(() => evaluateOriginal(expression, scope));
        }
        var result = new List<Term>();
        var pairs = new List<KeyValuePair<Term, Term>>();
        try
        {
            await Qualifiers(0, CompiledBindingScope.Copy(incoming));
        }
        catch (ErlangException error) when (frames is not null)
        {
            throw frames.Capture(error);
        }
        if (fields is not null)
            return new MapTerm(pairs);
        if (binary)
            return BitConstruction.Create(result.Select(value => (value, (Term?)null, new BitSegment(
                new Expr.Literal(value),
                null,
                BitSegmentTypes.Binary,
                BitSyntaxDefaults.BitStringUnit
            ))).ToArray());

        return Cons.From(result);

        async ValueTask Qualifiers(int index, Dictionary<string, Term> scope)
        {
            if (index == qualifiers.Count)
            {
                if (fields is not null)
                {
                    if (module is not null)
                    {
                        pairs.AddRange(await MapComprehensionBindings.Evaluate(
                            fields,
                            qualifiers,
                            incoming,
                            scope,
                            evaluate
                        ));

                        return;
                    }
                    foreach (var field in fields)
                    {
                        var local = CompiledBindingScope.Copy(scope);
                        Expr[] expressions = [field.Key, field.Value];
                        var fieldValues = await ExpressionBindings.EvaluateList(expressions, local, evaluate);
                        pairs.Add(new(fieldValues[0], fieldValues[1]));
                    }

                    return;
                }
                if (binary)
                {
                    var chunk = await evaluate(items[0], CompiledBindingScope.Copy(scope));
                    if (chunk is not BitString)
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    result.Add(chunk);

                    return;
                }
                if (frames is { TerminalBody: true } && evaluateTail is not null)
                {
                    result.Add(await frames.RunTerminal(() => evaluateTail(items[0], scope)));

                    return;
                }
                var values = module is null
                    ? await ExpressionBindings.EvaluateList(items, scope, evaluate)
                    : await CompiledExpressionBindings.EvaluateList(items, scope, evaluate);
                result.AddRange(values);
                frames?.Emitted();

                return;
            }
            if (qualifiers[index] is ComprehensionQualifier.Zip zip)
            {
                await ZipComprehensionExecution.Evaluate(
                    zip,
                    scope,
                    context,
                    module is not null,
                    evaluate,
                    async nested =>
                    {
                        await Qualifiers(index + 1, nested);
                        frames?.Resume(index);
                    },
                    frames is null ? null : () => frames.EnterGenerator(index),
                    frames is null ? null : () => frames.Resume(index)
                );

                return;
            }
            if (qualifiers[index] is ComprehensionQualifier.Generator generator)
            {
                Term source = await evaluate(generator.Source, CompiledBindingScope.Copy(scope));
                frames?.EnterGenerator(index);
                while (source is Cons cell)
                {
                    frames?.Resume(index);
                    if (frames is null)
                    {
                        await context.ReduceAsync();
                    }
                    else
                    {
                        await frames.Reduce();
                    }
                    var fresh = new Dictionary<string, Term>(StringComparer.Ordinal);
                    bool matched = frames is null
                        ? await generator.Pattern.MatchAsync(
                            cell.Head,
                            fresh,
                            context,
                            scope
                        )
                        : await frames.Run(() => generator.Pattern.MatchAsync(
                            cell.Head,
                            fresh,
                            context,
                            scope
                        ));
                    if (matched)
                    {
                        var nested = new Dictionary<string, Term>(scope, StringComparer.Ordinal);
                        foreach (var binding in fresh)
                            nested[binding.Key] = binding.Value;
                        await Qualifiers(index + 1, nested);
                        frames?.Resume(index);
                    }
                    else if (generator.Strict)
                        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), cell.Head));
                    source = cell.Tail;
                }
                if (source is not Nil)
                    throw new ErlangException(Term.Tuple(Term.A(ComprehensionErrorReasons.BadGenerator), source));

                return;
            }
            if (qualifiers[index] is ComprehensionQualifier.MapGenerator mapGenerator)
            {
                Term source = await evaluate(mapGenerator.Source, CompiledBindingScope.Copy(scope));
                frames?.EnterGenerator(index);
                IReadOnlyList<KeyValuePair<Term, Term>> entries;
                try
                {
                    entries = MapIteration.Entries(source, context.Runtime);
                }
                catch (ErlangException)
                {
                    throw new ErlangException(Term.Tuple(Term.A(ComprehensionErrorReasons.BadGenerator), source));
                }
                foreach (var entry in entries)
                {
                    frames?.Resume(index);
                    if (frames is null)
                    {
                        await context.ReduceAsync();
                    }
                    else
                    {
                        await frames.Reduce();
                    }
                    var fresh = new Dictionary<string, Term>(StringComparer.Ordinal);
                    var pair = Term.Tuple(entry.Key, entry.Value);
                    if (await mapGenerator.Pattern.MatchAsync(
                        pair,
                        fresh,
                        context,
                        scope
                    ))
                    {
                        var nested = CompiledBindingScope.Copy(scope);
                        foreach (var binding in fresh)
                            nested[binding.Key] = binding.Value;
                        await Qualifiers(index + 1, nested);
                        frames?.Resume(index);
                    }
                    else if (mapGenerator.Strict)
                        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), pair));
                }

                return;
            }
            if (qualifiers[index] is ComprehensionQualifier.BinaryGenerator binaryGenerator)
            {
                Term source = await evaluate(binaryGenerator.Source, CompiledBindingScope.Copy(scope));
                frames?.EnterGenerator(index);
                if (source is not BitString input)
                    throw new ErlangException(Term.Tuple(Term.A(ComprehensionErrorReasons.BadGenerator), source));
                input.PinForMatch();
                byte[] bytes = input.ToArray();
                int position = 0;
                while (true)
                {
                    frames?.Resume(index);
                    if (frames is null)
                    {
                        await context.ReduceAsync();
                    }
                    else
                    {
                        await frames.Reduce();
                    }
                    var fresh = new Dictionary<string, Term>(StringComparer.Ordinal);
                    var readResult = await binaryGenerator.Pattern.ReadGeneratorAsync(
                        input,
                        bytes,
                        position,
                        fresh,
                        context,
                        scope
                    );
                    bool complete = readResult.Complete;
                    int consumed = readResult.Consumed;
                    bool matched = readResult.Matched;
                    if (!complete)
                    {
                        if (binaryGenerator.Strict && position != input.BitLength)
                            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), Remaining()));

                        return;
                    }
                    if (consumed == 0)
                        throw new ErlangException(ErlangErrorReasons.SystemLimit);
                    if (matched)
                    {
                        var nested = new Dictionary<string, Term>(scope, StringComparer.Ordinal);
                        foreach (var binding in fresh)
                            nested[binding.Key] = binding.Value;
                        await Qualifiers(index + 1, nested);
                        frames?.Resume(index);
                    }
                    else if (binaryGenerator.Strict)
                        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), Remaining()));
                    position += consumed;
                }

                BitString Remaining()
                {
                    int count = input.BitLength - position;
                    var rest = new byte[(int)(((long)count + BitStorageLayout.ByteRoundingOffset) / BitStorageLayout.BitsPerByte)];
                    for (int bit = 0; bit < count; bit++)
                        if (((bytes[(position + bit) / BitStorageLayout.BitsPerByte] >> (BitStorageLayout.MostSignificantBitIndex - (position + bit) % BitStorageLayout.BitsPerByte)) & 1) != 0)
                            rest[bit / BitStorageLayout.BitsPerByte] |= (byte)(1 << (BitStorageLayout.MostSignificantBitIndex - bit % BitStorageLayout.BitsPerByte));

                    return new BitString(rest, count);
                }
            }
            var filter = (ComprehensionQualifier.Filter)qualifiers[index];
            frames?.SetLocation(filter.SourceLine);
            if (ComprehensionGuard.IsTest(filter.Expression, module))
            {
                bool accepted = frames is null
                    ? await Execution.GuardAsync(filter.Expression, scope, context)
                    : await frames.Run(() => Execution.GuardAsync(filter.Expression, scope, context));
                if (accepted)
                    await Qualifiers(index + 1, scope);

                return;
            }
            var filterScope = CompiledBindingScope.Copy(scope);
            var value = await evaluate(filter.Expression, filterScope);
            if (value.Equals(Term.A(ErlangBooleanAtoms.True)))
                await Qualifiers(index + 1, filterScope);
            else if (!value.Equals(Term.A(ErlangBooleanAtoms.False)))
                throw new ErlangException(Term.Tuple(Term.A(ComprehensionErrorReasons.BadFilter), value));
        }
    }
}
