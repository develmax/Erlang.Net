using System.Runtime.InteropServices;
namespace Erlang;

internal sealed unsafe partial class ErlDriverPrimitiveServices422
{
    [DllImport(ErlDriverPrimitiveNames422.Kernel, EntryPoint = "WideCharToMultiByte", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern int EncodeAcp(
        uint page,
        uint flags,
        string text,
        int length,
        nint output,
        int capacity,
        nint replacement,
        nint usedReplacement
    );

    private static long Frequency(int unit) => unit switch
    {
        ErlDriverPrimitiveNames422.Second => 1,
        ErlDriverPrimitiveNames422.Millisecond => 1000,
        ErlDriverPrimitiveNames422.Microsecond => 1000000,
        ErlDriverPrimitiveNames422.Nanosecond => 1000000000,
        _ => 0
    };

    private static long ConvertTime(long value, int from, int to)
    {
        long input = Frequency(from), output = Frequency(to);
        if (input == 0 || output == 0) return ErlDriverPrimitiveNames422.TimeError;
        if (output >= input) return unchecked(value * (output / input));
        long denominator = input / output;

        // Preserve the pinned native signed-word arithmetic, including its
        // boundary wrap, rather than silently changing the native ABI result.
        return value >= 0 ? value / denominator : unchecked(value - (denominator - 1)) / denominator;
    }

    private long ReadClock(bool offset, int unit)
        => !schedulerThread() || Frequency(unit) == 0 ? ErlDriverPrimitiveNames422.TimeError : host.Clock(offset, unit);

    private int GetNow(nint buffer)
    {
        if (buffer == nint.Zero) return -1;
        long value = host.GetNowMicroseconds();
        long seconds = value / ErlangClockParameters.MicrosecondsPerSecond;
        Marshal.WriteInt32(buffer, Field(ErlDriverLayoutNames422.NowMega), unchecked((int)(seconds / ErlangClockParameters.SecondsPerMegaSecond)));
        Marshal.WriteInt32(buffer, Field(ErlDriverLayoutNames422.NowSeconds), unchecked((int)(seconds % ErlangClockParameters.SecondsPerMegaSecond)));
        Marshal.WriteInt32(buffer, Field(ErlDriverLayoutNames422.NowMicro), unchecked((int)(value % ErlangClockParameters.MicrosecondsPerSecond)));

        return 0;
    }

    private int GetEnvironment(nint key, nint value, nint lengthPointer)
    {
        nuint capacity = checked((nuint)Marshal.ReadInt64(lengthPointer));
        string? text = host.ReadEnvironment(CString(key));
        Marshal.WriteInt64(lengthPointer, 0);
        if (text is null) return -1;
        if (text.Length == 0) return 0;
        if (capacity > (nuint)int.MaxValue || text.Length > (long)capacity * 2 || capacity <= 1) return 1;
        int count = EncodeAcp(
            ErlDriverPrimitiveNames422.Acp,
            0,
            text,
            text.Length,
            value,
            checked((int)capacity - 1),
            nint.Zero,
            nint.Zero
        );
        if (count == 0) return Marshal.GetLastPInvokeError() == ErlDriverPrimitiveNames422.InsufficientBuffer ? 1 : -1;
        ((byte*)value)[count] = 0;
        Marshal.WriteInt64(lengthPointer, count);

        return 0;
    }

    private void SystemInfo(nint buffer, nuint length)
    {
        if (buffer == nint.Zero || length < (nuint)checked(Field(ErlDriverLayoutNames422.SysSmp) + sizeof(int)))
            throw new ArgumentException(ErlDriverPrimitiveNames422.InvalidSystemInfo);
        ErlDriverSystemInfo422 info = host.SystemInfo;
        Marshal.WriteInt32(buffer, Field(ErlDriverLayoutNames422.SysDriverMajor), ErlDriverPrimitiveNames422.DriverMajor);
        Marshal.WriteInt32(buffer, Field(ErlDriverLayoutNames422.SysDriverMinor), ErlDriverPrimitiveNames422.DriverMinor);
        Marshal.WriteIntPtr(buffer, Field(ErlDriverLayoutNames422.SysErts), PermanentString(info.ErtsVersion));
        Marshal.WriteIntPtr(buffer, Field(ErlDriverLayoutNames422.SysOtp), PermanentString(info.OtpRelease));
        Marshal.WriteInt32(buffer, Field(ErlDriverLayoutNames422.SysThreads), 1);
        Marshal.WriteInt32(buffer, Field(ErlDriverLayoutNames422.SysSmp), 1);
        if (length >= (nuint)checked(Field(ErlDriverLayoutNames422.SysSchedulers) + sizeof(int)))
        {
            Marshal.WriteInt32(buffer, Field(ErlDriverLayoutNames422.SysAsync), info.AsyncThreads);
            Marshal.WriteInt32(buffer, Field(ErlDriverLayoutNames422.SysSchedulers), info.SchedulerThreads);
        }
        if (length >= (nuint)checked(Field(ErlDriverLayoutNames422.SysNifMinor) + sizeof(int)))
        {
            Marshal.WriteInt32(buffer, Field(ErlDriverLayoutNames422.SysNifMajor), info.NifMajorVersion);
            Marshal.WriteInt32(buffer, Field(ErlDriverLayoutNames422.SysNifMinor), info.NifMinorVersion);
        }
        if (length >= (nuint)checked(Field(ErlDriverLayoutNames422.SysDirty) + sizeof(int)))
            Marshal.WriteInt32(buffer, Field(ErlDriverLayoutNames422.SysDirty), info.DirtySchedulerSupport ? 1 : 0);
    }
}
