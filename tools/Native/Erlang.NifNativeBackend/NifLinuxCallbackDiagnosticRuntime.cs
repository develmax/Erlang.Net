namespace Erlang.NifNativeBackend;

internal sealed class NifLinuxCallbackDiagnosticRuntime(ProcessRuntime runtime, string destination) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.RuntimeDisposeBefore);
        try
        {
            await runtime.DisposeAsync();
            NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.RuntimeDisposeAfter);
        }
        catch (Exception failure)
        {
            NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.RuntimeDisposeFailed, failure: failure);
            throw;
        }
    }
}
