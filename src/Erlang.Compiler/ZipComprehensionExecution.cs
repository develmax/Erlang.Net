// Modified: CLR adaptation of OTP-29.1.1 erl_eval zip and v3_core joint clauses.
namespace Erlang.Compiler;

internal static class ZipComprehensionExecution
{
    public static async ValueTask Evaluate(
        ComprehensionQualifier.Zip zip,
        Dictionary<string, Term> incoming,
        ProcessContext context,
        bool compiled,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate,
        Func<Dictionary<string, Term>, ValueTask> body
    )
    {
        var definitions = ComprehensionGenerator.Flatten(zip.Generators).ToArray();
        var cursors = new ZipGeneratorCursor[definitions.Length];
        var sourceScope = CompiledBindingScope.Copy(incoming);
        for (int index = 0; index < definitions.Length; index++)
            cursors[index] = new(definitions[index], await evaluate(definitions[index].Source, compiled ? sourceScope : CompiledBindingScope.Copy(incoming)));
        var strictVariables = definitions.Where(generator => generator.Strict).SelectMany(generator => Semantics.Variables(generator.Pattern)).ToHashSet(StringComparer.Ordinal);
        var skipPatterns = definitions.Select(generator => generator.Strict ? generator.Pattern : ZipSkipPattern.Create(generator.Pattern, strictVariables)).ToArray();
        var patterns = definitions.Select(generator => generator.Pattern).ToArray();
        var complete = new bool[cursors.Length];
        var consumed = new int[cursors.Length];
        while (true)
        {
            await context.ReduceAsync();
            for (int index = 0; index < cursors.Length; index++)
                complete[index] = cursors[index].Read(
                    definitions[index].Pattern,
                    incoming,
                    new(StringComparer.Ordinal),
                    context,
                    out _,
                    out _
                );

            if (compiled)
            {
                var row = new Dictionary<string, Term>(StringComparer.Ordinal);
                bool matches = JointMatch(patterns, row);
                if (!matches)
                {
                    row.Clear();
                    if (!JointMatch(skipPatterns, row))
                    {
                        if (cursors.All(cursor => cursor.CompiledTail))
                            return;

                        throw Failure();
                    }
                }
                if (matches)
                {
                    var scope = CompiledBindingScope.Copy(incoming);
                    foreach (var binding in row)
                        scope[binding.Key] = binding.Value;
                    await body(scope);
                }
            }
            else
            {
                var ends = cursors.Select((cursor, index) => cursor.NormalizedEnd(complete[index])).ToArray();
                if (ends.Any(value => value) && !ends.All(value => value))
                    throw Failure();
                var row = new Dictionary<string, Term>(StringComparer.Ordinal);
                bool skip = false;
                for (int index = 0; index < cursors.Length; index++)
                {
                    var cursor = cursors[index];
                    var scope = row;
                    var fresh = new Dictionary<string, Term>(StringComparer.Ordinal);
                    bool read = cursor.Read(
                        definitions[index].Pattern,
                        scope,
                        fresh,
                        context,
                        out bool matched,
                        out consumed[index]
                    );
                    if (!read)
                    {
                        if (cursor.IsDone(false) && !cursor.HasStrictBinaryTail)
                            return;

                        throw Failure();
                    }
                    int conflict = matched ? Merge(fresh, row, strictVariables) : ZipBindingConflict.None;
                    if (!matched || conflict != ZipBindingConflict.None)
                    {
                        if (definitions[index].Strict || conflict == ZipBindingConflict.Strict)
                            throw Failure();
                        skip = true;
                    }
                    else if (!skip || definitions[index].Strict)
                        foreach (var binding in fresh)
                            row[binding.Key] = binding.Value;
                }
                if (!skip)
                {
                    var scope = CompiledBindingScope.Copy(incoming);
                    foreach (var binding in row)
                        scope[binding.Key] = binding.Value;
                    await body(scope);
                }
            }
            if (consumed.All(value => value == 0))
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            for (int index = 0; index < cursors.Length; index++)
                cursors[index].Advance(consumed[index]);
        }

        bool JointMatch(IReadOnlyList<Pattern> patterns, Dictionary<string, Term> row)
        {
            for (int index = 0; index < cursors.Length; index++)
            {
                var fresh = new Dictionary<string, Term>(StringComparer.Ordinal);
                if (!cursors[index].Read(
                    patterns[index],
                    incoming,
                    fresh,
                    context,
                    out bool matched,
                    out consumed[index]
                ) || !matched || Merge(fresh, row, strictVariables) != ZipBindingConflict.None)
                    return false;
                foreach (var binding in fresh)
                    row[binding.Key] = binding.Value;
            }

            return true;
        }

        ErlangException Failure() => new(Term.Tuple(
            Term.A(ComprehensionErrorReasons.BadGenerators),
            new TupleTerm(cursors.Select((cursor, index) => cursor.Remainder(!compiled, complete[index])).ToArray())
        ));
    }

    private static int Merge(Dictionary<string, Term> fresh, Dictionary<string, Term> row, IReadOnlySet<string> strictVariables)
    {
        foreach (var binding in fresh.OrderBy(binding => Term.A(binding.Key)))
            if (row.TryGetValue(binding.Key, out var value) && !value.Equals(binding.Value))
                return strictVariables.Contains(binding.Key) ? ZipBindingConflict.Strict : ZipBindingConflict.Relaxed;

        return ZipBindingConflict.None;
    }
}
