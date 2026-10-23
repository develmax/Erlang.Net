namespace Erlang;

public sealed partial class RuntimeRegexEngine411 : IAsyncDisposable
{
    private readonly ProcessRuntime runtime;
    private readonly ReEngineApi411 api;
    private readonly object gate = new();
    private readonly HashSet<ReCode411> codes = new();
    private readonly Dictionary<ProcessContext, ReInvocation411> invocations = new();
    private readonly TaskCompletionSource drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int active;
    private bool closed;
    private int disposalStarted;
    private long createdCodes, freedCodes, createdData, freedData, matchYields;
    private bool libraryFreed;

    internal RuntimeRegexEngine411(ProcessRuntime runtime, ReEngineApi411 api)
    {
        this.runtime = runtime;
        this.api = api;
    }

    internal static RuntimeRegexEngine411 Open(ProcessRuntime runtime, string officialAdmission)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        runtime.RequireNativePrimitiveRegistration();
        string image = ReAdmission411.Validate(officialAdmission);
        var api = new ReEngineApi411(image);
        try
        {
            ReAdmission411.Validate(officialAdmission);

            return new(runtime, api);
        }
        catch
        {
            api.Dispose();
            throw;
        }
    }

    public RuntimeRegexReport411 Report
    {
        get
        {
            lock (gate) return new(
                createdCodes,
                freedCodes,
                createdData,
                freedData,
                matchYields,
                active,
                codes.Count,
                closed,
                libraryFreed
            );
        }
    }

    private void Enter(ProcessContext context)
    {
        if (!ReferenceEquals(context.Runtime, runtime)) throw new ArgumentException(ReNames411.Foreign);
        runtime.AdmitRegex411(
            context,
            () =>
        {
            lock (gate)
            {
                if (closed) throw new ReOwnerClosing411(runtime);
                active++;
                if (!invocations.TryGetValue(context, out var invocation)) invocations.Add(context, invocation = new());
                invocation.Count++;
            }
        }
        );
    }

    private void Exit(ProcessContext? context = null)
    {
        lock (gate)
        {
            if (context is not null)
            {
                var invocation = invocations[context];
                if (--invocation.Count == 0)
                {
                    invocations.Remove(context);
                    invocation.Completion.TrySetResult();
                }
            }
            if (--active < 0) throw new InvalidOperationException(ReDiagnosticNames411.RegexActiveLeaseUnderflow);
            if (closed && active == 0) drained.TrySetResult();
        }
    }

    internal Task DrainProcess(ProcessContext context)
    {
        lock (gate) return invocations.TryGetValue(context, out var invocation) ? invocation.Completion.Task : Task.CompletedTask;
    }

    internal bool RetirementCompletedSuccessfully => disposed.Task.IsCompletedSuccessfully;

    private void Check(ProcessContext context)
    {
        context.Cancellation.ThrowIfCancellationRequested();
        lock (gate) if (closed) throw new ReOwnerClosing411(runtime);
    }

    internal void ReleaseGuest(ReCode411 code)
    {
        bool free = false;
        lock (gate)
        {
            if (!codes.Contains(code)) return;
            code.GuestReleased = true;
            if (code.Readers == 0 && !closed)
            {
                codes.Remove(code);
                active++;
                free = true;
            }
        }
        if (free)
        {
            try
            {
                Free(code);
            }
            finally
            {
                Exit();
            }
        }
    }

    private void Read(ReCode411 code)
    {
        lock (gate)
        {
            if (closed || !codes.Contains(code) || code.GuestReleased) throw new ErlangException(ErlangErrorReasons.BadArgument);
            code.Readers++;
        }
    }

    private void EndRead(ReCode411 code)
    {
        bool free = false;
        lock (gate)
        {
            if (--code.Readers < 0) throw new InvalidOperationException(ReDiagnosticNames411.RegexReadLeaseUnderflow);
            if (code.Readers == 0 && code.GuestReleased && !closed && codes.Remove(code)) free = true;
        }
        if (free) Free(code); // Invocation still owns its active engine lease.
    }

    private void Free(ReCode411 code)
    {
        var pointer = Interlocked.Exchange(ref code.Pointer, IntPtr.Zero);
        if (pointer != IntPtr.Zero)
        {
            api.FreeCode(pointer);
            Interlocked.Increment(ref freedCodes);
        }
    }

    public void RequestClose()
    {
        lock (gate)
        {
            closed = true;
            if (active == 0) drained.TrySetResult();
        }
    }

    public async ValueTask DisposeAsync()
    {
        RequestClose();
        if (Interlocked.Exchange(ref disposalStarted, 1) == 0)
        {
            try
            {
                await Task.Yield(); // Never free code or unload synchronously under the runtime close gate.
                await drained.Task.ConfigureAwait(false);
                ReCode411[] owned;
                lock (gate)
                {
                    owned = codes.ToArray();
                    codes.Clear();
                }
                var failures = new List<Exception>();
                foreach (var code in owned) try
                    {
                        Free(code);
                    }
                    catch (Exception error)
                    {
                        failures.Add(error);
                    }
                try
                {
                    api.Dispose();
                    libraryFreed = true;
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
                if (failures.Count != 0) throw new AggregateException(failures);
                disposed.TrySetResult();
            }
            catch (Exception error)
            {
                disposed.TrySetException(error);
            }
        }
        await disposed.Task.ConfigureAwait(false);
    }
}
