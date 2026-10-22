using System.Runtime.InteropServices;

namespace Erlang;

internal static class PrimFileWindowsPaths410
{
    internal static byte[] Bytes(string value)
    {
        var bytes = new byte[checked(value.Length * 2)];
        for (int index = 0; index < value.Length; index++)
        {
            bytes[index * 2] = (byte)value[index];
            bytes[index * 2 + 1] = (byte)(value[index] >> 8);
        }

        return bytes;
    }

    internal static bool Marshal(Term term, out string path, out string error)
    {
        path = string.Empty;
        error = PrimFileWindowsNames410.Invalid;
        if (term is not BitString { IsBinary: true } binary) return false;
        byte[] bytes = binary.ToArray();
        if (bytes.Length < 2 || bytes.Length % 2 != 0) return false;
        var chars = new char[bytes.Length / 2];
        int firstNull = -1;
        for (int index = 0; index < chars.Length; index++)
        {
            chars[index] = (char)(bytes[index * 2] | (bytes[index * 2 + 1] << 8));
            if (chars[index] == 0)
            {
                if (firstNull < 0) firstNull = index;
            }
            else if (firstNull >= 0) return false;
        }
        if (firstNull < 0) return false;

        return Full(new string(chars, 0, firstNull), out path, out error);
    }

    internal static bool Full(string input, out string path, out string error)
    {
        path = string.Empty;
        error = PrimFileWindowsNames410.Missing;
        uint length = PrimFileWindowsNative410.FullPath(
            input,
            0,
            null,
            IntPtr.Zero
        );
        if (length == 0) return false;
        uint capacity = checked(length + (uint)PrimFileWindowsNames410.LongUncPrefix.Length);
        var buffer = new char[checked((int)capacity)];
        uint actual = PrimFileWindowsNative410.FullPath(
            input,
            capacity,
            buffer,
            IntPtr.Zero
        );
        if (actual >= capacity)
        {
            error = PrimFileWindowsNames410.Invalid;

            return false;
        }
        // Preserve native zero-length second-result behavior as an explicit failure.
        if (actual == 0)
        {
            error = PrimFileWindowsNames410.Invalid;

            return false;
        }
        path = new string(buffer, 0, (int)actual);
        if (!Long(path))
            path = path.StartsWith(PrimFileWindowsNames410.UncPrefix, StringComparison.Ordinal)
                ? PrimFileWindowsNames410.LongUncPrefix + path[2..]
                : PrimFileWindowsNames410.LongPrefix + path;
        if (Slash(path[^1]) && path.Length >= 2 && path[^2] != ':') path = path[..^1];

        return true;
    }

    internal static bool Long(string path)
        => path.StartsWith(PrimFileWindowsNames410.LongPrefix, StringComparison.Ordinal)
            || path.StartsWith(PrimFileWindowsNames410.DevicePrefix, StringComparison.Ordinal);

    internal static bool Slash(char value) => value is '/' or '\\';

    internal static bool Root(string path)
    {
        if (!path.StartsWith(PrimFileWindowsNames410.LongUncPrefix, StringComparison.Ordinal))
        {
            ReadOnlySpan<char> rest = path.AsSpan(PrimFileWindowsNames410.LongPrefix.Length);

            return rest.Length == 1 && Slash(rest[0]) || rest.Length == 3
                && (rest[0] is >= 'A' and <= 'Z' or >= 'a' and <= 'z') && rest[1] == ':' && Slash(rest[2]);
        }
        ReadOnlySpan<char> unc = path.AsSpan(PrimFileWindowsNames410.LongUncPrefix.Length);
        if (unc.Length <= 1) return false;
        int index = 0;
        while (index < unc.Length && !Slash(unc[index])) index++;
        do
        {
            index++;
        } while (index < unc.Length && !Slash(unc[index]));

        return index >= unc.Length && !Slash(unc[^1]);
    }

    internal static uint Drive(string path)
    {
        ReadOnlySpan<char> rest = path.AsSpan(PrimFileWindowsNames410.LongPrefix.Length);
        if (rest.Length >= 2 && rest[1] == ':')
        {
            if (rest[0] is >= 'A' and <= 'Z') return (uint)(rest[0] - 'A' + 1);
            if (rest[0] is >= 'a' and <= 'z') return (uint)(rest[0] - 'a' + 1);
        }

        return uint.MaxValue;
    }

    internal static string NormalizeResult(string path)
    {
        if (path.StartsWith(PrimFileWindowsNames410.LongUncPrefix, StringComparison.Ordinal))
            path = PrimFileWindowsNames410.UncPrefix + path[PrimFileWindowsNames410.LongUncPrefix.Length..];
        else if (Long(path))
            path = path[PrimFileWindowsNames410.LongPrefix.Length..];
        if (path.Length >= 2 && path[1] == ':' && path[0] is >= 'A' and <= 'Z')
            path = (char)(path[0] - 'A' + 'a') + path[1..];

        return path.Replace('\\', '/');
    }
}
