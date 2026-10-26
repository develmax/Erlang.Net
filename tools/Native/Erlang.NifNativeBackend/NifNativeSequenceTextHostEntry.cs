namespace Erlang.NifNativeBackend;

public static class NifNativeSequenceTextHostEntry
{
    public static Task TupleLifetime() => NifNativeSequenceTextHostChecks.Run(NifNativeSequenceTextHostChecks.TupleLifetime);

    public static Task ListLengths() => NifNativeSequenceTextHostChecks.Run(NifNativeSequenceTextHostChecks.ListLengths);

    public static Task Atoms() => NifNativeSequenceTextHostChecks.Run(NifNativeSequenceTextHostChecks.Atoms);

    public static Task Strings() => NifNativeSequenceTextHostChecks.Run(NifNativeSequenceTextHostChecks.Strings);

    public static Task InvocationOwnership() => NifNativeSequenceTextHostChecks.Run(NifNativeSequenceTextHostChecks.InvocationOwnership);

    public static Task Containment() => NifNativeSequenceTextHostChecks.Run(NifNativeSequenceTextHostChecks.Containment);
}
