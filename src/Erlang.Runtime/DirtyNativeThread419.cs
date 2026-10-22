namespace Erlang;

internal static class DirtyNativeThread419
{
    [ThreadStatic] internal static ProcessContext? Owner;
    [ThreadStatic] internal static DirtyNativeKind419 Kind;

    internal static DirtyNativeKind419 Read(ProcessContext context) => ReferenceEquals(Owner, context) ? Kind : DirtyNativeKind419.Normal;
}
