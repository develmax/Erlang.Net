using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
using static Erlang.Tests.UnicodeTraversalCancellationNames;
using static Erlang.Tests.LibrarySelectionCancellationPhaseAccess;

namespace Erlang.Tests;

internal static class UnicodeTraversalCancellationHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        string[] dependencies = JsonSerializer.Deserialize<string[]>(Read("dependencies.json"))!;
        PreparedBeamImage image = runtime.Modules.PrepareBeam(Read(UnicodeModule + ".beam"), new BeamDependencySelection(dependencies));
        Require(image.NativePrimitiveExports.Count == 3);
        Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        byte[] chunk = Enumerable.Repeat((byte)65, ChunkBytes).ToArray();
        Term input = Nil.Value;
        var binary = new BitString(chunk);
        for (int index = 0; index < ChunkCount; index++) input = new Cons(binary, input);
        for (int index = 0; index < Depth; index++) input = new Cons(input, Nil.Value);
        Term sentinel = Term.Tuple(Term.A(Sentinel), Term.I(392));
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var outcome = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle worker = runtime.Spawn(async context =>
        {
            context.Mailbox.Send(sentinel);
            entered.TrySetResult(context);
            try
            {
                Term result = await runtime.Modules.Call(
                    context,
                    UnicodeModule,
                    Function,
                    input,
                    Term.A(Encoding)
                );
                outcome.TrySetResult(result);

                return result;
            }
            catch (Exception error)
            {
                outcome.TrySetException(error);
                throw;
            }
            finally
            {
                unwound.TrySetResult();
            }
        });
        Exception? primary = null;
        try
        {
            ProcessContext active = await entered.Task.WaitAsync(Deadline);
            PropertyInfo counter = typeof(ProcessContext).GetProperty(Counter, BindingFlags.Instance | BindingFlags.NonPublic)!;
            using var limit = new CancellationTokenSource(Deadline);
            while ((long)counter.GetValue(active)! < MinimumObservedWork)
            {
                limit.Token.ThrowIfCancellationRequested();
                Require(!worker.Completion.IsCompleted && !outcome.Task.IsCompleted);
                await Task.Yield();
            }
            Require(!outcome.Task.IsCompleted && active.Mailbox.Count == 1 && ActiveLeases(active) > 0);
            var beforeExit = (IReadOnlyList<Term>)typeof(Mailbox).GetMethod(Snapshot, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(active.Mailbox, null)!;
            Require(beforeExit.Count == 1 && beforeExit[0].Equals(sentinel));
            await controller.Invoke(context =>
{
    runtime.Exit(context, worker.Pid, Term.A(Kill));

    return true;
});
            Require((await worker.Completion.WaitAsync(Deadline)).Equals(Term.A(ProcessExitReasons.Killed)));
            await unwound.Task.WaitAsync(Deadline);
            await Unwound(active).WaitAsync(Deadline);
            bool cancelled = false;
            try
            {
                _ = await outcome.Task.WaitAsync(Deadline);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            Require(cancelled && active.Cancellation.IsCancellationRequested && ActiveLeases(active) == 0 && active.Mailbox.Count == 0);
            var messages = (IReadOnlyList<Term>)typeof(Mailbox).GetMethod(Snapshot, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(active.Mailbox, null)!;
            Require(messages.Count == 0);
            runtime.Send(worker.Pid, sentinel);
            Require(active.Mailbox.Count == 0);
            lock (RuntimeGate(runtime)) Require(!ProcessesWhileLocked(runtime).Any(context => context.Self.Equals(worker.Pid)));
            Require(controller.Process.Completion.IsCompleted == false);
            await controller.InvokeAsync(async context =>
            {
                Require(context.Mailbox.Count == 0);
                Term fresh = await runtime.Modules.Call(
                    context,
                    UnicodeModule,
                    Function,
                    Term.List(Term.I(65)),
                    Term.A(Encoding)
                );
                Require(fresh.Equals(new BitString(new byte[] { 65 })) && context.Mailbox.Count == 0 && ActiveLeases(context) == 0);

                return true;
            }).WaitAsync(Deadline);
        }
        catch (Exception error)
        {
            primary = error;
            throw;
        }
        finally
        {
            try
            {
                if (!worker.Completion.IsCompleted)
                    await controller.Invoke(context =>
{
    runtime.Exit(context, worker.Pid, Term.A(Kill));

    return true;
});
                await worker.Completion.WaitAsync(Deadline);
                await unwound.Task.WaitAsync(Deadline);
                if (outcome.Task.IsFaulted) _ = outcome.Task.Exception;
            }
            catch (Exception cleanup) when (primary is not null)
            {
                throw new AggregateException(primary, cleanup);
            }
        }
    }

    private static byte[] Read(string name)
    {
        var assembly = typeof(UnicodeTraversalCancellationHostChecks).Assembly;
        using Stream manifest = assembly.GetManifestResourceStream(ResourcePrefix + "manifest.json")!;
        using JsonDocument document = JsonDocument.Parse(manifest);
        string expected = document.RootElement.GetProperty(name).GetString()!;
        using Stream stream = assembly.GetManifestResourceStream(ResourcePrefix + name)!;
        using var output = new MemoryStream();
        stream.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)).Equals(expected, StringComparison.OrdinalIgnoreCase));

        return bytes;
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }
}
