namespace Erlang.Differential;

internal static class DistributionInputCaseNames
{
    internal const string Prefix = "erlang-dist-ctrl-put-data-";
    internal const string ModulePrefix = "-module(oracle_dist_input). -export([run/0]). -compile({no_inline,[invoke/4]}). run()->";
    internal const string Module = "erlang";
    internal const string Function = "dist_ctrl_put_data";
    internal const string InvalidHandle = "invalid_distribution_handle";
    internal const string OrdinaryReference = "ordinary_reference";
    internal const string ErrorClass = "error";
    internal const string BadArgument = "badarg";
    internal const string DirectCall = "erlang:dist_ctrl_put_data(H,D)";
    internal const string DynamicCall = "apply(M,F,[H,D])";
    internal const string ReferenceProjection = "[H,D]=Args,{C,R,{M1,F1,[ordinary_reference,D]}}";
    internal const string ExactProjection = "[H,D]=Args,{C,R,{M1,F1,Args}}";
}
