using System.Numerics;
using static Erlang.Tests.VmStorageBirthAccess;

namespace Erlang.Tests;

// No direct counter increments, field writes, synthetic heap or
// guest execution admission. Every write is to the real public VM dictionary.
internal static class VmStorageBirthChecks
{
    internal static async Task Birth()
    {
        await using var runtime = new ProcessRuntime(1);
        await runtime.InitializeModulesAsync(_ => { });
        Require(OwnerCount(runtime) == 1);
        var observed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var process = runtime.Spawn(context =>
        {
            // First body statement: no canonical Source/native call/public GC.
            bool born = HasRegisteredBacking(context);
            observed.TrySetResult(born && !ExecutionEnabled(context)
                && Property(context, VmStorageBirthNames.CurrentInvocation) is null);

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        Require(await observed.Task.WaitAsync(VmStorageBirthNames.Timeout));
        await process.Completion.WaitAsync(VmStorageBirthNames.Timeout);
    }

    internal static async Task DictionaryCollection()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Require(HasRegisteredBacking(context) && !ExecutionEnabled(context));
            Term hostOnly = Payload();
            var initial = await Sample(context);
            Require((await Sample(context)) == initial);
            Term keep = Term.Tuple(Term.I(31), Term.I(47));
            context.Dictionary[Term.A(VmStorageBirthNames.Keep)] = keep;
            context.Dictionary[Term.A(VmStorageBirthNames.Drop)] = Payload();
            Require(context.Dictionary.Remove(Term.A(VmStorageBirthNames.Drop)));
            await Collect(context);
            var after = await Sample(context);
            Require(after.Collections == initial.Collections + 1 && after.Words > initial.Words);
            Require(context.Dictionary[Term.A(VmStorageBirthNames.Keep)].Equals(keep));
            Require(hostOnly.Equals(Payload()) && !ExecutionEnabled(context));
            GC.KeepAlive(hostOnly);

            return true;
        }).WaitAsync(VmStorageBirthNames.Timeout);
    }

    internal static async Task Isolation()
    {
        await using var first = new ProcessRuntime(1);
        await using var second = new ProcessRuntime(1);
        await using var a = new RuntimeNodeFoundationHostActor(first);
        await using var b = new RuntimeNodeFoundationHostActor(second);
        var beforeA = await a.InvokeAsync(Sample);
        var beforeB = await b.InvokeAsync(Sample);
        // This is deliberately HOST CLR work, not a logical VM collection.
        byte[] host = new byte[1024 * 1024];
        host[0] = 17;
        GC.Collect(
            GC.MaxGeneration,
            GCCollectionMode.Forced,
            blocking: true,
            compacting: true
        );
        GC.WaitForPendingFinalizers();
        Require(await a.InvokeAsync(Sample) == beforeA);
        Require(await b.InvokeAsync(Sample) == beforeB);
        await a.InvokeAsync(async context =>
        {
            Require(!ExecutionEnabled(context));
            context.Dictionary[Term.A(VmStorageBirthNames.Drop)] = Payload();
            context.Dictionary.Remove(Term.A(VmStorageBirthNames.Drop));
            await Collect(context);

            return true;
        });
        var afterA = await a.InvokeAsync(Sample);
        Require(afterA.Collections == beforeA.Collections + 1 && afterA.Words > beforeA.Words);
        Require(await b.InvokeAsync(Sample) == beforeB);
        await b.InvokeAsync(async context =>
        {
            context.Dictionary[Term.A(VmStorageBirthNames.Drop)] = Payload();
            context.Dictionary.Remove(Term.A(VmStorageBirthNames.Drop));
            await Collect(context);

            return true;
        });
        var afterB = await b.InvokeAsync(Sample);
        Require(afterB.Collections == beforeB.Collections + 1 && afterB.Words > beforeB.Words);
        Require(await a.InvokeAsync(Sample) == afterA);
        Require(host[0] == 17);
        GC.KeepAlive(host);
    }

    internal static async Task Drain()
    {
        var runtime = new ProcessRuntime(1);
        var held = new TaskCompletionSource<(ProcessContext Context, IDisposable Borrow)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseBody = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IDisposable? borrow = null;
        Task? disposal = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                Require(HasRegisteredBacking(context) && !ExecutionEnabled(context));
                context.Dictionary[Term.A(VmStorageBirthNames.Keep)] = Payload();
                held.TrySetResult((context, BorrowDictionary(context, Term.A(VmStorageBirthNames.Keep))));
                await releaseBody.Task;
            }
            catch (Exception error)
            {
                held.TrySetException(error);
                throw;
            }

            return Nil.Value;
        });
        try
        {
            var state = await held.Task.WaitAsync(VmStorageBirthNames.Timeout);
            borrow = state.Borrow;
            releaseBody.TrySetResult();
            await Until(() => HeapClosing(state.Context));
            Require(!process.Completion.IsCompleted && Registered(state.Context));
            Term value = (Term)Property(borrow, VmStorageBirthNames.BorrowValue)!;
            Require(value.Equals(Payload()));
            disposal = runtime.DisposeAsync().AsTask();
            Require(!disposal.IsCompleted);
            borrow.Dispose();
            borrow = null;
            await process.Completion.WaitAsync(VmStorageBirthNames.Timeout);
            await disposal.WaitAsync(VmStorageBirthNames.Timeout);
            await Until(() => !Registered(state.Context));
        }
        finally
        {
            releaseBody.TrySetResult();
            borrow?.Dispose();
            if (disposal is not null)
                await disposal.WaitAsync(VmStorageBirthNames.Timeout);
            else
                await runtime.DisposeAsync();
        }
    }

    internal static async Task Rollback()
    {
        await using var runtime = new ProcessRuntime(1);
        await runtime.InitializeModulesAsync(_ => { });
        // Real dispatcher retirement refuses the subsequent Dispatch after
        // context creation/registry enrollment. No private field is edited.
        object dispatcher = Field(runtime, VmStorageBirthNames.Dispatcher);
        await ((Task)Call(dispatcher, VmStorageBirthNames.Retire)).WaitAsync(VmStorageBirthNames.Timeout);
        int before = OwnerCount(runtime);
        bool entered = false;
        bool refused = false;
        try
        {
            runtime.Spawn(_ =>
            {
                entered = true;

                return ValueTask.FromResult<Term>(Nil.Value);
            });
        }
        catch (ObjectDisposedException)
        {
            refused = true;
        }
        Require(refused && !entered);
        await Until(() => OwnerCount(runtime) == before);
    }
}
