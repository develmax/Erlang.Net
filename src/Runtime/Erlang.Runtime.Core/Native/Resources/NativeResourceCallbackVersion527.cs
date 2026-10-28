namespace Erlang;

internal sealed class NativeResourceCallbackVersion527(
    NifNativeResourceImage524 image,
    NativeResourceCallbacks527 callbacks
) : IDisposable
{
    internal NifNativeResourceImage524 Image527 { get; } = image;
    internal NativeResourceCallbacks527 Callbacks527 { get; } = callbacks;
    private NifResourceImagePin524? pin;

    internal void Prepare527(NifResourceImagePin524 authority)
    {
        if (Callbacks527.HasCallbacks527 && pin is null) pin = authority.Fork524();
    }

    internal void RequireReady527()
    {
        if (Callbacks527.HasCallbacks527 && pin is null) pin = Image527.Retain524();
    }

    // Capture while the table reader/type lock still selects this exact
    // version. A later takeover may drop the base pin after the reader exits;
    // this invocation pin keeps old code/private data alive until C returns.
    internal NifResourceImagePin524? RetainCallback529()
        => Callbacks527.HasCallbacks527
            ? (pin ?? throw new InvalidOperationException(NativeResource524Names.ImageUnavailable)).Fork524()
            : null;

    public void Dispose() => Interlocked.Exchange(ref pin, null)?.Dispose();
}
