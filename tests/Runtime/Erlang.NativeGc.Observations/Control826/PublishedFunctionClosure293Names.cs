namespace Erlang.Tests;

internal static class PublishedFunctionClosure293Names
{
    internal const string Failure293 = "Published function closure proof or real promotion differs.";
    internal const string IdentityModule293 = "function_closure293";
    internal const string UseProof293 = "CanUseImmutableClosure293";
    internal const string PrepareImport293 = "PrepareImport";
    internal const string FunctionStorage293 = "OwnedFunctionStorage";
    internal const string Published293 = "Published";
    internal const string NativePrepare293 = "PrepareNativeHeaders524";
    internal const string NativeCommit293 = "Commit524";
    internal const string LiteralOnly293 = "HasImmutableLiteralOnlyClosure132";
    internal const string Readonly293 = "HasImmutableReadonlyCaptureClosure138";
    internal const string EmptyCapture293 = "HasEmptyLiteralCapture200";
    internal const string Entries293 = "functionEntries";
    internal const string Roots293 = "roots";
    internal const string Custody293 = "constructionCustody514";
    internal const string Publish293 = "Publish";
    internal const string CreateCons293 = "CreateCons";
    internal const string Value293 = "Value";
    internal const string Gate293 = "Gate";
    internal const string Identity293 = "Identity";
    internal const string LiteralSidecar293 = "RetainCertifiedLiteralBlock134";
    internal const int Small293 = 64;
    internal const int Large293 = 256;
    internal const string FallbackModule464 = "function_fallback464";
    internal const string FallbackKey464 = "function_fallback464_root";
    internal const string KeepGlobal464 = "keep_global";
    internal const string KeepLiteral464 = "keep_literal";
    internal const string CallGlobal464 = "call_global";
    internal const string CallLiteral464 = "call_literal";
    internal const string LiteralAtom464 = "literal464";
    internal const string Abs464 = "abs";
    internal const string FallbackSource464 = """
        -module(function_fallback464).
        -export([keep_global/0,keep_literal/0,call_global/0,call_literal/0]).
        wrap(X)->fun()->X end.
        keep_global()->F=fun erlang:abs/1,G=wrap(F),put(function_fallback464_root,wrap(G)),ok.
        call_global()->G=get(function_fallback464_root),F=G(),A=F(),A(-464).
        keep_literal()->L={literal464,[17,23]},F=wrap(L),put(function_fallback464_root,wrap(F)),ok.
        call_literal()->G=get(function_fallback464_root),F=G(),F().
        """;
}
