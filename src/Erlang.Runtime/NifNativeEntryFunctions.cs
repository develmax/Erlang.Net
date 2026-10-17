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
        string libraryModule = ReadLatin1(entry.Name);
        if (libraryModule != module)
            throw new NifNativeEntryAdmissionException(
                NifNativeDiagnostics.ModuleIdentity,
                new NifNativeLoadReason(
                    NifNativeLoadReasonKind.ModuleMismatch,
                    module,
                    libraryModule,
                    string.Empty,
                    0,
                    0
                )
            );
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
                throw FunctionError(
                    NifNativeLoadReasonKind.MissingFunction,
                    NifNativeDiagnostics.NoDeclaredNif,
                    module,
                    name,
                    function.Arity
                );
            var key = (name, (int)function.Arity);
            if (!descriptor.Functions.Contains(key))
                throw FunctionError(
                    NifNativeLoadReasonKind.MissingFunction,
                    NifNativeDiagnostics.NoDeclaredNif,
                    module,
                    name,
                    function.Arity
                );
            if (descriptor.DeclaredNifs is { } declared && !declared.Contains(key))
                throw FunctionError(
                    NifNativeLoadReasonKind.UndeclaredFunction,
                    NifNativeDiagnostics.NoDeclaredNif,
                    module,
                    name,
                    function.Arity
                );
            if (function.Flags != 0 || function.Function == 0)
                throw new NotSupportedException(NifNativeDiagnostics.UntrustedFunction);
            if (!functions.TryAdd(key, function.Function))
                throw FunctionError(
                    NifNativeLoadReasonKind.DuplicateFunction,
                    NifNativeDiagnostics.UntrustedFunction,
                    module,
                    name,
                    function.Arity
                );
        }

        return functions;
    }

    private static NifNativeEntryAdmissionException FunctionError(
        NifNativeLoadReasonKind kind,
        string diagnostic,
        string module,
        string function,
        uint arity
    ) => new(
        diagnostic,
        new NifNativeLoadReason(
            kind,
            module,
            string.Empty,
            function,
            arity,
            0
        )
    );

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
