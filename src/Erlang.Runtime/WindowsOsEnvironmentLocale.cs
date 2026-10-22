using System.Runtime.InteropServices;

namespace Erlang;

internal sealed unsafe class WindowsOsEnvironmentLocale : SafeHandle
{
    internal WindowsOsEnvironmentLocale() : base(0, true)
    {
        SetHandle(Create(OsNativeNames.AllLocaleCategories, OsNativeNames.InvariantLocale));
        if (IsInvalid) throw new InvalidOperationException(OsNativeNames.LocaleCreationFailed);
    }

    public override bool IsInvalid => handle == 0;

    protected override bool ReleaseHandle()
    {
        Free(handle);

        return true;
    }

    internal int Compare(string left, string right)
    {
        int relation;
        fixed (char* l = left)
        fixed (char* r = right)
            relation = CompareNative(
                l,
                r,
                (nuint)Math.Min(left.Length, right.Length),
                this
            );

        return relation != 0 ? relation : left.Length.CompareTo(right.Length);
    }

    [DllImport(OsNativeNames.CrtLibrary, EntryPoint = OsNativeNames.CreateLocaleEntry, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern nint Create(int category, string name);

    [DllImport(OsNativeNames.CrtLibrary, EntryPoint = OsNativeNames.FreeLocaleEntry, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern void Free(nint locale);

    [DllImport(OsNativeNames.CrtLibrary, EntryPoint = OsNativeNames.CompareLocaleEntry, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int CompareNative(
        char* left,
        char* right,
        nuint length,
        WindowsOsEnvironmentLocale locale
    );
}
