namespace Erlang;

internal static class NativeRecordHeapLayout
{
    internal const int HeaderWords = 1;
    internal const int DefinitionPointerWords = 1;
    internal const int DefinitionHeaderWords = 1;
    internal const int DefinitionOrder = 0;
    internal const int DefinitionHash = 1;
    internal const int DefinitionModule = 2;
    internal const int DefinitionName = 3;
    internal const int DefinitionExported = 4;
    internal const int DefinitionKeys = 5;
    internal const int HeaderArityShift = 6;
    internal const nuint HeaderTag = 0x1C;
    internal const nuint BoxedPointerTag = 2;
    internal const string Invalid = "Invalid native record physical storage";
    internal const string MissingPlan = "Missing native record physical preparation";
    internal const string InvalidMetadata = "Invalid generated native record definition metadata";

    internal static nuint Header(int fields)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fields);

        return checked((nuint)(fields + DefinitionPointerWords) << HeaderArityShift) | HeaderTag;
    }
}
