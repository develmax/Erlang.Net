using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Erlang.NifNativeBackend;

public static class NifVersion651Checks
{
    public static unsafe Task Run()
    {
        for (int minor = 0; minor <= NifNativeAbi.MinorVersion; minor++)
        {
            if (minor == NifNativeVersion651.RejectedMapsMinor)
                continue;
            Check(minor, 0);
            Check(minor, NifNativeAbi.InitializerEntryOptions);
        }

        return Task.CompletedTask;
    }

    private static unsafe void Check(int minor, uint options)
    {
        int bytes = minor >= NifNativeVersion651.MinimumErtsMinor ? NifNativeAbi.EntryBytes
            : minor >= NifNativeVersion651.ResourceInitMinor ? NifVersion651Names.PrefixWithResourceBytes
            : minor >= NifNativeVersion651.FlagsMinor ? NifVersion651Names.PrefixWithOptionsBytes
            : minor >= NifNativeVersion651.VariantMinor ? NifVersion651Names.PrefixWithVariantBytes
            : NifNativeVersion651.PrefixBytes;
        int stride = minor >= NifNativeVersion651.FlagsMinor ? NifNativeAbi.FunctionBytes : NifNativeVersion651.LegacyFunctionBytes;
        nint pointer = 0, functions = 0, module = 0, variant = 0, first = 0, second = 0, minimum = 0;
        try
        {
            pointer = Marshal.AllocHGlobal(bytes);
            new Span<byte>((void*)pointer, bytes).Clear();
            functions = Marshal.AllocHGlobal(stride * NifVersion651Names.FunctionCount);
            new Span<byte>((void*)functions, stride * NifVersion651Names.FunctionCount).Clear();
            module = Marshal.StringToCoTaskMemUTF8(NifVersion651Names.Module);
            variant = Marshal.StringToCoTaskMemUTF8(NifNativeNames.Variant);
            first = Marshal.StringToCoTaskMemUTF8(NifVersion651Names.Identity);
            second = Marshal.StringToCoTaskMemUTF8(NifVersion651Names.Second);
            minimum = Marshal.StringToCoTaskMemUTF8(NifVersion651Names.MinimumErts);
            Marshal.StructureToPtr(
                new NifNativeAbiEntryPrefix651
                {
                    Major = NifNativeAbi.MajorVersion,
                    Minor = minor,
                    Name = module,
                    FunctionCount = NifVersion651Names.FunctionCount,
                    Functions = functions
                },
                pointer,
                false
            );
            if (minor >= NifNativeVersion651.VariantMinor)
                Marshal.WriteIntPtr(pointer, NifNativeVersion651.VariantOffset, variant);
            if (minor >= NifNativeVersion651.FlagsMinor)
                Marshal.WriteInt32(pointer, NifNativeVersion651.OptionsOffset, unchecked((int)options));
            if (minor >= NifNativeVersion651.ResourceInitMinor)
                Marshal.WriteIntPtr(pointer, NifNativeVersion651.ResourceInitOffset, (nint)NifNativeAbi.PointerBytes);
            if (minor >= NifNativeVersion651.MinimumErtsMinor)
                Marshal.WriteIntPtr(pointer, NifNativeVersion651.MinimumErtsOffset, minimum);
            for (int index = 0; index < NifVersion651Names.FunctionCount; index++)
                Marshal.StructureToPtr(
                    new NifNativeAbiFunctionLegacy651
                    {
                        Name = index == 0 ? first : second,
                        Arity = (uint)index,
                        Function = (nint)(index + 1)
                    },
                    functions + index * stride,
                    false
                );
            NifNativeAbiEntry entry = NifNativeEntryReader.Read(pointer, NifVersion651Names.Module);
            NifNativeHarness.Require(entry.Minor == minor);
            NifNativeHarness.Require(entry.Options == (minor >= NifNativeVersion651.FlagsMinor ? options : 0));
            NifNativeHarness.Require(entry.ResourceInitSize == (minor >= NifNativeVersion651.ResourceInitMinor ? (nuint)NifNativeAbi.PointerBytes : 0));
            NifNativeHarness.Require(entry.MinimumErts == (minor >= NifNativeVersion651.MinimumErtsMinor ? minimum : 0));
            var descriptor = new ModuleCodeDescriptor(
                [(NifVersion651Names.Identity, 0), (NifVersion651Names.Second, 1)],
                [],
                new BitString(new byte[MD5.HashSizeInBytes]),
                Nil.Value
            );
            NifNativeHarness.Require(NifNativeEntryFunctions.Read(entry, NifVersion651Names.Module, descriptor).Count == NifVersion651Names.FunctionCount);
        }
        finally
        {
            Marshal.FreeCoTaskMem(minimum);
            Marshal.FreeCoTaskMem(second);
            Marshal.FreeCoTaskMem(first);
            Marshal.FreeCoTaskMem(variant);
            Marshal.FreeCoTaskMem(module);
            Marshal.FreeHGlobal(functions);
            Marshal.FreeHGlobal(pointer);
        }
    }
}
