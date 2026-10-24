namespace Erlang.Tests;

internal static class SourceLoadNifArgumentHostNames
{
    internal const string TwoEffects = "two_effects";
    internal const string NonTailEffects = "non_tail_effects";
    internal const string SecondFails = "second_fails";
    internal const string Order = "argument_order";
    internal const string First = "first";
    internal const string Second = "second";
    internal const string FirstEffect = "first_effect";
    internal const string FailureReason = "second_argument_failed";
    internal const int SuccessfulHandlerCalls = 2;
    internal const int GeneratedHashLength = 64;
    internal const string Source = "-module(source_load_nif_argument_host). -export([two_effects/0,non_tail_effects/0,second_fails/0]). two_effects()->erlang:load_nif(first(),second()). non_tail_effects()->{erlang:load_nif(first(),second())}. second_fails()->erlang:load_nif(first(),failed_second()). first()->erlang:put(first_effect,true),erlang:put(argument_order,first),17. second()->erlang:put(argument_order,{erlang:get(argument_order),second}),42. failed_second()->erlang:put(argument_order,{erlang:get(argument_order),second}),erlang:error(second_argument_failed).";
}
