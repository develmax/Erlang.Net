namespace Erlang.NifNativeBackend;

internal sealed record CpuVmLeakFixture(WeakReference Runtime, RuntimeUserCpuAccounting Counter);
