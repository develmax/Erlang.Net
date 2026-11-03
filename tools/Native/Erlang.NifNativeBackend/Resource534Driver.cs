using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Text.Json;

namespace Erlang.NifNativeBackend;

// Diagnostic-only observer: exact Resource529 runtime/fixtures/comparison unchanged.
internal static class Resource534Driver
{
    [ThreadStatic] private static bool recording;

    internal static async Task Run(string[] arguments)
    {
        if (arguments.Length != 5 || arguments[2] != Resource529Names.Ordinary || arguments[3] != Resource529Names.Private)
            throw new ArgumentException(Resource534Names.Incomplete);
        using var requested = JsonDocument.Parse(File.ReadAllBytes(arguments[0]));
        if (requested.RootElement.GetProperty("Job").GetString() != Resource529Names.Family)
            throw new ArgumentException(Resource534Names.Incomplete);
        // Resource529Driver still verifies the exact admission SHA and all inputs.
        string output = Path.GetFullPath(arguments[4]);
        var state = new State();
        Exception? failure = null;
        AppDomain.CurrentDomain.FirstChanceException += state.ExceptionObserved;
        try
        {
            await Resource529Driver.Run(arguments);
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= state.ExceptionObserved;
            try
            {
                Directory.CreateDirectory(output);
                await File.WriteAllTextAsync(Path.Combine(output, Resource534Names.Exceptions), JsonSerializer.Serialize(state.Exceptions.ToArray(), Options));
                await File.WriteAllTextAsync(
                    Path.Combine(output, Resource534Names.Receipt),
                    JsonSerializer.Serialize(
                        new
                        {
                            OriginalDriverUsed = true,
                            OriginalFixtureChanged = false,
                            ComparisonChanged = false,
                            RuntimeFactoryChanged = false,
                            TimeoutChanged = false,
                            state.ExceptionCount,
                            state.DroppedExceptions,
                            state.ObserverFailures,
                            CompleteWithinBound = state.DroppedExceptions == 0 && state.ObserverFailures == 0,
                            DriverSucceeded = failure is null,
                            DriverError = failure?.ToString(),
                            NormalizationPerformed = false,
                            FullCompatibilityClaimed = false
                        },
                        Options
                    )
                );
            }
            catch (Exception diagnostic)
            {
                failure = failure is null ? diagnostic : new AggregateException(failure, diagnostic);
            }
        }
        if (state.DroppedExceptions != 0 || state.ObserverFailures != 0)
            failure = failure is null ? new InvalidOperationException(Resource534Names.Incomplete) : new AggregateException(failure, new InvalidOperationException(Resource534Names.Incomplete));
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private sealed class State
    {
        internal readonly ConcurrentQueue<object> Exceptions = new();
        internal long ExceptionCount, DroppedExceptions, ObserverFailures;

        internal void ExceptionObserved(object? sender, FirstChanceExceptionEventArgs arguments)
        {
            if (recording)
            {
                Interlocked.Increment(ref ObserverFailures);

                return;
            }
            recording = true;
            try
            {
                long index = Interlocked.Increment(ref ExceptionCount);
                if (index > Resource534Names.MaximumEvents)
                {
                    Interlocked.Increment(ref DroppedExceptions);

                    return;
                }
                // Never inspect/serialize guest Terms inside an arbitrary throw,
                // which may occur under heap/runtime gates. Exception.Message
                // was captured at construction; CLR ToString reads no guest cells.
                Exceptions.Enqueue(new
                {
                    Index = index,
                    ThreadId = Environment.CurrentManagedThreadId,
                    Type = arguments.Exception.GetType().FullName,
                    arguments.Exception.Message,
                    ClrException = arguments.Exception.ToString()
                });
            }
            catch
            {
                Interlocked.Increment(ref ObserverFailures);
            }
            finally
            {
                recording = false;
            }
        }
    }
}
