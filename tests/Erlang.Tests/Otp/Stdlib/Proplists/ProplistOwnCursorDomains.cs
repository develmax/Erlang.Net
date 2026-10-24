using System.Numerics;
using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class ProplistOwnCursorDomains
{
    internal static async Task<IReadOnlyList<ProplistOwnCursorDomain>> Create(
        ProcessContext context,
        GeneratedModuleArtifact compiled,
        ReferenceTerm alias
    )
    {
        ProcessRuntime runtime = context.Runtime;

        Term Bind(Term term) => runtime.ImportExternalTerm(ExternalTermFormat.Encode(term)).Term;
        FunctionTerm local = (FunctionTerm)await Execution.InvokeAsync(
            compiled.Definition,
            ProplistOwnCursorNames.LocalFactory,
            context,
            [Term.I(42)]
        );
        FunctionTerm external = runtime.Functions.External(runtime, new(
            Term.A(ProplistOwnCursorNames.Erlang),
            Term.A(ProplistOwnCursorNames.Alias),
            0));
        var record = new NativeRecordTerm(
            Term.A(ProplistOwnCursorNames.RecordModule),
            Term.A(ProplistOwnCursorNames.RecordName),
            false,
            [new(Term.A(ProplistOwnCursorNames.RecordField), Term.I(1))]
        );

        return new ProplistOwnCursorDomain[]
        {
            new(ProplistOwnCursorDomainNames.Atom, () => Term.A(ProplistOwnCursorNames.FirstAtom)),
            new(ProplistOwnCursorDomainNames.Float, () => new FloatTerm(1.25)),
            new(ProplistOwnCursorDomainNames.Integer, () => new Integer(BigInteger.One << 128)),
            new(ProplistOwnCursorDomainNames.Tuple, () => Term.Tuple(Term.A(ProplistOwnCursorNames.FirstAtom), Term.I(1))),
            new(ProplistOwnCursorDomainNames.List, () => Term.List(Term.I(1), Term.I(2))),
            new(ProplistOwnCursorDomainNames.ImproperList, () => new Cons(Term.I(1), Term.I(2))),
            new(ProplistOwnCursorDomainNames.Binary, () => new BitString([1, 2, 3])),
            new(ProplistOwnCursorDomainNames.Bits, () => new BitString([160], 3)),
            new(ProplistOwnCursorDomainNames.LocalPid, () => context.Self),
            new(ProplistOwnCursorDomainNames.RemotePid, () => Bind(new Pid(ProplistOwnCursorNames.Foreign, 1, 7))),
            new(ProplistOwnCursorDomainNames.LocalPort, () => Bind(new PortTerm(runtime.Node, 2, runtime.InitializationPid.Creation))),
            new(ProplistOwnCursorDomainNames.RemotePort, () => Bind(new PortTerm(ProplistOwnCursorNames.Foreign, 3, 7))),
            new(ProplistOwnCursorDomainNames.LocalReference, () => runtime.MakeReference()),
            new(
                ProplistOwnCursorDomainNames.RemoteReference,
                () => Bind(new ReferenceTerm(ProplistOwnCursorNames.Foreign, [1U, 2U, 3U, 4U, 5U], 7))
            ),
            new(ProplistOwnCursorDomainNames.Alias, () => alias),
            new(ProplistOwnCursorDomainNames.LocalFunction, () => local),
            new(ProplistOwnCursorDomainNames.ExternalFunction, () => external),
            new(ProplistOwnCursorDomainNames.HostFunction, () => new FunctionTerm(1, (_, arguments) => ValueTask.FromResult(arguments[0]))),
            new(ProplistOwnCursorDomainNames.Record, () => record),
            new(ProplistOwnCursorDomainNames.Map, () => new MapTerm([new(Term.A(ProplistOwnCursorNames.FirstAtom), record)])),
            new(ProplistOwnCursorDomainNames.WidePid, () => new Pid(runtime.Node, ulong.MaxValue, runtime.InitializationPid.Creation)),
            new(
                ProplistOwnCursorDomainNames.WidePort,
                () => new PortTerm(runtime.Node, ulong.MaxValue, runtime.InitializationPid.Creation)
            ),
            new(
                ProplistOwnCursorDomainNames.WideEnvironment,
                () => new FunctionTerm(
                    local.LocalIdentity!,
                    0,
                    Enumerable.Range(0, ProplistOwnCursorNames.LargeEnvironment).Select(index => Term.I(index)),
                    (_, arguments) => ValueTask.FromResult<Term>(Term.I(arguments.Count))
                )
            )
        };
    }
}
