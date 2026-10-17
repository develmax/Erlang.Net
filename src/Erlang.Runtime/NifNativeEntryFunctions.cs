using System.Runtime.InteropServices;
using System.Text;

namespace Erlang;

internal static unsafe class NifNativeEntryFunctions
{
    internal static IReadOnlyDictionary<(string Name, int Arity), nint> Read(
        NifNativeAbiEntry entry,
        string module,
        ModuleCodeDescriptor descriptor
    )
    {
        if (ReadLatin1(entry.Name) != module)
            throw new NotSupportedException(NifNativeDiagnostics.ModuleIdentity);
        if (Marshal.PtrToStringUTF8(entry.Variant) != NifNativeNames.Variant ||
            entry.FunctionCount < 0 || entry.FunctionCount > 0 && entry.Functions == 0 ||
            entry.Options != NifNativeAbi.InitializerEntryOptions)
            throw new NotSupportedException(NifNativeDiagnostics.UnsupportedNativeEntry);
        var functions = new Dictionary<(string Name, int Arity), nint>();
        for (int index = 0; index < entry.FunctionCount; index++)
        {
            var function = ((NifNativeAbiFunction*)entry.Functions)[index];
            string name = ReadLatin1(function.Name);
            if (function.Arity > int.MaxValue)
                throw new NotSupportedException(NifNativeDiagnostics.NoDeclaredNif);
            var key = (name, (int)function.Arity);
            if (!descriptor.Functions.Contains(key) ||
                descriptor.DeclaredNifs is { } declared && !declared.Contains(key))
                throw new NotSupportedException(NifNativeDiagnostics.NoDeclaredNif);
            if (function.Flags != 0 || function.Function == 0 || !functions.TryAdd(key, function.Function))
                throw new NotSupportedException(NifNativeDiagnostics.UntrustedFunction);
        }

        return functions;
    }

    private static string ReadLatin1(nint pointer)
    {
        if (pointer == 0)
            throw new NotSupportedException(NifNativeDiagnostics.UnsupportedNativeEntry);
        Span<byte> bytes = stackalloc byte[NifNativeEntryTextLimits.MaximumLatin1AtomBytes];
        for (int index = 0; index <= bytes.Length; index++)
        {
            byte value = Marshal.ReadByte(pointer, index);
            if (value == 0)
                return Encoding.Latin1.GetString(bytes[..index]);
            if (index == bytes.Length)
                throw new NotSupportedException(NifNativeDiagnostics.UnsupportedNativeEntry);
            bytes[index] = value;
        }

        throw new NotSupportedException(NifNativeDiagnostics.UnsupportedNativeEntry);
    }
}
