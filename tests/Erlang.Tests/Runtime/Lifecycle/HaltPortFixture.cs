using System.Diagnostics;

namespace Erlang.Tests;

internal sealed class HaltPortFixture(
    ProcessRuntime runtime,
    ProcessHandle handle,
    ProcessContext context,
    PortTerm port,
    Process child,
    string? capture,
    string? release = null
) : IAsyncDisposable
{
    public ProcessRuntime Runtime { get; } = runtime;
    public ProcessHandle Handle { get; } = handle;
    public ProcessContext Context { get; } = context;
    public PortTerm Port { get; } = port;
    public Process Child { get; } = child;
    public string? Capture { get; } = capture;
    public string? Release { get; } = release;

    public void ReleaseChild()
    {
        if (Release is not null)
            File.WriteAllText(Release, string.Empty);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            ReleaseChild();
            await Runtime.DisposeAsync().AsTask().WaitAsync(HaltPortHostNames.Timeout);
        }
        finally
        {
            Child.Dispose();
            if (Capture is not null && File.Exists(Capture))
                File.Delete(Capture);
            if (Release is not null && File.Exists(Release))
                File.Delete(Release);
        }
    }
}
