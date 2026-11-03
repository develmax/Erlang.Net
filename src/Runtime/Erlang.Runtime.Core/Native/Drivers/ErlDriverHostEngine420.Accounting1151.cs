using System.Text.Json;

namespace Erlang;

internal sealed partial class ErlDriverHostEngine420
{
    private readonly object asyncAccountingGate1151 = new();
    private readonly Dictionary<ulong, ErlDriverAsyncAccountingCapture1151> asyncAccountingCaptures1151 = new();
    private readonly TaskCompletionSource asyncAccountingDrained1151 = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly string asyncAccountingGeneration1151 = Guid.NewGuid().ToString("N");
    private ulong nextAsyncAccountingToken1151;
    private int asyncAccountingOperations1151, asyncAccountingReleases1151;
    private bool asyncAccountingClosing1151;
    private Exception? asyncAccountingReleaseFailure1151;

    private static bool IsAsyncAccounting1151(ErlDriverHostOperation420 operation)
        => operation is ErlDriverHostOperation420.AsyncAccountingCapture1151
            or ErlDriverHostOperation420.AsyncAccountingApply1151
            or ErlDriverHostOperation420.AsyncAccountingFinish1151
            or ErlDriverHostOperation420.AsyncAccountingRelease1151;

    private async ValueTask<ErlDriverHostFrame420> HandleAsyncAccounting1151(ErlDriverHostFrame420 frame)
    {
        if (fault.Failure is { } initialFailure) throw new IOException(ErlDriverHostNames420.Closed, initialFailure);
        if (frame.Version != ErlDriverHostNames420.Version || frame.Kind != ErlDriverHostKind420.Request
            || frame.Session != session || frame.Request == 0 || frame.Driver != 0 || frame.Port != 0
            || frame.Actor != 0 || frame.Command != 0 || frame.NativeResult != 0 || frame.Name is not null
            || frame.SchedulerThread || frame.EventSequence != 0 || frame.Data is null)
            throw InvalidAsyncAccounting1151();
        byte[] response;
        switch (frame.Operation)
        {
            case ErlDriverHostOperation420.AsyncAccountingCapture1151:
                response = CaptureAsyncAccounting1151(DecodeAsyncAccounting1151<ErlDriverAsyncAccountingCaptureRequest1151>(frame.Data));
                break;
            case ErlDriverHostOperation420.AsyncAccountingApply1151:
                response = ApplyAsyncAccounting1151(DecodeAsyncAccounting1151<ErlDriverAsyncAccountingControlRequest1151>(frame.Data), false);
                break;
            case ErlDriverHostOperation420.AsyncAccountingFinish1151:
                response = ApplyAsyncAccounting1151(DecodeAsyncAccounting1151<ErlDriverAsyncAccountingControlRequest1151>(frame.Data), true);
                break;
            case ErlDriverHostOperation420.AsyncAccountingRelease1151:
                response = await ReleaseAsyncAccounting1151(DecodeAsyncAccounting1151<ErlDriverAsyncAccountingReleaseRequest1151>(frame.Data)).ConfigureAwait(false);
                break;
            default: throw InvalidAsyncAccounting1151();
        }
        if (fault.Failure is { } failure) throw new IOException(ErlDriverHostNames420.Closed, failure);

        return frame with { Kind = ErlDriverHostKind420.Response, NativeResult = 0, Name = null, Data = response };
    }

    private static InvalidDataException InvalidAsyncAccounting1151() => new(ErlDriverHostNames420.InvalidFrame);

    private static T DecodeAsyncAccounting1151<T>(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw InvalidAsyncAccounting1151();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
            if (!names.Add(property.Name)) throw InvalidAsyncAccounting1151();

        // Existing decoder rejects unknown members and exact-case mismatches.
        return ErlDriverPrimitiveWire420.Decode<T>(bytes);
    }

    private static void RequireAsyncAccounting1151(int version, string profile, ulong epoch)
    {
        if (version != ErlDriverAsyncAccountingWireNames1151.Version
            || profile != ErlDriverAsyncAccountingWireNames1151.Profile || epoch == 0)
            throw InvalidAsyncAccounting1151();
    }

    private byte[] CaptureAsyncAccounting1151(ErlDriverAsyncAccountingCaptureRequest1151 input)
    {
        RequireAsyncAccounting1151(input.Version, input.Profile, input.Epoch);
        var services = async ?? throw InvalidAsyncAccounting1151();
        if (!services.IsAccountingConfigured1151) throw InvalidAsyncAccounting1151();
        ErlDriverNativeAsyncAccounting1126? owner = services.AccountingOwner1151;
        ErlDriverNativeAsyncAccounting1126.Lease? lease = null;
        ulong token = 0;
        bool published = false;
        try
        {
            // No Gather here: capture must not flush an enabled native counter.
            lease = owner?.Acquire();
            ErlDriverAsyncAccountingCapture1151 captured;
            lock (asyncAccountingGate1151)
            {
                ObjectDisposedException.ThrowIf(asyncAccountingClosing1151, this);
                foreach (var existing in asyncAccountingCaptures1151.Values)
                    if (existing.Metadata.Epoch == input.Epoch) throw InvalidAsyncAccounting1151();
                token = checked(nextAsyncAccountingToken1151 + 1);
                var metadata = new ErlDriverAsyncAccountingCaptureReply1151(
                    ErlDriverAsyncAccountingWireNames1151.Version,
                    ErlDriverAsyncAccountingWireNames1151.Profile,
                    input.Epoch,
                    asyncAccountingGeneration1151,
                    token,
                    owner?.Count ?? 0,
                    owner?.Frequency ?? ErlDriverAsyncAccountingNames1126.PublicFrequency,
                    owner?.RawQpcFrequency ?? 0
                );
                captured = new(lease, metadata);
                asyncAccountingCaptures1151.EnsureCapacity(checked(asyncAccountingCaptures1151.Count + 1));
                asyncAccountingCaptures1151.Add(token, captured);
                nextAsyncAccountingToken1151 = token;
                published = true;
            }
            byte[] bytes = ErlDriverPrimitiveWire420.Encode(captured.Metadata);
            lease = null; // The published dictionary token owns it now.

            return bytes;
        }
        catch (Exception primary)
        {
            bool registered = false;
            lock (asyncAccountingGate1151)
            {
                if (published && asyncAccountingCaptures1151.Remove(token))
                {
                    registered = true;
                    asyncAccountingReleases1151++;
                }
            }
            Exception? cleanup = null;
            try
            {
                lease?.Dispose();
            }
            catch (Exception failure)
            {
                cleanup = failure;
            }
            finally
            {
                lock (asyncAccountingGate1151)
                {
                    if (registered) asyncAccountingReleases1151--;
                    asyncAccountingReleaseFailure1151 ??= cleanup;
                    SignalAsyncAccountingDrained1151();
                }
            }
            if (cleanup is not null) throw new AggregateException(primary, cleanup);
            throw;
        }
    }

    private ErlDriverAsyncAccountingCapture1151 FindAsyncAccounting1151(
        ulong epoch, string generation, ulong token)
    {
        if (token == 0 || generation != asyncAccountingGeneration1151
            || !asyncAccountingCaptures1151.TryGetValue(token, out var captured)
            || captured.Metadata.Epoch != epoch)
            throw InvalidAsyncAccounting1151();

        return captured;
    }

    private static ErlDriverAsyncAccountingAction1126 AsyncAccountingAction1151(string action) => action switch
    {
        ErlDriverAsyncAccountingWireNames1151.Enable => ErlDriverAsyncAccountingAction1126.Enable,
        ErlDriverAsyncAccountingWireNames1151.Disable => ErlDriverAsyncAccountingAction1126.Disable,
        ErlDriverAsyncAccountingWireNames1151.Reset => ErlDriverAsyncAccountingAction1126.Reset,
        ErlDriverAsyncAccountingWireNames1151.Gather => ErlDriverAsyncAccountingAction1126.Gather,
        _ => throw InvalidAsyncAccounting1151()
    };

    private byte[] ApplyAsyncAccounting1151(ErlDriverAsyncAccountingControlRequest1151 input, bool finishing)
    {
        RequireAsyncAccounting1151(input.Version, input.Profile, input.Epoch);
        ErlDriverAsyncAccountingAction1126 action = AsyncAccountingAction1151(input.Action);
        var settled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ErlDriverAsyncAccountingCapture1151 captured;
        lock (asyncAccountingGate1151)
        {
            captured = FindAsyncAccounting1151(input.Epoch, input.Generation, input.Token);
            if (captured.Busy || captured.Failed || (finishing
                ? !captured.Applied || captured.Finished || captured.Action != input.Action
                : captured.Applied))
                throw InvalidAsyncAccounting1151();
            int operations = checked(asyncAccountingOperations1151 + 1);
            // Bind exactly once BEFORE native effects. Failure is never reset
            // to Prepared and may only be followed by actual lease Release.
            if (finishing) captured.Finished = true;
            else
            {
                captured.Applied = true;
                captured.Action = input.Action;
            }
            captured.Busy = true;
            captured.Running = settled;
            asyncAccountingOperations1151 = operations;
        }
        try
        {
            if (finishing)
            {
                captured.Lease?.Finish(action);

                return ErlDriverPrimitiveWire420.Encode(new ErlDriverAsyncAccountingAck1151(
                    input.Version,
                    input.Profile,
                    input.Epoch,
                    input.Generation,
                    input.Token,
                    ErlDriverAsyncAccountingWireNames1151.Finish
                ));
            }
            ErlDriverAsyncAccountingSnapshot1126[] observations = captured.Lease?.Apply(action) ?? [];
            var rows = new ErlDriverAsyncAccountingRow1151[observations.Length];
            for (int index = 0; index < rows.Length; index++)
            {
                var row = observations[index];
                rows[index] = new(
                    row.Identity,
                    row.OperatingSystemThread,
                    (uint)row.State,
                    row.Frequency,
                    row.PerformanceCounter,
                    row.Counters
                );
            }
            if (rows.Length != (action == ErlDriverAsyncAccountingAction1126.Gather ? captured.Metadata.Count : 0))
                throw InvalidAsyncAccounting1151();

            return ErlDriverPrimitiveWire420.Encode(new ErlDriverAsyncAccountingApplyReply1151(
                input.Version,
                input.Profile,
                input.Epoch,
                input.Generation,
                input.Token,
                input.Action,
                rows
            ));
        }
        catch
        {
            lock (asyncAccountingGate1151) captured.Failed = true;
            throw;
        }
        finally
        {
            lock (asyncAccountingGate1151)
            {
                captured.Busy = false;
                captured.Running = null;
                asyncAccountingOperations1151--;
                SignalAsyncAccountingDrained1151();
            }
            settled.TrySetResult();
        }
    }

    private async Task<byte[]> ReleaseAsyncAccounting1151(ErlDriverAsyncAccountingReleaseRequest1151 input)
    {
        RequireAsyncAccounting1151(input.Version, input.Profile, input.Epoch);
        ErlDriverAsyncAccountingCapture1151 captured;
        Task running;
        lock (asyncAccountingGate1151)
        {
            captured = FindAsyncAccounting1151(input.Epoch, input.Generation, input.Token);
            running = captured.Running?.Task ?? Task.CompletedTask;
            asyncAccountingReleases1151 = checked(asyncAccountingReleases1151 + 1);
            asyncAccountingCaptures1151.Remove(input.Token);
        }
        Exception? failure = null;
        try
        {
            await running.ConfigureAwait(false);
            captured.Lease?.Dispose();
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            lock (asyncAccountingGate1151)
            {
                asyncAccountingReleaseFailure1151 ??= failure;
                asyncAccountingReleases1151--;
                SignalAsyncAccountingDrained1151();
            }
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();

        return ErlDriverPrimitiveWire420.Encode(new ErlDriverAsyncAccountingAck1151(
            input.Version,
            input.Profile,
            input.Epoch,
            input.Generation,
            input.Token,
            ErlDriverAsyncAccountingWireNames1151.Release
        ));
    }

    private void SealAsyncAccounting1151()
    {
        lock (asyncAccountingGate1151)
        {
            asyncAccountingClosing1151 = true;
            SignalAsyncAccountingDrained1151();
        }
    }

    private void SignalAsyncAccountingDrained1151()
    {
        if (!asyncAccountingClosing1151 || asyncAccountingCaptures1151.Count != 0
            || asyncAccountingOperations1151 != 0 || asyncAccountingReleases1151 != 0) return;
        if (asyncAccountingReleaseFailure1151 is { } failure) asyncAccountingDrained1151.TrySetException(failure);
        else asyncAccountingDrained1151.TrySetResult();
    }
}
