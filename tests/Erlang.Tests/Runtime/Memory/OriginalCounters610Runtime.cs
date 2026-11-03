using System.Reflection;

namespace Erlang.Tests;

internal static class OriginalCounters610Runtime
{
    internal static void RequireOutside(ProcessContext context)
    {
        const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic;
        object runtimeGate = typeof(ProcessRuntime).GetField(OriginalCounters610Fields.RuntimeGateField, members)!.GetValue(context.Runtime)!;
        object mailboxGate = typeof(Mailbox).GetField(OriginalCounters610Fields.MailboxGateField, members)!.GetValue(context.Mailbox)!;
        OriginalCounters610Protocol.Require(!Monitor.IsEntered(runtimeGate) && !Monitor.IsEntered(mailboxGate));
    }

    internal static object[] RequireNativeFive(ProcessRuntime runtime)
    {
        Type native = typeof(ProcessRuntime).Assembly.GetType(OriginalCounters610Fields.ErlangCountersNative421, true)!;
        MethodInfo capture = native.GetMethod(OriginalCounters610Fields.TryCapture, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;

        return OriginalCounters610Names.NativeFive.Select(signature =>
        {
            object?[] args = [runtime, OriginalCounters610Fields.ErtsInternal, signature.Item1, signature.Item2, null];
            OriginalCounters610Protocol.Require((bool)capture.Invoke(null, args)! && args[4] is ErlangFunction);
            var body = (ErlangFunction)args[4]!;
            OriginalCounters610Protocol.Require(body.Method.DeclaringType?.FullName?.StartsWith(native.FullName!, StringComparison.Ordinal) == true);

            return (object)new
            {
                Module = OriginalCounters610Fields.ErtsInternal,
                Function = signature.Item1,
                Arity = signature.Item2,
                DeclaringType = body.Method.DeclaringType!.FullName,
                Method = body.Method.Name,
                CaptureOnlyNotInvocationCounter = true
            };
        }).ToArray();
    }

    internal static ProcessRuntime Create(bool owned, RuntimeNativeMetadataImage metadata, OriginalCounters610Profile profile)
    {
        var options = new ProcessRuntimeOptions { NativeMetadataImage = metadata };
        typeof(ProcessRuntimeOptions).GetProperty(OwnedGuest506Names.PrivateOption, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(options, owned);

        return new ProcessRuntime(
            OriginalAtomics590Names.LocalNode,
            null,
            OriginalCounters610Names.MaximumProcesses,
            null,
            profile.Schedulers,
            options
        );
    }

    internal static async Task RequireProfile(ProcessContext context, OriginalCounters610Profile expected)
    {
        async Task<Term> Info(string field) => await context.Runtime.Modules.Call(
            context,
            OriginalCounters610Fields.Erlang,
            OriginalCounters610Fields.SystemInfo,
            Term.A(field)
        );
        int wordSize = OriginalCounters610Protocol.Int(await Info(OriginalCounters610Fields.Wordsize));
        int schedulers = OriginalCounters610Protocol.Int(await Info(OriginalCounters610Fields.Schedulers));
        Term debug = await Info(OriginalCounters610Fields.DebugCompiled);
        OriginalCounters610Protocol.Require(debug is Atom { Name: OriginalCounters610Fields.True or OriginalCounters610Fields.False });
        // This is the compiled runtime CounterRef layout, not test-assembly DEBUG
        // or provider metadata. Refuse before any memory leaf is compared.
        Type header = typeof(ProcessRuntime).Assembly.GetType(OriginalCounters610Fields.ErlangCountersMagicHeader421, true)!;
        bool hasDebugUlen = header.GetField(OriginalCounters610Fields.CacheLineCount591, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is not null;
        OriginalCounters610Protocol.Require(wordSize == expected.WordSize && schedulers == expected.Schedulers
            && (((Atom)debug).Name == OriginalCounters610Fields.True) == expected.DebugCompiled && hasDebugUlen == expected.DebugCompiled);
        int current = OriginalCounters610Protocol.Int(await Info(OriginalCounters610Fields.SchedulersOnline));
        if (current != expected.Online)
            _ = await context.Runtime.Modules.Call(
                context,
                OriginalCounters610Fields.Erlang,
                OriginalCounters610Fields.SystemFlag,
                Term.A(OriginalCounters610Fields.SchedulersOnline),
                Term.I(expected.Online)
            );
        OriginalCounters610Protocol.Require(OriginalCounters610Protocol.Int(await Info(OriginalCounters610Fields.SchedulersOnline)) == expected.Online);
    }
}
