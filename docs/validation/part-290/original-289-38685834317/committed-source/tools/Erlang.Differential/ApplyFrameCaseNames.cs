namespace Erlang.Differential;

internal static class ApplyFrameCaseNames
{
    internal const string Prefix = "erlang-apply-forwarded-frame-";
    internal const string ModulePrefix = "-module(oracle_apply_frame). -export([run/0,invoke/3]). run()->";
    internal const string Helpers = " invoke(M,F,A)->try apply(M,F,A) of V->{unexpected_success,V} catch C:R:S->case {M,F} of {erlang,raise}->{C,R,S};_->project(C,R,S) end end. project(C,R,[{M,F,A,_Annotations}|_])->{C,R,{M,F,A}}.";
    internal const string Error = "error";
    internal const string BadArgument = "badarg";
    internal const string Undefined = "undef";
    internal const string Erlang = "erlang";
    internal const string Apply = "apply";
    internal const string Head = "hd";
    internal const string NonList = "not_a_list";
    internal const string MissingModule = "missing_apply_frame_module";
    internal const string MissingFunction = "absent";
    internal const string TraceModule = "synthetic_module";
    internal const string TraceFunction = "synthetic_function";
    internal const string Reason = "reason";
    internal const string Throw = "throw";
    internal const string Exit = "exit";
}
