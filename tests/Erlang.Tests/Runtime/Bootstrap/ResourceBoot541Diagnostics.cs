using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using static Erlang.Tests.ResourceBoot541Names;

namespace Erlang.Tests;

// Same passive FirstChance approach as StandardBootOriginal412Checks/Resource534.
// No guest Terms are inspected by arbitrary exception or timer callbacks.
internal sealed class ResourceBoot541Diagnostics : IAsyncDisposable
{
    [ThreadStatic] private static bool recording;
    private readonly string directory;
    private readonly ConcurrentQueue<object> exceptions = new();
    private readonly ConcurrentQueue<object> progress = new();
    private readonly Stopwatch elapsed = Stopwatch.StartNew();
    private readonly Timer timer;
    private ProcessRuntime? runtime;
    private string stage = RuntimeCreated;
    private long exceptionCount, dropped, failures, samples;
    private int sampling, actorSamples543;

    internal ResourceBoot541Diagnostics(string directory)
    {
        this.directory = directory;
        AppDomain.CurrentDomain.FirstChanceException += Observe;
        timer = new(
            _ => Sample(true),
            null,
            SamplePeriod,
            SamplePeriod
        );
    }

    internal void Attach(ProcessRuntime value)
    {
        Volatile.Write(ref runtime, value);
        Mark(RuntimeCreated);
    }

    internal void Mark(string value)
    {
        Volatile.Write(ref stage, value);
        Sample();
    }

    private void Sample(bool timerTick = false)
    {
        if (Interlocked.Exchange(ref sampling, 1) != 0) return;
        try
        {
            long index = Interlocked.Increment(ref samples);
            if (index > MaximumSamples) return;
            ProcessRuntime? current = Volatile.Read(ref runtime);
            // RegisteredSnapshot copies names under its own gate and returns;
            // no metadata/IO is performed while that runtime gate is held.
            string[] names = current?.RegisteredSnapshot().OrderBy(name => name, StringComparer.Ordinal).ToArray() ?? [];
            string selectedStage = Volatile.Read(ref stage);
            object? actors543 = null;
            if (timerTick && current is not null && selectedStage == Boot && actorSamples543 < ResourceBoot543Names.ActorSamples)
            {
                actorSamples543++;
                actors543 = ResourceBoot543Access.Capture(current);
            }
            progress.Enqueue(new { Index = index, ElapsedMilliseconds = elapsed.ElapsedMilliseconds, Stage = selectedStage, RegisteredNames = names, ActorSampleIndex = actors543 is null ? (int?)null : actorSamples543, LiveActors = actors543 });
            File.WriteAllText(Path.Combine(directory, Progress), JsonSerializer.Serialize(progress.ToArray()));
        }
        catch
        {
            Interlocked.Increment(ref failures);
        }
        finally
        {
            Volatile.Write(ref sampling, 0);
        }
    }

    private void Observe(object? sender, FirstChanceExceptionEventArgs observation)
    {
        if (recording)
        {
            Interlocked.Increment(ref failures);

            return;
        }
        recording = true;
        try
        {
            long index = Interlocked.Increment(ref exceptionCount);
            if (index > MaximumExceptions)
            {
                Interlocked.Increment(ref dropped);

                return;
            }
            exceptions.Enqueue(new
            {
                Index = index,
                ElapsedMilliseconds = elapsed.ElapsedMilliseconds,
                ThreadId = Environment.CurrentManagedThreadId,
                Stage = Volatile.Read(ref stage),
                Type = observation.Exception.GetType().FullName,
                observation.Exception.Message,
                ClrException = observation.Exception.ToString()
            });
        }
        catch
        {
            Interlocked.Increment(ref failures);
        }
        finally
        {
            recording = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await timer.DisposeAsync();
        AppDomain.CurrentDomain.FirstChanceException -= Observe;
        await File.WriteAllTextAsync(Path.Combine(directory, FirstChance), JsonSerializer.Serialize(exceptions.ToArray()));
        await File.WriteAllTextAsync(
            Path.Combine(directory, Diagnostic),
            JsonSerializer.Serialize(new
            {
                ExceptionCount = exceptionCount,
                DroppedExceptions = dropped,
                ObserverFailures = failures,
                ActorSamplesAttempted = actorSamples543,
                CompleteWithinBound = dropped == 0 && failures == 0,
                RuntimeOrFixtureChanged = false,
                TimeoutChanged = false,
                RawComparisonChanged = false,
                FullCompatibilityClaimed = false
            })
        );
    }
}
