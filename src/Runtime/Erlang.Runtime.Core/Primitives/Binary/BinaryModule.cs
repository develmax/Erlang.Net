namespace Erlang;

public static class BinaryModule
{
    public static void Register(ModuleRegistry registry)
    {
        void Add(string name, int arity, ErlangFunction body) => registry.Register(
            BinaryModuleNames.Module,
            name,
            arity,
            body
        );

        void Read577(string name, int arity, ErlangFunction body) => registry.RegisterCanonicalRuntimeRead535(
            BinaryModuleNames.Module,
            name,
            arity,
            body
        );
        Add(BinaryModuleNames.At, 2, (_, a) => ValueTask.FromResult(BinaryPrimitiveOperations.At(a[0], a[1])));
        Add(BinaryModuleNames.First, 1, (_, a) => ValueTask.FromResult(BinaryPrimitiveOperations.Edge(a[0], false)));
        Add(BinaryModuleNames.Last, 1, (_, a) => ValueTask.FromResult(BinaryPrimitiveOperations.Edge(a[0], true)));
        Read577(BinaryModuleNames.Part, 2, (_, a) => ValueTask.FromResult(BinaryPrimitiveOperations.Part(a[0], a[1])));
        Read577(BinaryModuleNames.Part, 3, (_, a) => ValueTask.FromResult(ErlangIoData.Part(a[0], a[1], a[2])));
        foreach (int arity in new[] { 1, 2, 3 })
            Add(BinaryModuleNames.BinToList, arity, BinaryListConversion.ToList);
        Add(BinaryModuleNames.ListToBin, 1, (c, a) => BinaryListConversion.FromList(c, a[0]));
        Add(BinaryModuleNames.Copy, 1, (c, a) => BinaryPrimitiveOperations.Copy(c, a[0], Term.I(1)));
        Add(BinaryModuleNames.Copy, 2, (c, a) => BinaryPrimitiveOperations.Copy(c, a[0], a[1]));
        Add(BinaryModuleNames.DecodeUnsigned, 1, (c, a) => BinaryUnsigned.Decode(c, a[0], Term.A(BinaryPrimitiveParameters.BigEndian)));
        Add(BinaryModuleNames.DecodeUnsigned, 2, (c, a) => BinaryUnsigned.Decode(c, a[0], a[1]));
        Add(BinaryModuleNames.EncodeUnsigned, 1, (c, a) => BinaryUnsigned.Encode(c, a[0], Term.A(BinaryPrimitiveParameters.BigEndian)));
        Add(BinaryModuleNames.EncodeUnsigned, 2, (c, a) => BinaryUnsigned.Encode(c, a[0], a[1]));
        Add(BinaryModuleNames.LongestCommonPrefix, 1, (c, a) => BinaryPrimitiveOperations.Common(c, a[0], false));
        Add(BinaryModuleNames.LongestCommonSuffix, 1, (c, a) => BinaryPrimitiveOperations.Common(c, a[0], true));
        registry.RegisterCanonicalRuntimeRead535(
            BinaryModuleNames.Module,
            BinaryModuleNames.ReferencedByteSize,
            1,
            (_, a) => ValueTask.FromResult<Term>(Term.I(ErlangIoData.Bits(a[0]).ReferencedByteSize))
        );
        BinarySearchModule.Register(registry);
        RuntimeManagedLibraries456.Register(registry, RuntimeManagedLibraryStage456.BinaryWrappers);
    }
}
