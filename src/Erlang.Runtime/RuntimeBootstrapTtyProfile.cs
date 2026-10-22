namespace Erlang;

public enum RuntimeBootstrapTtyProfile
{
    // Explicit own-runtime profile: real pipe/disk handles, no console mutation,
    // absent termcap, ANSI FormatMessage byte ABI. Broader console profile held.
    WindowsRedirectedNoTermcapAnsi = 1
}
