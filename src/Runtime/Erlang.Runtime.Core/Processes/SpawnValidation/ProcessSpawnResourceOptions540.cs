using System.Numerics;

namespace Erlang;

// Validation only: this component neither accepts a resource policy for execution
// nor constructs a guest process. The optional default is actual VM policy input.
internal static class ProcessSpawnResourceOptions540
{
    internal static bool IsResourceOption(Term option)
        => option is TupleTerm
        {
            Items: [Atom
            {
                Name:
            ProcessSpawnResourceNames540.MinimumHeap or
            ProcessSpawnResourceNames540.MinimumBinaryHeap or
            ProcessSpawnResourceNames540.MaximumHeap
            }, _]
        };

    internal static ProcessSpawnResourceValidation540 Analyze(
        IReadOnlyList<Term> options,
        int wordBits,
        ulong? actualMinimumHeapDefault = null
    )
    {
        PinnedHeapSizeClasses508? classes = null;
        bool hasResource = false;
        bool invalid = false;
        ulong minimum = actualMinimumHeapDefault ?? 0;
        ulong maximum = 0;
        foreach (Term option in options)
        {
            if (!IsResourceOption(option))
                continue;
            classes ??= new PinnedHeapSizeClasses508(wordBits);
            hasResource = true;
            var tuple = (TupleTerm)option;
            string name = ((Atom)tuple.Items[0]).Name;
            Term value = tuple.Items[1];
            if (name == ProcessSpawnResourceNames540.MaximumHeap)
            {
                if (!TryMaximum(value, classes.MaximumNativeSmall, out ulong nextMaximum))
                    invalid = true;
                else
                    maximum = nextMaximum;
                continue;
            }
            if (!TrySmall(value, classes.MaximumNativeSmall, out ulong requested))
            {
                invalid = true;
                continue;
            }
            if (name != ProcessSpawnResourceNames540.MinimumHeap)
                continue;
            // A request outside the representable size table is not invented
            // into badarg. The lower bound is still sufficient to prove a
            // nonzero max<min contradiction; actual allocation stays refused.
            ulong rounded;
            try
            {
                rounded = classes.Next(requested);
            }
            catch (ArgumentOutOfRangeException)
            {
                rounded = requested;
            }
            minimum = Math.Max(actualMinimumHeapDefault ?? 0, rounded);
        }
        if (maximum != 0 && maximum < minimum)
            invalid = true;

        return new(hasResource, invalid,
            hasResource && maximum != 0 && actualMinimumHeapDefault is null);
    }

    private static bool TrySmall(Term value, BigInteger maximum, out ulong result)
    {
        result = 0;
        if (value is not Integer number || number.Value < 0 || number.Value > maximum)
            return false;
        result = (ulong)number.Value;

        return true;
    }

    private static bool TryMaximum(Term value, BigInteger nativeMaximum, out ulong size)
    {
        if (value is not MapTerm map)
            return TrySmall(value, nativeMaximum, out size);
        size = 0;
        if (!map.TryGet(Term.A(ProcessSpawnResourceNames540.Size), out Term? requested)
            || requested is null || !TrySmall(requested, nativeMaximum, out size))
            return false;
        foreach (string flag in new[]
        {
            ProcessSpawnResourceNames540.Kill,
            ProcessSpawnResourceNames540.ErrorLogger,
            ProcessSpawnResourceNames540.IncludeSharedBinaries
        })
        {
            if (map.TryGet(Term.A(flag), out Term? setting)
                && setting is not Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False })
                return false;
        }

        return true;
    }
}
