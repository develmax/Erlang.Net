using System.Runtime.InteropServices;

namespace Erlang;

internal static class PrimFileWindowsDirectory410
{
    internal static Term Run(Term term, CancellationToken cancellation)
    {
        PrimFileWindowsOperations410.Require(cancellation);
        if (!PrimFileWindowsPaths410.Marshal(term, out string path, out string error))
            return PrimFileWindowsErrors410.Tuple(error);
        IntPtr search = PrimFileWindowsNative410.First(path + PrimFileWindowsNames410.SearchSuffix, out var entry);
        int last = Marshal.GetLastPInvokeError();
        if (search == new IntPtr(-1)) return PrimFileWindowsErrors410.Last(last);
        try
        {
            Term result = Nil.Value;
            do
            {
                cancellation.ThrowIfCancellationRequested();
                if (entry.Name != PrimFileWindowsNames410.Dot && entry.Name != PrimFileWindowsNames410.DotDot)
                    result = new Cons(new BitString(PrimFileWindowsPaths410.Bytes(entry.Name)), result);
            } while (PrimFileWindowsNative410.Next(search, out entry));

            // Pinned efile_list_dir intentionally ignores terminal FindNext error.
            return PrimFileWindowsErrors410.Ok(result);
        }
        finally
        {
            _ = PrimFileWindowsNative410.CloseSearch(search);
        }
    }
}
