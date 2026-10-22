using System.Buffers.Binary;

namespace Erlang;

internal static class ProplistFamilyToMapFunction
{
    internal static FunctionTerm Create(ProcessContext context)
    {
        var fingerprint = (BitString)context.Runtime.Modules.ModuleInformation(
            ProplistFamilyNames.Module,
            ModuleInformationKeys.Md5
        );
        var identity = new LocalFunctionIdentity(
            Term.A(ProplistFamilyNames.Module),
            Term.A(ProplistFamilyFoldNames.Callback),
            ProplistFamilyFoldNames.CallbackIndex,
            (int)(BinaryPrimitives.ReadUInt32BigEndian(fingerprint.ToArray())
                >> (FunctionCodeLayout.UniquePrefixBits - FunctionCodeLayout.OldUniqueBits)),
            fingerprint
        );

        FunctionTerm Materialize(IReadOnlyList<Term> environment) => new(
            identity,
            ProplistFamilyFoldNames.CallbackArity,
            environment,
            Invoke
        );
        identity = context.Runtime.Functions.Register(
            identity,
            ProplistFamilyFoldNames.CallbackArity,
            ProplistFamilyFoldNames.EnvironmentCount,
            Materialize
        );

        return Materialize([]);
    }

    private static ValueTask<Term> Invoke(
        ITermExecutionContext execution,
        IReadOnlyList<Term> arguments
    )
    {
        if (execution is not ProcessContext context)
            throw new ArgumentException(ProplistFamilyFoldDiagnostics.ProcessContextRequired);
        Term property = arguments[0];
        Term accumulator = arguments[1];
        if (property is TupleTerm { Items.Count: 2 } pair)
        {
            return ValueTask.FromResult<Term>(Put(
                context,
                pair.Items[0],
                pair.Items[1],
                accumulator,
                ProplistFamilyFoldNames.PairUpdateLine
            ));
        }
        if (property is TupleTerm { Items.Count: > 0 } tuple)
        {
            return context.Runtime.Modules.Call(
                context,
                MapModuleNames.Module,
                MapModuleNames.Remove,
                tuple.Items[0],
                accumulator
            );
        }
        if (property is Atom)
        {
            return ValueTask.FromResult<Term>(Put(
                context,
                property,
                ProplistFamilyNames.True,
                accumulator,
                ProplistFamilyFoldNames.AtomUpdateLine
            ));
        }

        return ValueTask.FromResult(accumulator);
    }

    private static MapTerm Put(
        ProcessContext context,
        Term key,
        Term value,
        Term accumulator,
        int line
    )
    {
        if (accumulator is not MapTerm map)
        {
            throw ProplistFamilyTrace.Capture(
                context,
                new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), accumulator)),
                ProplistFamilyTrace.Frame(
                    ProplistFamilyFoldNames.Callback,
                    Term.I(ProplistFamilyFoldNames.CallbackArity),
                    line
                )
            );
        }
        var values = map.Entries.ToDictionary(entry => entry.Key, entry => entry.Value);
        values[key] = value;

        return new MapTerm(values);
    }

    internal static void FoldInto(Term property, Dictionary<Term, Term> values)
    {
        if (property is TupleTerm { Items.Count: 2 } pair)
            values[pair.Items[0]] = pair.Items[1];
        else if (property is TupleTerm { Items.Count: > 0 } tuple)
            values.Remove(tuple.Items[0]);
        else if (property is Atom)
            values[property] = ProplistFamilyNames.True;
    }
}
