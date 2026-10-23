using System.Text;
using System.Text.Json;
using Erlang;

namespace Erlang.Tests;

internal static class ReUnicodeCancellation453Checks
{
    internal static async Task Run()
    {
        var observations = new List<object>();
        foreach (bool run in new[] { false, true })
            foreach (bool close in new[] { false, true })
                observations.Add(await Conversion(run, close));
        string output = Path.Combine(ReUnicodeCancellation453Names.Output,
            Guid.NewGuid().ToString(ReUnicodeOriginal453Names.GuidFormat));
        Directory.CreateDirectory(output);
        File.WriteAllText(
            Path.Combine(output, ReUnicodeCancellation453Names.Receipt),
            JsonSerializer.Serialize(new { Passed = true, Profiles = observations })
        );
    }

    private static async Task<object> Conversion(bool run, bool close)
    {
        await using ProcessRuntime runtime = await RegexThreeOriginal413Checks.Start(RegexThreeOriginal413Names.Suite);
        RuntimeRegexEngine411 engine = ReUnicodeCancellation453Access.Engine(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        Term options = Term.List(Term.A(RegexNative411Names.Unicode));
        Term? pattern = null;
        if (run)
            pattern = await controller.InvokeAsync(async context => RegexNative411Access.Pattern(
                await engine.CompileAsync(context,
                    new BitString(Encoding.ASCII.GetBytes(ReUnicodeOriginal453Names.BinaryPattern)), options)));
        byte[] chunk = Enumerable.Repeat(ReUnicodeCancellation453Names.Letter,
            ReUnicodeCancellation453Names.ChunkBytes).ToArray();
        Term input = Nil.Value;
        for (int index = 0; index < ReUnicodeCancellation453Names.Chunks; index++)
            input = new Cons(new BitString(chunk), input);
        var before = engine.Report;
        object reOwner = runtime.Modules.SourceOwner(ReUnicodeOriginal453Names.Re,
            run ? ReUnicodeOriginal453Names.UnicodeRun : ReUnicodeOriginal453Names.UnicodeCompile, run ? 3 : 2)!;
        object unicodeOwner = runtime.Modules.SourceOwner(ReUnicodeCancellation453Names.Unicode,
            ReUnicodeCancellation453Names.Convert, 1)!;
        ReUnicodeOriginal453Access.Require(reOwner is PreparedBeamImage && unicodeOwner is PreparedBeamImage);
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var outcome = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle worker = runtime.Spawn(async context =>
        {
            entered.TrySetResult(context);
            try
            {
                Term value = run ? await engine.RunAsync(
                    context,
                    input,
                    pattern!,
                    Nil.Value
                )
                    : await engine.CompileAsync(context, input, options);
                outcome.TrySetResult(value);

                return value;
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
        ProcessContext active = await entered.Task.WaitAsync(ReUnicodeOriginal453Names.Deadline);
        using var limit = new CancellationTokenSource(ReUnicodeOriginal453Names.Deadline);
        while (ReUnicodeCancellation453Access.Work(active) < ReUnicodeCancellation453Names.MinimumWork
            || !ReUnicodeCancellation453Access.HasOriginalLease(active, reOwner)
            || !ReUnicodeCancellation453Access.HasOriginalLease(active, unicodeOwner))
        {
            limit.Token.ThrowIfCancellationRequested();
            ReUnicodeOriginal453Access.Require(!outcome.Task.IsCompleted && !worker.Completion.IsCompleted);
            await Task.Yield();
        }
        var during = engine.Report;
        long observedWork = ReUnicodeCancellation453Access.Work(active);
        ReUnicodeOriginal453Access.Require(during.Active > 0 && during.CodesCreated == before.CodesCreated
            && during.MatchDataCreated == before.MatchDataCreated && !outcome.Task.IsCompleted);
        if (close)
            await runtime.DisposeAsync().AsTask().WaitAsync(ReUnicodeOriginal453Names.Deadline);
        else
            await controller.Invoke(context =>
            {
                runtime.Exit(context, worker.Pid, Term.A(ReUnicodeCancellation453Names.Kill));

                return true;
            });
        Term exit = await worker.Completion.WaitAsync(ReUnicodeOriginal453Names.Deadline);
        if (!close)
            ReUnicodeOriginal453Access.Require(exit.Equals(Term.A(ProcessExitReasons.Killed)));
        await unwound.Task.WaitAsync(ReUnicodeOriginal453Names.Deadline);
        await LibrarySelectionCancellationPhaseAccess.Unwound(active).WaitAsync(ReUnicodeOriginal453Names.Deadline);
        bool canceled = false;
        try
        {
            await outcome.Task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }
        ReUnicodeOriginal453Access.Require(canceled && LibrarySelectionCancellationPhaseAccess.ActiveLeases(active) == 0
            && engine.Report.Active == 0);
        if (!close && run)
            await controller.InvokeAsync(async context =>
            {
                Term usable = await engine.RunAsync(
                    context,
                    new BitString(Encoding.ASCII.GetBytes(ReUnicodeOriginal453Names.BinarySubject)),
                    pattern!,
                    Nil.Value
                );
                ReUnicodeOriginal453Access.Require(usable.Equals(RegexNative411Access.Match(0, 3)));

                return true;
            });
        await controller.DisposeAsync();
        await runtime.DisposeAsync().AsTask().WaitAsync(ReUnicodeOriginal453Names.Deadline);
        RegexNative411Access.Empty(engine);

        return new
        {
            Run = run,
            OwnerClose = close,
            ReductionsAtPhase = observedWork,
            OriginalReAndUnicodeLeases = true,
            Before = before,
            During = during,
            After = engine.Report,
            Canceled = canceled,
            Exit = exit.ToString(),
            ActiveCodeLeasesAfter = LibrarySelectionCancellationPhaseAccess.ActiveLeases(active)
        };
    }
}
