using Erlang;

namespace Erlang.Tests;

internal sealed record LibraryDependencyOriginalSuite(
    string Prefix,
    string Observer,
    string[] Fixtures,
    string[] AdditionalLibraries,
    int Rows,
    int ProfileArity,
    RuntimeWarningMapping? WarningMapping = null,
    RuntimePrintableRange? PrintableRange = null
);
