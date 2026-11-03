-module(dictionary_keys605_capture631).
-export([run/0]).
run() ->
    try dictionary_keys605:run() of Value -> {ok,Value}
    catch Class:Reason:Stack -> {Class,Reason,Stack}
    end.
