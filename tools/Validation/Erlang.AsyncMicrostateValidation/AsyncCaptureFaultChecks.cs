using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
namespace Erlang.Validation.AsyncMicrostates;

internal static class AsyncCaptureFaultChecks
{
    private static bool TransportFailure(Exception e) => e is IOException or InvalidDataException or ObjectDisposedException || e is AggregateException aggregate
        && aggregate.InnerExceptions.Count != 0
        && aggregate.InnerExceptions.All(TransportFailure);

    internal static async Task<object> Run(JsonElement input, string mode)
    {
        string manifest = input.GetProperty(AsyncCaptureFaultNames.PackageManifestProperty).GetString()!;
        string package = Path.GetDirectoryName(manifest)!;
        string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(manifest)));
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;

        void Require(bool value)
        {
            if (!value)
            {
                throw new InvalidOperationException(AsyncCaptureFaultNames.ContractFailureMessage);
            }
        }
        ProcessRuntime? runtime = null;
        RuntimeAsyncAccountingCapture1170? capture = null;
        ErlDriverHostChild420? child = null;
        RuntimeDriverFrontend422? frontend = null;
        Exception? primary = null, operationFailure = null, releaseFailure = null, vmFailure = null;
        bool rejected = false, physicalExit = false, physicalCleanup = false, leaseReleased = false;
        try
        {
            runtime = await ProcessRuntime.CreateWithOptionsAsync(
                new RuntimeLibrarySelection(Array.Empty<RuntimeModuleSelection>()),
                new RuntimeLibraryStartupOptions(
                    RuntimeNativePrimitives.None,
                    false,
                    null,
                    new RuntimeDriverFrontendOptions(manifest, hash, asyncThreads: AsyncCaptureFaultNames.AsyncWorkers)
                ),
                default
            );
            ProcessRuntime actual = runtime;
            var admitted = new TaskCompletionSource<RuntimeAsyncAccountingCapture1170>(TaskCreationOptions.RunContinuationsAsynchronously);
            var actor = actual.Spawn(async context =>
            {
                try
                {
                    RuntimeAsyncAccountingCapture1170 owned = await actual.CaptureAsyncAccounting1170Async(context, AsyncCaptureFaultNames.InitialCaptureEpoch) ?? throw new InvalidOperationException(AsyncCaptureFaultNames.MissingCaptureMessage);
                    admitted.TrySetResult(owned);
                }
                catch (Exception failure)
                {
                    admitted.TrySetException(failure);
                }

                return Nil.Value;
            });
            try
            {
                capture = await admitted.Task.WaitAsync(AsyncCaptureFaultNames.Deadline);
            }
            finally
            {
                await actor.Completion.WaitAsync(AsyncCaptureFaultNames.Deadline);
            }
            Require(capture.Count == AsyncCaptureFaultNames.AsyncWorkers && capture.IsOpen1170);
            frontend = (RuntimeDriverFrontend422)capture.FrontendIdentity1170;
            child = (ErlDriverHostChild420)typeof(RuntimeDriverFrontend422).GetField(AsyncCaptureFaultNames.FrontendChildField, fields)!.GetValue(frontend)!;
            var process = (Process)typeof(ErlDriverHostChild420).GetField(AsyncCaptureFaultNames.ChildProcessField, fields)!.GetValue(child)!;
            object runtimeGate = typeof(ProcessRuntime).GetField(AsyncCaptureFaultNames.RuntimeGateField, fields)!.GetValue(actual)!;

            int Leases()
            {
                lock (runtimeGate)
                {
                    return (int)typeof(RuntimeDriverFrontend422).GetField(AsyncCaptureFaultNames.FrontendLeaseCountField, fields)!.GetValue(frontend)!;
                }
            }
            Require(Leases() == AsyncCaptureFaultNames.SingleFrontendLease && !child.PhysicalExit.IsCompleted && !process.HasExited);
            Require(process.Id != Environment.ProcessId
                && string.Equals(
                    process.MainModule!.FileName,
                    Path.Combine(package, AsyncCaptureFaultNames.ChildExecutableName),
                    StringComparison.OrdinalIgnoreCase
                ));
            if (mode == AsyncCaptureFaultNames.ChildExitMode)
            {
                process.Kill(entireProcessTree: false);
                await child.PhysicalExit.WaitAsync(AsyncCaptureFaultNames.Deadline);
                try
                {
                    _ = await capture.ApplyAsync(RuntimeMicrostateAction.Gather).WaitAsync(AsyncCaptureFaultNames.Deadline);
                }
                catch (Exception failure) when (TransportFailure(failure))
                {
                    operationFailure = failure;
                    rejected = true;
                }
            }
            else
            {
                var metadata = (ErlDriverAsyncAccountingCaptureReply1151)typeof(RuntimeAsyncAccountingCapture1170).GetField(AsyncCaptureFaultNames.CaptureMetadataField, fields)!.GetValue(capture)!;
                byte[] controlPayload = ErlDriverPrimitiveWire420.Encode(new ErlDriverAsyncAccountingControlRequest1151(
                    metadata.Version,
                    metadata.Profile,
                    checked(metadata.Epoch + AsyncCaptureFaultNames.WrongEpochIncrement),
                    metadata.Generation,
                    metadata.Token,
                    RuntimeAsyncAccountingCodec1170.Action(RuntimeMicrostateAction.Gather)
                ));
                try
                {
                    _ = await frontend.RequestAsyncAccounting1170(ErlDriverHostOperation420.AsyncAccountingApply1151, controlPayload).WaitAsync(AsyncCaptureFaultNames.Deadline);
                }
                catch (Exception failure) when (TransportFailure(failure))
                {
                    operationFailure = failure;
                    rejected = true;
                }
            }
            Require(rejected && operationFailure is not null);
            try
            {
                await capture.DisposeAsync().AsTask().WaitAsync(AsyncCaptureFaultNames.Deadline);
            }
            catch (Exception failure)
            {
                releaseFailure = failure;
            }
            await child.PhysicalExit.WaitAsync(AsyncCaptureFaultNames.Deadline);
            physicalExit = child.PhysicalExit.IsCompletedSuccessfully;
            leaseReleased = Leases() == 0;
            Require(physicalExit && leaseReleased);
        }
        catch (Exception failure)
        {
            primary = failure;
        }
        finally
        {
            if (capture is not null)
                try
                {
                    await capture.DisposeAsync().AsTask().WaitAsync(AsyncCaptureFaultNames.Deadline);
                }
                catch (Exception failure)
                {
                    releaseFailure = releaseFailure is null ? failure : new AggregateException(releaseFailure, failure);
                }
            if (runtime is not null)
                try
                {
                    await runtime.DisposeAsync().AsTask().WaitAsync(AsyncCaptureFaultNames.Deadline);
                }
                catch (Exception failure)
                {
                    vmFailure = failure;
                }
            if (child is not null)
            {
                try
                {
                    await child.PhysicalCleanup.WaitAsync(AsyncCaptureFaultNames.Deadline);
                    physicalCleanup = child.PhysicalCleanup.IsCompletedSuccessfully;
                }
                catch (Exception failure)
                {
                    primary = primary is null ? failure : new AggregateException(primary, failure);
                }
            }
        }
        // Faulted VM/Release results are retained as actual diagnostic outcomes;
        // they do not replace the successful-only physical child/lease fences.
        var report = new { Mode = mode, Rejected = rejected, PhysicalExit = physicalExit, PhysicalCleanup = physicalCleanup, LeaseReleased = leaseReleased, OperationFailure = operationFailure?.ToString(), ReleaseFailure = releaseFailure?.ToString(), VmFailure = vmFailure?.ToString(), Primary = primary?.ToString(), Scope = AsyncCaptureFaultNames.Scope };
        Console.WriteLine(JsonSerializer.Serialize(report));
        Require(primary is null
            && rejected
            && physicalExit
            && physicalCleanup
            && leaseReleased
            && (releaseFailure is null || TransportFailure(releaseFailure))
            && (vmFailure is null || TransportFailure(vmFailure)));

        return report;
    }
}
