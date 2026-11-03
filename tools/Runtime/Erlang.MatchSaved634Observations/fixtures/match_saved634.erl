-module(match_saved634).
-export([run/0]).

run() ->
    Values = [[], saved_constant634, 123, 1.25, 1 bsl 70, -(1 bsl 77),
              {7,[]}, [3,4], [9|11], {1 bsl 70,-2.5,[9|11]}],
    try
        Results = lists:map(
            fun(Value) ->
                Program = ets:match_spec_compile([{'_',[],[{const,Value}]}]),
                [Value] = ets:match_spec_run([ignored], Program),
                {const,Value}
            end,
            Values),
        {ok,Results}
    catch Class:Reason:Stack -> {Class,Reason,Stack}
    end.
