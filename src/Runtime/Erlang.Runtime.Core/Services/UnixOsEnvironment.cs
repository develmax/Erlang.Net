using System.Runtime.InteropServices;
using System.Text;

namespace Erlang;

internal sealed class UnixOsEnvironment
{
    private readonly object gate = new();
    private readonly Dictionary<string, byte[]> values = new(StringComparer.Ordinal);

    [DllImport(PrimFilePosixNames.Library, EntryPoint = UnixOsNativeNames.Snapshot, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Snapshot(out nint buffer, out int length);

    [DllImport(PrimFilePosixNames.Library, EntryPoint = UnixOsNativeNames.Free, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void Free(nint buffer);

    internal UnixOsEnvironment()
    {
        int error = Snapshot(out nint buffer, out int length);
        if (error != 0) throw new IOException(UnixOsNativeNames.SnapshotFailed + ": " + error);
        try
        {
            byte[] bytes = new byte[length];
            Marshal.Copy(
                buffer,
                bytes,
                0,
                length
            );
            int position = 0;
            while (position < length)
            {
                int end = Array.IndexOf(bytes, (byte)0, position);
                int separator = Array.IndexOf(
                    bytes,
                    (byte)'=',
                    position,
                    end - position
                );
                if (separator >= position)
                    values[Encoding.Latin1.GetString(bytes, position, separator - position)] = bytes.AsSpan(separator + 1, end - separator - 1).ToArray();
                position = end + 1;
            }
        }
        finally
        {
            Free(buffer);
        }
    }

    private static async ValueTask<byte[]> Encode(ProcessContext context, Term value, bool key)
    {
        var native = (BitString)await new PrimFileEncodingWork410(context).Run(PrimFileEncodingNames410.ToNative, value);
        byte[] bytes = native.ToArray();
        Array.Resize(ref bytes, bytes.Length - 1);
        if (key && bytes.Contains((byte)'=')) throw new ErlangException(ErlangErrorReasons.BadArgument);

        return bytes;
    }

    private static async ValueTask<Term> Decode(ProcessContext context, byte[] bytes)
    {
        Term result = await new PrimFileEncodingWork410(context).Run(PrimFileEncodingNames410.FromNative, new BitString(bytes));

        return result is TupleTerm ? new BitString(bytes) : result;
    }

    internal async ValueTask<Term> Invoke(ProcessContext context, string operation, IReadOnlyList<Term> arguments)
    {
        if (operation == OsNativeNames.Env)
        {
            KeyValuePair<string, byte[]>[] entries;
            lock (gate) entries = values.ToArray();
            var result = new List<Term>();
            foreach (var entry in entries)
                result.Add(Term.Tuple(await Decode(context, Encoding.Latin1.GetBytes(entry.Key)), await Decode(context, entry.Value)));

            return Cons.From(result);
        }
        string name = Encoding.Latin1.GetString(await Encode(context, arguments[0], true));
        if (operation == OsNativeNames.PutEnv)
        {
            byte[] value = await Encode(context, arguments[1], false);
            lock (gate) values[name] = value;

            return Term.A(CommonAtomNames.True);
        }
        if (operation == OsNativeNames.UnsetEnv)
        {
            lock (gate) values.Remove(name);

            return Term.A(CommonAtomNames.True);
        }
        byte[]? found;
        lock (gate) values.TryGetValue(name, out found);

        return found is null ? Term.A(CommonAtomNames.False) : await Decode(context, found);
    }
}
