namespace Erlang.Differential;

public static class ErrorLoggerTermLimitCaseData
{
    public static IReadOnlyList<(string Name, string Source)> Rows { get; } =
    [
        ("limit-small-map-values", "-module(oracle_limit_small_map).\n-export([run/0]).\nrun()->\n    ok=application:set_env(kernel,error_logger_format_depth,10),\n    Value=#{first=>{second,[third,fourth]}},\n    try Value=error_logger:limit_term(Value),ok\n    after application:unset_env(kernel,error_logger_format_depth) end.\n"),
        ("limit-large-own-iterator", "-module(oracle_limit_large_map).\n-export([run/0]).\nrun()->\n    ok=application:set_env(kernel,error_logger_format_depth,10),\n    Value=maps:from_list([{N,{keep,N}}||N<-lists:seq(1,64)]),\n    Expected=maps:from_list(first(11,maps:iterator(Value))),\n    try Expected=error_logger:limit_term(Value),ok\n    after application:unset_env(kernel,error_logger_format_depth) end.\nfirst(0,_)->[];\nfirst(N,Iterator)->\n    case maps:next(Iterator) of\n        none->[];\n        {Key,Value,Next}->[{Key,Value}|first(N-1,Next)]\n    end.\n")
    ];
}
