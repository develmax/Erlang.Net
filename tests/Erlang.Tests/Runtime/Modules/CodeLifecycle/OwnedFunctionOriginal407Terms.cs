namespace Erlang.Tests;

internal static class OwnedFunctionOriginal407Terms
{
    internal static Term Decode(byte[] bytes) => ExternalTermFormat.DecodeOne(
        bytes,
        new EtfDecodeOptions
        {
            ExternalFunctionFactory = (identity, _) => new FunctionTerm(identity, Refuse),
            LocalFunctionFactory = (code, arity, environment) => new FunctionTerm(
                new LocalFunctionIdentity(
                    code.Module,
                    null,
                    code.Index,
                    code.OldUnique,
                    code.Unique,
                    code.OldIndex
                ),
                arity,
                environment,
                Refuse
            )
        }
    ).Term;

    private static ValueTask<Term> Refuse(ITermExecutionContext context, IReadOnlyList<Term> arguments)
        => throw new InvalidOperationException(OwnedFunctionOriginal407Names.PassiveFunction);

    internal static object Typed(Term term) => term switch
    {
        FunctionTerm function => new
        {
            Kind = nameof(FunctionTerm),
            function.Arity,
            LocalIdentity = function.LocalIdentity is not { } local ? null : new
            {
                Module = local.Module.Name,
                Name = local.Name?.Name,
                local.Index,
                local.OldIndex,
                local.OldUnique,
                Unique = Typed(local.Unique)
            },
            ExternalIdentity = function.ExternalIdentity is not { } external ? null : new
            {
                Module = external.Module.Name,
                Name = external.Function.Name,
                external.Arity
            },
            Environment = function.Environment.Select(Typed).ToArray(),
            PassiveDescription = true
        },
        TupleTerm tuple => new { Kind = nameof(TupleTerm), Items = tuple.Items.Select(Typed).ToArray() },
        Cons cell => TypedList(cell),
        MapTerm map => new { Kind = nameof(MapTerm), Entries = map.Entries.Select(pair => new { Key = Typed(pair.Key), Value = Typed(pair.Value) }).ToArray() },
        _ => IoPrintableOriginal404Terms.Typed(term)
    };

    private static object TypedList(Cons start)
    {
        var items = new List<object>();
        Term tail = start;
        while (tail is Cons cell)
        {
            items.Add(Typed(cell.Head));
            tail = cell.Tail;
        }

        return new { Kind = nameof(Cons), Items = items.ToArray(), Tail = Typed(tail) };
    }
}
