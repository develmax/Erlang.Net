-module(oracle_proc_format).
-export([run/0]).
run() ->
    try proc_lib:format([[{dictionary,[{a,{b,{c,d}}},{text,"long enough printable text"}]}],[]],latin1,2) of
        Text -> {text, lists:flatten(Text)}
    catch
        Class:Reason -> {exception, Class, Reason}
    end.
