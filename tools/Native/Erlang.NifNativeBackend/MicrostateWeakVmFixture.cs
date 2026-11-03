namespace Erlang.NifNativeBackend;

internal sealed record MicrostateWeakVmFixture(WeakReference Runtime, RuntimeMicrostateAccounting Counter);
