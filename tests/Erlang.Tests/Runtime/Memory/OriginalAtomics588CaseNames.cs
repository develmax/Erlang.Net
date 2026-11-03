namespace Erlang.Tests;

internal static class OriginalAtomics588CaseNames
{
    internal const string Normal = "normal";
    internal const string Returned = "returned";
    internal const string Raised = "raised";
    internal const string WordSize = "wordsize";
    internal const string AtomicsAllSeven = "atomics_all_seven";
    internal const string ConstructorErrors = "constructor_errors";
    internal const string NativeErrors = "native_errors";
    internal const string CountersDefaultAndAtomics = "counters_original_default_and_atomics";
    internal const string Zero = "zero";
    internal const string Negative = "negative";
    internal const string WrongSize = "wrong_size";
    internal const string WrongOptions = "wrong_options";
    internal const string ImproperOptions = "improper_options";
    internal const string WrongSigned = "wrong_signed";
    internal const string UnknownOption = "unknown_option";
    internal const string GetIndexZero = "get_index_zero";
    internal const string PutIndexPastEnd = "put_index_past_end";
    internal const string AddWrongIncrement = "add_wrong_increment";
    internal const string AddGetWrongIndex = "add_get_wrong_index";
    internal const string ExchangeWrongValue = "exchange_wrong_value";
    internal const string CompareWrongValue = "compare_wrong_value";
    internal const string InfoWrongReference = "info_wrong_reference";
    internal const string SyntheticStack = "synthetic_stack_shape_only";
    internal const string AtomicsOption = "atomics";
    internal const int ExpectedAtomicsRows = 14;
    internal const int DefaultAndAtomicsRows = 2;
    internal const int FixtureWordBytes = 8;
    internal const int FailedFixtureLength = 1;
    internal static int[] ValidLengths => [1, 2, 7, 8, 9, 32, 33];
}
