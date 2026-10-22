using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Erlang;

internal static class RuntimeManagedObjectExtent420
{
    private static readonly Lazy<Func<object, nuint>> Reader = new(CreateReader);

    internal static nuint Measure(object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        nuint raw = Reader.Value(value);

        return checked((raw + ManagedObjectExtentNames420.HeaderBytes + ManagedObjectExtentNames420.AlignmentMask)
            & ~ManagedObjectExtentNames420.AlignmentMask);
    }

    private static Func<object, nuint> CreateReader()
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64
            || IntPtr.Size != ManagedObjectExtentNames420.PointerBytes)
            throw new PlatformNotSupportedException(ManagedObjectExtentNames420.UnadmittedRuntime);

        string coreLib = typeof(object).Assembly.Location;
        if (string.IsNullOrEmpty(coreLib))
            throw new PlatformNotSupportedException(ManagedObjectExtentNames420.UnadmittedRuntime);

        string clr = Path.Combine(Path.GetDirectoryName(coreLib)!, ManagedObjectExtentNames420.ClrFile);
        if (!Matches(coreLib, ManagedObjectExtentNames420.CoreLibHash)
            || !Matches(clr, ManagedObjectExtentNames420.ClrHash))
            throw new PlatformNotSupportedException(ManagedObjectExtentNames420.UnadmittedRuntime);

        MethodInfo? method = typeof(RuntimeHelpers).GetMethod(
            ManagedObjectExtentNames420.RawMethod,
            BindingFlags.Static | BindingFlags.NonPublic,
            [typeof(object)]);

        if (method is null || !method.IsAssembly || !method.IsStatic || method.ReturnType != typeof(nuint)
            || method.MetadataToken != ManagedObjectExtentNames420.RawMethodToken)
            throw new PlatformNotSupportedException(ManagedObjectExtentNames420.UnadmittedRuntime);

        return method.CreateDelegate<Func<object, nuint>>();
    }

    private static bool Matches(string path, string expected)
    {
        using var input = File.OpenRead(path);

        return Convert.ToHexString(SHA256.HashData(input)) == expected;
    }
}
