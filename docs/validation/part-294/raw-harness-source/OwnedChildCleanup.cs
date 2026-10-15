using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text.Json;

namespace PressureRawControls;

internal static class OwnedChildCleanup
{
    internal static async Task<object> Run(string identityPath, string nonce, Task? capture, string reportPath)
    {
        int? pid = null;
        long? creation = null;
        long? actualCreation = null;
        string? executable = null;
        string? actualExecutable = null;
        bool verified = false;
        bool wasAlive = false;
        bool exited = false;
        Exception? failure = null;
        Process? child = null;
        Task captureJoin = capture ?? Task.CompletedTask;
        var clock = Stopwatch.StartNew();
        try
        {
            using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(identityPath));
            var identity = document.RootElement;
            pid = identity.GetProperty(RawCaptureNames.Pid).GetInt32();
            creation = identity.GetProperty(RawCaptureNames.CreationFileTimeUtc).GetInt64();
            executable = Path.GetFullPath(identity.GetProperty(RawCaptureNames.Executable).GetString()!);
            if (identity.GetProperty(RawCaptureNames.Nonce).GetString() != nonce)
                throw new InvalidOperationException(RawCaptureNames.ChildMismatch);
            child = Process.GetProcessById(pid.Value);
            actualCreation = child.StartTime.ToUniversalTime().ToFileTimeUtc();
            actualExecutable = Path.GetFullPath(child.MainModule!.FileName!);
            verified = creation == actualCreation && StringComparer.OrdinalIgnoreCase.Equals(executable, actualExecutable);
            if (!verified)
                throw new InvalidOperationException(RawCaptureNames.ChildMismatch);
            wasAlive = !child.HasExited;
            if (wasAlive)
                child.Kill(entireProcessTree: false);
            await Task.WhenAll(child.WaitForExitAsync(), captureJoin)
                .WaitAsync(TimeSpan.FromSeconds(RawCaptureNames.CleanupDeadlineSeconds));
            exited = child.HasExited;
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        finally
        {
            clock.Stop();
            child?.Dispose();
        }
        var report = new
        {
            Status = failure is null ? RawCaptureNames.Pass : RawCaptureNames.Fail,
            Pid = pid,
            ExpectedCreationFileTimeUtc = creation,
            ActualCreationFileTimeUtc = actualCreation,
            ExpectedExecutable = executable,
            ActualExecutable = actualExecutable,
            Nonce = nonce,
            IdentityVerified = verified,
            WasAlive = wasAlive,
            KillEntireProcessTree = false,
            ChildExited = exited,
            CaptureJoined = captureJoin.IsCompleted,
            CaptureFaulted = captureJoin.IsFaulted,
            CleanupElapsedMilliseconds = clock.ElapsedMilliseconds,
            CleanupDeadlineSeconds = RawCaptureNames.CleanupDeadlineSeconds,
            FailureType = failure?.GetType().FullName,
            Failure = failure?.ToString()
        };
        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true
        }) + RawCaptureNames.NewLine);
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();

        return report;
    }
}
