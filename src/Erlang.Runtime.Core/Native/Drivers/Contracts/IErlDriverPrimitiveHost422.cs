namespace Erlang;

// The dedicated child must proxy these to the owning runtime. A child-local
// clock or OS environment would silently create a second VM state.
internal interface IErlDriverPrimitiveHost422
{
    long GetNowMicroseconds();

    long Clock(bool offset, int unit);

    string? ReadEnvironment(byte[] nativeKey);

    int WriteEnvironment(byte[] nativeKey, byte[] nativeValue);

    ErlDriverSystemInfo422 SystemInfo { get; }
}
