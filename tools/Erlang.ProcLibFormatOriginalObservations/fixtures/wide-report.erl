-module(oracle_proc_format).
-export([run/0]).
run() ->
    try proc_lib:format([[{messages,[{a,lists:seq(1,40)},{b,lists:seq(41,80)}]}],[]]) of
        Text -> {text, lists:flatten(Text)}
    catch
        Class:Reason -> {exception, Class, Reason}
    end.
