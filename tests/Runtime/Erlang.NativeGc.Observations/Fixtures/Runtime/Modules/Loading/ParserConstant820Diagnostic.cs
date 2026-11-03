using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Text.Json;

namespace Erlang.Tests;

internal sealed class ParserConstant820Diagnostic : IAsyncDisposable
{
    private const string OutputEnvironment = "ERLANG_SOURCE820_DIAGNOSTIC";
    private const int MaximumExceptions = 128;
    private const int SampleMilliseconds = 25;
    [ThreadStatic] private static bool observing;
    private readonly ProcessRuntime runtime;
    private readonly string output;
    private readonly CancellationTokenSource stop = new();
    private readonly ConcurrentQueue<string> exceptions = new();
    private readonly List<string> completions = new();
    private readonly Dictionary<Pid, ProcessHandle> known = new();
    private readonly HashSet<Pid> finished = new();
    private readonly EventHandler<FirstChanceExceptionEventArgs> handler;
    private readonly Task sample;
    private int exceptionCount;

    internal static ParserConstant820Diagnostic? Begin(ProcessRuntime runtime)
    {
        string? output = Environment.GetEnvironmentVariable(OutputEnvironment);

        return string.IsNullOrEmpty(output) ? null : new(runtime, output);
    }

    private ParserConstant820Diagnostic(ProcessRuntime runtime, string output)
    {
        this.runtime = runtime;
        this.output = output;
        handler = Observe;
        AppDomain.CurrentDomain.FirstChanceException += handler;
        sample = Sample();
    }

    private void Observe(object? sender, FirstChanceExceptionEventArgs item)
    {
        if (observing || item.Exception is OperationCanceledException) return;
        Exception error = item.Exception;
        // Exclude internal trampoline/control exceptions; retain guest failures
        // and ordinary CLR errors. Do not traverse native Term fields under R/H.
        if (error is not ErlangException && error.GetType().Namespace?.StartsWith("System", StringComparison.Ordinal) != true) return;
        if (Interlocked.Increment(ref exceptionCount) > MaximumExceptions) return;
        observing = true;
        try
        {
            exceptions.Enqueue(error.GetType().FullName + ": " + error.Message + Environment.NewLine + error.StackTrace);
        }
        finally
        {
            observing = false;
        }
    }

    private async Task Sample()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(SampleMilliseconds));
        try
        {
            do
            {
                foreach (Pid pid in runtime.ProcessSnapshot())
                    if (!known.ContainsKey(pid))
                        try
                        {
                            known.Add(pid, runtime.GetProcessHandle(pid));
                        }
                        catch (ErlangException)
                        {
                            completions.Add("Disappeared before handle acquisition: " + pid);
                        }
                ReadCompleted();
            }
            while (await timer.WaitForNextTickAsync(stop.Token));
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
        }
    }

    private void ReadCompleted()
    {
        foreach (var entry in known)
            if (entry.Value.Completion.IsCompleted && finished.Add(entry.Key))
                try
                {
                    Task<Term> task = entry.Value.Completion;
                    completions.Add(entry.Key + ": " + (task.IsCompletedSuccessfully ? task.Result.ToString() : task.Exception?.ToString() ?? task.Status.ToString()));
                }
                catch (Exception failure)
                {
                    completions.Add(entry.Key + ": completion reason read failed: " + failure.GetType().FullName + ": " + failure.Message);
                }
    }

    public async ValueTask DisposeAsync()
    {
        stop.Cancel();
        try
        {
            await sample;
            ReadCompleted();
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= handler;
            stop.Dispose();
        }
        await File.WriteAllTextAsync(
            output,
            JsonSerializer.Serialize(
                new
                {
                    DiagnosticOnly = true,
                    GuestWorkersUnchanged = true,
                    NoDeadlineIncrease = true,
                    FirstChance = exceptions.ToArray(),
                    FirstChanceSeen = exceptionCount,
                    FirstChanceLimit = MaximumExceptions,
                    PublicProcessCompletions = completions.ToArray(),
                    ObservedProcessCount = known.Count,
                    IncompleteFastProcessSampling = true,
                    ProcessCompletionIsNotMailboxDown = true
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
    }
}
