internal static class NonTailRecursionManagedNames
{
    internal const int RequiredArguments = 4;
    internal const int RetainedArguments = 5;
    internal const int ExpectedNewCases = 8;
    internal const string Usage = "report.json approved-freeze.json approved-sha256 fixture-suffix [exact-original328.erl]";
    internal const string RetainedSelector = "original-retained";
    internal const string RetainedCase = "undefined-lambda-no-current-original-fun-environment-arguments-self";
    internal const string RetainedSha256 = "87C6025BECE5C525D430257D10EF1308EA4B623AA53EF89355E83CFB7EB01BF5";
    internal const string FreezeChanged = "Approved source freeze changed";
    internal const string InvalidFixture = "Select exactly one of the eight bounded recursion fixtures or original-retained";
    internal const string OriginalSourceChanged = "Retained original328 source differs from the actual fatal first fixture";
}
