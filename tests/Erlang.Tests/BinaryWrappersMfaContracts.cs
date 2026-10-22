namespace Erlang.Tests;

internal static class BinaryWrappersMfaContracts
{
    internal static bool Owns(string module, string function, int arity)
        => module == BinaryModuleNames.Module && (function, arity) is
            (BinaryModuleNames.Replace, 3) or (BinaryModuleNames.Replace, 4)
            or (BinaryModuleNames.EncodeHex, 1) or (BinaryModuleNames.EncodeHex, 2)
            or (BinaryModuleNames.DecodeHex, 1) or (BinaryModuleNames.Join, 2);

    internal static async Task Run(string function, int arity)
    {
        if (!Owns(BinaryModuleNames.Module, function, arity))
            throw new InvalidOperationException(BinaryWrappersHostNames.Failure);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term[] arguments;
            Term expected;
            switch (function)
            {
                case BinaryModuleNames.Replace:
                    arguments = arity == 3
                        ? [new BitString(new byte[] { 1, 2, 1 }), new BitString(new byte[] { 1 }), new BitString(new byte[] { 9 })]
                        : [new BitString(new byte[] { 1, 2, 1 }), new BitString(new byte[] { 1 }), new BitString(new byte[] { 9 }), Term.List(Term.A(BinaryWrappersHostNames.Global))];
                    expected = new BitString(arity == 3 ? new byte[] { 9, 2, 1 } : new byte[] { 9, 2, 9 });
                    break;
                case BinaryModuleNames.EncodeHex:
                    Term binary = new BitString(new byte[] { 10, 240 });
                    arguments = arity == 1 ? [binary] : [binary, Term.A(BinaryWrappersHostNames.Lowercase)];
                    expected = new BitString(arity == 1 ? new byte[] { 48, 65, 70, 48 } : new byte[] { 48, 97, 102, 48 });
                    break;
                case BinaryModuleNames.DecodeHex:
                    arguments = [new BitString(new byte[] { 48, 97, 70, 48 })];
                    expected = new BitString(new byte[] { 10, 240 });
                    break;
                default:
                    arguments = [Term.List(new BitString(new byte[] { 1 }), new BitString(new byte[] { 2 })), new BitString(new byte[] { 0 })];
                    expected = new BitString(new byte[] { 1, 0, 2 });
                    break;
            }
            Term actual = await runtime.Modules.Call(
                context,
                BinaryModuleNames.Module,
                function,
                arguments
            );
            if (!actual.Equals(expected))
                throw new InvalidOperationException(BinaryWrappersHostNames.Failure);

            return Nil.Value;
        });
    }
}
