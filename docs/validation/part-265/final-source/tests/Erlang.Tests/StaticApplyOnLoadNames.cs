namespace Erlang.Tests;

internal static class StaticApplyOnLoadNames
{
    internal const string Unqualified = "unqualified-old-native";
    internal const string Qualified = "qualified-old-native";
    internal const string Dynamic = "dynamic260";
    internal const string EffectOnce = "effectonce";
    internal const string ListVariable = "listvariable";
    internal const string Improper = "improper";
    internal const string ItemThrow = "itemthrow";
    internal const string LocalShadow = "localshadow";
    internal const string Test = "host/static-known-apply-onload-evaluation-and-routing";
    internal const string Failure = "Static apply on_load contract failed";
    internal const string ItemEvaluated = "item_evaluated";
    internal const string ItemFailed = "item_failed";
    internal const string Local = "local";
    internal const string ObserverSource = "-module(loader_candidate_direct). -export([direct/1,dynamic/3]). direct(M)->erlang:apply(erlang,call_on_load_function,[M]). dynamic(Mod,Fun,Args)->erlang:apply(Mod,Fun,Args).";
    internal static readonly IReadOnlyDictionary<string, string> Cases = new Dictionary<string, string>
    {
        [Unqualified] = "-module(static_apply_contract). -export([run/1]). run(M)->apply(erlang,call_on_load_function,[M]).",
        [Qualified] = "-module(static_apply_contract). -export([run/1]). run(M)->erlang:apply(erlang,call_on_load_function,[M]).",
        [Dynamic] = "-module(static_apply_contract). -export([run/1,invoke/3]). invoke(M,F,A)->erlang:apply(M,F,A). run(M)->invoke(erlang,call_on_load_function,[M]).",
        [EffectOnce] = "-module(static_apply_contract). -export([run/1]). run(M)->erlang:apply(erlang,call_on_load_function,[begin self() ! item_evaluated,M end]).",
        [ListVariable] = "-module(static_apply_contract). -export([run/1]). run(M)->A=[M],erlang:apply(erlang,call_on_load_function,A).",
        [Improper] = "-module(static_apply_contract). -export([run/1]). run(M)->erlang:apply(erlang,call_on_load_function,[M|tail]).",
        [ItemThrow] = "-module(static_apply_contract). -export([run/1]). run(M)->erlang:apply(erlang,call_on_load_function,[begin erlang:error(item_failed),M end]).",
        [LocalShadow] = "-module(static_apply_contract). -export([run/1]). apply(A,B,C)->{local,A,B,C}. run(M)->apply(erlang,call_on_load_function,[M])."
    };
}
