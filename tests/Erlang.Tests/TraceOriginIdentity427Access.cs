using System.Collections;
using System.Reflection;

namespace Erlang.Tests;

internal static class TraceOriginIdentity427Access
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(TraceOriginIdentity427Names.Failure);
    }

    private static ErlangFunction NativeBody(ModuleRegistry registry, string function, int arity)
    {
        // Read the actual authenticated installed delegate. Calling it directly
        // deliberately tests its own domain guard independently of Call's earlier
        // external-term-domain admission. No binding or process state is changed.
        lock (typeof(ModuleRegistry).GetField(TraceOriginIdentity427Names.GateField, Fields)!.GetValue(registry)!)
            return (ErlangFunction)((IDictionary)typeof(ModuleRegistry).GetField(TraceOriginIdentity427Names.FunctionsField, Fields)!.GetValue(registry)!)[
                (TraceOriginIdentity427Names.Module, function, arity)]!;
    }

    internal static Task<Term> Native(RuntimeNodeFoundationHostActor actor, string function, params Term[] args)
        => actor.InvokeAsync(async context => await NativeBody(context.Runtime.Modules, function, args.Length)(context, args));

    internal static async Task<ErlangException> Bad(RuntimeNodeFoundationHostActor actor, string function, params Term[] args)
    {
        try
        {
            await Native(actor, function, args);
        }
        catch (ErlangException error)
        {
            Require(error.Reason.Equals(Term.A(TraceOriginIdentity427Names.BadArgument)));

            return error;
        }
        throw new InvalidOperationException(TraceOriginIdentity427Names.Failure);
    }

    internal static void Frame(
        ErlangException error,
        string module,
        string function,
        Term[] arguments,
        string? cause
    )
    {
        var first = (TupleTerm)Cons.Items(error.StackTraceTerm).First();
        Require(first.Items.Count == 4 && first.Items[0].Equals(Term.A(module)) && first.Items[1].Equals(Term.A(function))
            && first.Items[2].Equals(Cons.From(arguments)));
        if (cause is not null)
        {
            var entry = (TupleTerm)Cons.Items(first.Items[3]).Single(value => value is TupleTerm tuple
                && tuple.Items[0].Equals(Term.A(TraceOriginIdentity427Names.ErrorInfo)));
            var info = (MapTerm)entry.Items[1];
            Require(info.Entries.Any(pair => pair.Key.Equals(Term.A(TraceOriginIdentity427Names.Cause)) && pair.Value.Equals(Term.A(cause))));
        }
    }
}
