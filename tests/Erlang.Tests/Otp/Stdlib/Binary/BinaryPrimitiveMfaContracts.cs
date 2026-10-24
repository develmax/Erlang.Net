using System.Numerics;

namespace Erlang.Tests;

internal static class BinaryPrimitiveMfaContracts
{
    internal static bool Owns(string module, string function, int arity)
        => module == BinaryModuleNames.Module && (function, arity) is
            (BinaryModuleNames.At, 2) or (BinaryModuleNames.First, 1) or (BinaryModuleNames.Last, 1)
            or (BinaryModuleNames.Part, 2) or (BinaryModuleNames.Part, 3)
            or (BinaryModuleNames.BinToList, 1) or (BinaryModuleNames.BinToList, 2) or (BinaryModuleNames.BinToList, 3)
            or (BinaryModuleNames.ListToBin, 1) or (BinaryModuleNames.Copy, 1) or (BinaryModuleNames.Copy, 2)
            or (BinaryModuleNames.DecodeUnsigned, 1) or (BinaryModuleNames.DecodeUnsigned, 2)
            or (BinaryModuleNames.EncodeUnsigned, 1) or (BinaryModuleNames.EncodeUnsigned, 2)
            or (BinaryModuleNames.LongestCommonPrefix, 1) or (BinaryModuleNames.LongestCommonSuffix, 1)
            or (BinaryModuleNames.ReferencedByteSize, 1);

    internal static async Task Run(string function, int arity)
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await owner.InvokeAsync(async context =>
        {
            var bytes = new BitString(new byte[] { 1, 2, 3 });
            Term[] arguments;
            Term expected;
            switch (function)
            {
                case BinaryModuleNames.At:
                    arguments = [bytes, Term.I(1)]; expected = Term.I(2); break;
                case BinaryModuleNames.First:
                    arguments = [bytes]; expected = Term.I(1); break;
                case BinaryModuleNames.Last:
                    arguments = [bytes]; expected = Term.I(3); break;
                case BinaryModuleNames.Part:
                    arguments = arity == 2 ? [bytes, Term.Tuple(Term.I(3), Term.I(-2))] : [bytes, Term.I(3), Term.I(-2)];
                    expected = new BitString(new byte[] { 2, 3 }); break;
                case BinaryModuleNames.BinToList:
                    arguments = arity == 1 ? [bytes] : arity == 2 ? [bytes, Term.Tuple(Term.I(3), Term.I(-3))] : [bytes, Term.I(3), Term.I(-3)];
                    expected = Term.List(Term.I(1), Term.I(2), Term.I(3)); break;
                case BinaryModuleNames.ListToBin:
                    arguments = [Term.List(Term.I(1), new BitString(new byte[] { 2, 3 }))]; expected = bytes; break;
                case BinaryModuleNames.Copy:
                    arguments = arity == 1 ? [bytes] : [bytes, Term.I(2)];
                    expected = arity == 1 ? bytes : new BitString(new byte[] { 1, 2, 3, 1, 2, 3 }); break;
                case BinaryModuleNames.DecodeUnsigned:
                    arguments = arity == 1 ? [bytes] : [bytes, Term.A(BinaryPrimitiveHostNames.Little)];
                    expected = Term.I(arity == 1 ? 0x010203 : 0x030201); break;
                case BinaryModuleNames.EncodeUnsigned:
                    arguments = arity == 1 ? [Term.I(0x010203)] : [Term.I(0x030201), Term.A(BinaryPrimitiveHostNames.Little)];
                    expected = bytes; break;
                case BinaryModuleNames.LongestCommonPrefix:
                case BinaryModuleNames.LongestCommonSuffix:
                    arguments = [Term.List(bytes, bytes)]; expected = Term.I(3); break;
                case BinaryModuleNames.ReferencedByteSize:
                    arguments = [bytes]; expected = Term.I(3); break;
                default:
                    throw new InvalidOperationException(BinaryPrimitiveHostNames.Failure);
            }
            var actual = await runtime.Modules.Call(
                context,
                BinaryModuleNames.Module,
                function,
                arguments
            );
            if (!actual.Equals(expected))
                throw new InvalidOperationException(BinaryPrimitiveHostNames.Failure);

            return Nil.Value;
        });
    }
}
