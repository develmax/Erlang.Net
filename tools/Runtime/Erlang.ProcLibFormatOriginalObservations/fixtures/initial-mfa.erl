-module(oracle_proc_format).
-export([run/0]).
run() ->
    try proc_lib:format([[{initial_call,{alpha,beta,[one,two]}}],[]]) of
        Text -> {text, lists:flatten(Text)}
    catch
        Class:Reason -> {exception, Class, Reason}
    end.
