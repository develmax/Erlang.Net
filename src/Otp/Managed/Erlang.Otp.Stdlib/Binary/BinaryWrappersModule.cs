namespace Erlang;

public static class BinaryWrappersModule
{
    public static void Register(ModuleRegistry registry)
    {
        registry.RegisterCanonicalRuntimeRead535(
            BinaryModuleNames.Module,
            BinaryModuleNames.Replace,
            3,
            (context, a) => BinaryReplaceExecution.Run(
                context,
                a[0],
                a[1],
                a[2],
                Nil.Value,
                true
            )
        );
        registry.RegisterCanonicalRuntimeRead535(
            BinaryModuleNames.Module,
            BinaryModuleNames.Replace,
            4,
            (context, a) => BinaryReplaceExecution.Run(
                context,
                a[0],
                a[1],
                a[2],
                a[3],
                false
            )
        );
        registry.Register(
            BinaryModuleNames.Module,
            BinaryModuleNames.EncodeHex,
            1,
            (context, a) => BinaryHexExecution.Encode(
                context,
                a[0],
                Term.A(BinaryWrapperNames.Uppercase),
                true
            )
        );
        registry.Register(
            BinaryModuleNames.Module,
            BinaryModuleNames.EncodeHex,
            2,
            (context, a) => BinaryHexExecution.Encode(
                context,
                a[0],
                a[1],
                false
            )
        );
        registry.Register(
            BinaryModuleNames.Module,
            BinaryModuleNames.DecodeHex,
            1,
            (context, a) => BinaryHexExecution.Decode(context, a[0])
        );
        registry.Register(
            BinaryModuleNames.Module,
            BinaryModuleNames.Join,
            2,
            (context, a) => BinaryJoinExecution.Run(context, a[0], a[1])
        );
    }
}
