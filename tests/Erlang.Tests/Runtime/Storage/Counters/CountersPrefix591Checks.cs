using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using static Erlang.Tests.CountersPrefix591Names;
using static Erlang.Tests.NativeResource524Access;

namespace Erlang.Tests;

internal static class CountersPrefix591Checks
{
    internal static Task ActualPrefix() => Cases((
        array,
        pointer,
        length,
        lanes
    ) =>
    {
        long magic = Convert.ToInt64(typeof(ProcessRuntime).Assembly.GetType(MagicType, true)!
            .GetField(Magic, Members)!.GetRawConstantValue());
        Require(Marshal.ReadInt64(pointer, FlagsOffset) == magic);
        ulong total = (ulong)Property(array, AllocatedBytes);
        Require(Marshal.ReadInt64(pointer, OriginalSizeOffset) == checked((long)total - DestructorOffset));
        Require(Marshal.ReadInt64(pointer, ApparentSizeOffset) == checked((long)total - ArityOffset));
        Require(Marshal.ReadInt64(pointer, ArityOffset) == checked((long)length));
        Require(Marshal.ReadIntPtr(pointer, DestructorOffset) != 0);
        using var pin = (IDisposable)Call(array, Acquire)!;
        Require((ulong)Property(array, Length) == length);
        Require((ulong)Property(array, OriginalSize) == total - DestructorOffset);
    });

    internal static Task ActualLaneGeometry() => Cases((
        array,
        pointer,
        length,
        lanes
    ) =>
    {
        // First validate new shape before writing any bytes on the old RED
        // allocation: the existing baseline must fail safely, not corrupt it.
        Type prefix = typeof(ProcessRuntime).Assembly.GetType(PrefixType, true)!;
        Require(Marshal.OffsetOf(prefix, Length).ToInt64() == ArityOffset);
        Require(Marshal.SizeOf(prefix) == CellsOffset);
        using var pin = (IDisposable)Call(array, Acquire)!;
        foreach (ulong index in new[] { 0UL, length - 1 }.Distinct())
        {
            for (int lane = 0; lane <= lanes; lane++)
            {
                long offset = checked(CellsOffset + (long)((index / CellsPerLine) * (ulong)(lanes + 1) + (uint)lane) * CacheLineBytes
                    + (long)(index % CellsPerLine) * sizeof(long));
                Marshal.WriteInt64(pointer, checked((int)offset), Sentinel + lane);
                Require((long)Call(
                    array,
                    Cell,
                    index,
                    lane
                )! == Sentinel + lane);
                Marshal.WriteInt64(pointer, checked((int)offset), 0);
            }
        }
    });

    private static Task Cases(Action<object, nint, ulong, int> body)
    {
        var failures = new List<Exception>();
        foreach (int lanes in new[] { 1, 2, 4 })
        {
            foreach (ulong length in new[] { 1UL, 8UL, 9UL, 17UL })
            {
                object? array = null;
                try
                {
                    Type type = typeof(ProcessRuntime).Assembly.GetType(ArrayType, true)!;
                    array = Activator.CreateInstance(
                        type,
                        Members,
                        null,
                        [length, lanes],
                        null
                    )!;
                    nint pointer = (nint)type.GetField(Memory, Members)!.GetValue(array)!;
                    body(
                        array,
                        pointer,
                        length,
                        lanes
                    );
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
                finally
                {
                    if (array is not null)
                        try
                        {
                            Call(array, Close);
                        }
                        catch (Exception error)
                        {
                            failures.Add(error);
                        }
                }
            }
        }
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(CleanupFailure, failures);

        return Task.CompletedTask;
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
