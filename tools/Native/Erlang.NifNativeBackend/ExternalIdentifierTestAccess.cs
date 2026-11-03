using System.Collections;
using System.Reflection;

namespace Erlang.NifNativeBackend;

internal static class ExternalIdentifierTestAccess
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static object? Storage(Term value) => value.GetType().GetProperty(ExternalIdentifierCaseNames.Storage, Fields)!.GetValue(value);

    internal static int NodeCount(ProcessRuntime runtime) => ((ICollection)typeof(RuntimeKnownNodes).GetField(ExternalIdentifierCaseNames.PhysicalNodes, Fields)!.GetValue(runtime.KnownNodes)!).Count;

    internal static Term Snapshot(Term value) => value switch
    {
        Pid pid => Term.Tuple(Term.A(pid.Node), new Integer(pid.Id), Term.I(pid.Creation)),
        PortTerm port => Term.Tuple(Term.A(port.Node), new Integer(port.Id), Term.I(port.Creation)),
        ReferenceTerm reference => Term.Tuple(Term.A(reference.Node), Cons.From(reference.Words.Select(number => Term.I(number))), Term.I(reference.Creation)),
        _ => throw new InvalidOperationException()
    };
}
