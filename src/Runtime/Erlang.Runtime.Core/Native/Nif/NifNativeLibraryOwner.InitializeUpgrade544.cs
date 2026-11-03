using System.Runtime.ExceptionServices;

namespace Erlang;

internal sealed unsafe partial class NifNativeLibraryOwner
{
    internal IReadOnlyList<KeyValuePair<(string Name, int Arity), ErlangFunction>> InitializeUpgrade616(
        Term loadInfo,
        CancellationToken cancellation,
        NifNativeLifecyclePermit616 permit
    )
    {
        using var lifecycle = permit.Enter616();
        lock (gate)
        {
            if (disposed || phase616 != NifNativeInitializationPhase616.Prepared
                || UpgradeOperation544 is not { } operation || entry.Upgrade == 0)
                throw new InvalidOperationException(NifNativeTransition544Names.Admission);
            cancellation.ThrowIfCancellationRequested();
            // Reserve cleanup bookkeeping before acquiring callback custody.
            var errors = new Exception?[NifNativePhaseNames616.CleanupFailureSlots];
            NifResourceImagePin524 callback = operation.Previous544.RetainCallback544(this, out var previous);
            nint original = previous.privateData;
            bool returned = false;
            int result = 0;
            try
            {
                try
                {
                    var failure = new NifNativeFailureState();
                    using var scope = new NifNativeCallbackScope(allocations, failure, reallocationEnabled);
                    using var environment = new NifNativeCallEnvironment(
                        initializationContext,
                        image,
                        0,
                        failure
                    );
                    environment.AttachGuestRoots516(initializationContext);
                    using var invocation = scope.BindEnvironment(environment);
                    nuint information = environment.Add(loadInfo);
                    phase616 = NifNativeInitializationPhase616.Invoking;
                    result = InvokeUpgradeCallback544(previous, environment, information);
                    returned = true;
                    if (result == 0)
                    {
                        // This precedes all throwable environment unwinding.
                        initialized = true;
                        phase616 = NifNativeInitializationPhase616.NativeSucceeded;
                        operation.NativeSucceeded544 = true;
                    }
                    failure.ThrowPending();
                }
                catch (Exception error)
                {
                    errors[NifNativePhaseNames616.NativeCallbackFailure] = error;
                }
                finally
                {
                    // All native environment/post-call scopes have ended before
                    // normal progress resumes, matching the pinned OTP cutpoint.
                    try
                    {
                        operation.ReleaseNormalBarrier544();
                    }
                    catch (Exception error)
                    {
                        errors[NifNativePhaseNames616.BarrierReleaseFailure] = error;
                    }
                }
                if (!operation.NativeSucceeded544)
                {
                    try
                    {
                        // Keep callback custody until restoration AFTER unblock.
                        previous.privateData = original;
                        operation.EndVeto544();
                    }
                    catch (Exception error)
                    {
                        errors[NifNativePhaseNames616.VetoRestoreFailure] = error;
                    }
                }
            }
            finally
            {
                try
                {
                    callback.Dispose();
                }
                catch (Exception error)
                {
                    errors[NifNativePhaseNames616.CallbackPinReleaseFailure] = error;
                }
            }
            Exception[] failures = errors.Where(error => error is not null).Cast<Exception>().ToArray();
            if (failures.Length != 0)
            {
                Exception failure = failures.Length == 1 ? failures[0] : new AggregateException(failures);
                if (operation.NativeSucceeded544)
                    operation.Result544.NativePostCommitFailure544 = failure;
                else
                    ExceptionDispatchInfo.Capture(failure).Throw();
            }
            if (!returned || result != 0)
                throw new NifNativeUpgradeVetoException544(result);

            return operation.Result544.Bindings;
        }
    }
}
