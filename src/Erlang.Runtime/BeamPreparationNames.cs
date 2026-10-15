namespace Erlang;

internal static class BeamPreparationNames
{
    internal const string AttributesChunk = "Attr";
    internal const string CompilationChunk = "CInf";
    internal const string RecordsChunk = "Recs";
    internal const string ModuleInfo = "module_info";
    internal const string GetModuleInfo = "get_module_info";
    internal const string InvalidMetadata = "Invalid BEAM module metadata";
    internal const string UnsupportedRecords = "BEAM publication of native record definitions is not supported";
    internal const string ForeignImage = "Prepared BEAM image belongs to another module registry";
    internal const string ConsumedImage = "Prepared BEAM image has already been published";
    internal const int ModuleRegister = 0;
    internal const int KeyRegister = 1;
    internal const int MetadataImportExtraArguments = 1;
    internal const int MetadataZeroBodyInstructions = 2;
    internal const int MetadataKeyBodyInstructions = 3;
}
