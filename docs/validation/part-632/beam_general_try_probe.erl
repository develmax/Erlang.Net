-module(beam_general_try_probe).
        -export([outer/1]).
        run(Value) ->
            try {ok, element(1, make(Value))}
            catch error:Reason:Stack -> {caught,Reason,is_list(Stack)} end.
        outer(Value) ->
            try run(Value)
            catch Class:Reason:Stack -> {Class,Reason,is_list(Stack)} end.
        make(ok) -> {yes};
        make(bad) -> erlang:error(bad);
        make(exit) -> exit(stop);
        make(throw) -> throw(ball).