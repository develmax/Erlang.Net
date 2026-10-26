using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class ProcessRetirement500Checks
{
    internal static async Task Run()
    {
        byte[] manifest = Resource(ProcessRetirement500Names.Manifest);
        if (Convert.ToHexString(SHA256.HashData(manifest)) != ProcessRetirement500Names.ManifestSha256)
            throw new InvalidDataException(ProcessLiveness499Names.Failure);
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(manifest)!;

        Term Row(Term value) => Term.List(Term.Tuple(Term.A(ProcessRetirement500Names.Case), Term.Tuple(Term.A(ProcessRetirement500Names.Success), value)));
        Term values = Row(await RunScenario(null));
        var profiles = new List<Term>();
        foreach (int? depth in ProcessRetirement500Names.Depths)
            profiles.Add(Term.Tuple(depth is int n ? Term.I(n) : Term.A(ProcessRetirement500Names.Default), Row(await RunScenario(depth))));
        foreach (var pair in new[] { (ProcessRetirement500Names.Values, values), (ProcessRetirement500Names.Stacks, Cons.From(profiles)) })
        {
            byte[] expected = Resource(pair.Item1);
            if (Convert.ToHexString(SHA256.HashData(expected)) != hashes[pair.Item1] || !ExternalTermFormat.Encode(pair.Item2).AsSpan().SequenceEqual(expected))
                throw new InvalidDataException(ProcessLiveness499Names.Failure);
        }
    }

    private static byte[] Resource(string name)
    {
        using Stream input = typeof(ProcessRetirement500Checks).Assembly.GetManifestResourceStream(ProcessRetirement500Names.Prefix + name)
            ?? throw new InvalidDataException(ProcessLiveness499Names.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }

    private static async Task<Term> RunScenario(int? depth)
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapProcessAlive425.Register(runtime, registry);
            RuntimeBootstrapDirtyQuery423.Register(runtime, registry);
        });
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext child = await target.Ready;
        if (depth is int backtraceDepth)
            await observer.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                ProcessLiveness499Names.Erlang,
                ProcessRetirement500Names.SystemFlag,
                Term.A(ProcessRetirement500Names.BacktraceDepth),
                Term.I(backtraceDepth)
            ));
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        int finished = 0;
        Task<bool> work = target.InvokeAsync(async context =>
        {
            runtime.Register(ProcessRetirement500Names.Registered, context.Self);
            await DirtyNative419Access.Schedule(
                context,
                DirtyNative419Names.Cpu,
                [],
                (_, _) =>
                {
                    entered.Set();
                    if (!release.Wait(ProcessLiveness499Names.Deadline))
                        throw new InvalidOperationException(ProcessLiveness499Names.Failure);

                    Volatile.Write(ref finished, 1);

                    return Nil.Value;
                }
            );

            return true;
        });
        Task<Term>? query = null;
        Term? internalReply = null;
        bool physical;
        try
        {
            if (!entered.Wait(ProcessLiveness499Names.Deadline))
                throw new InvalidOperationException(ProcessLiveness499Names.Failure);
            physical = DirtyNative419Access.Field<int>(DirtyNative419Access.Snapshot(runtime), DirtyNative419Names.Running) == 1;
            if (!physical)
                throw new InvalidOperationException(ProcessLiveness499Names.Failure);
            await observer.Invoke(context =>
            {
                runtime.Exit(context, child.Self, Term.A(ProcessLiveness499Names.Kill));

                return true;
            });
            if (target.Process.Completion.IsCompleted)
                throw new InvalidOperationException(ProcessLiveness499Names.Failure);
            query = observer.InvokeAsync(async context =>
            {
                ReferenceTerm reference = runtime.MakeReference();
                await runtime.Modules.Call(
                    context,
                    ProcessRetirement500Names.Internal,
                    ProcessLiveness499Names.Alive,
                    child.Self,
                    reference
                );
                Term result = await runtime.Modules.Call(
                    context,
                    ProcessLiveness499Names.Erlang,
                    ProcessLiveness499Names.Alive,
                    child.Self
                );
                Term? reply = await context.Mailbox.ReceiveAsync<Term>(
                    value => value is TupleTerm pair && pair.Items[0].Equals(reference) ? value : null,
                    ProcessLiveness499Names.Deadline,
                    context.Cancellation
                );
                internalReply = ((TupleTerm)reply!).Items[1];

                return result;
            });
            if (!(await query.WaitAsync(ProcessLiveness499Names.Deadline)).Equals(Term.A(ErlangBooleanAtoms.False)))
                throw new InvalidOperationException(ProcessLiveness499Names.Failure);
            if (!runtime.WhereIs(ProcessRetirement500Names.Registered).Equals(Term.A(ProcessRegistryAtoms.Undefined)))
                throw new InvalidOperationException(ProcessLiveness499Names.Failure);
            if (target.Process.Completion.IsCompleted || Volatile.Read(ref finished) != 0)
                throw new InvalidOperationException(ProcessLiveness499Names.Failure);
            // This is a logical dirty-status query, not a physical worker completion marker.
            Term dirty = await observer.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                ProcessRetirement500Names.Internal,
                ProcessRetirement500Names.DirtyQuery,
                child.Self
            ));
            Term observation = Term.Tuple(
                Term.A(physical ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False),
                await query,
                runtime.WhereIs(ProcessRetirement500Names.Registered),
                dirty,
                internalReply!
            );
            release.Set();
            await target.Process.Completion.WaitAsync(ProcessLiveness499Names.Deadline);
            if (Volatile.Read(ref finished) != 1)
                throw new InvalidOperationException(ProcessLiveness499Names.Failure);

            return observation;
        }
        finally
        {
            release.Set();
            try
            {
                await work.WaitAsync(ProcessLiveness499Names.Deadline);
            }
            catch (OperationCanceledException) { }
            if (query is not null)
                await query.WaitAsync(ProcessLiveness499Names.Deadline);
        }
    }
}
