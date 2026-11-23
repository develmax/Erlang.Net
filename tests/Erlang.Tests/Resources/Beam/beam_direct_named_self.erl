-module(beam_direct_named_self).
-export([direct/1, captured/2, outer_shadow/2, nested_shadow/1,
         protected/1, self_escape/1, nested_capture/1, dynamic/1, wrong_arity/0]).

direct(Input) ->
    F = fun Loop([]) -> done;
            Loop([_|Tail]) -> Loop(Tail)
        end,
    F(Input).

captured(Input, Value) ->
    F = fun Loop([]) -> [];
            Loop([_|Tail]) -> [Value | Loop(Tail)]
        end,
    F(Input).

outer_shadow(Input, Loop) ->
    F = fun Loop([]) -> done;
            Loop([_|Tail]) -> Loop(Tail)
        end,
    {Loop, F(Input)}.

nested_shadow(Input) ->
    F = fun Loop({shadow, Local}) ->
                Apply = fun(Loop) -> Loop(value) end,
                Apply(Local);
            Loop([]) -> done;
            Loop([_|Tail]) -> Loop(Tail)
        end,
    Local = fun(Payload) -> {Input, Payload} end,
    F({shadow, Local}).

protected(Input) ->
    F = fun Loop([]) -> done;
            Loop([signal|_]) -> throw(signal);
            Loop([_|Tail]) ->
                try Loop(Tail) of
                    Result -> {ok, Result}
                catch
                    throw:signal -> recovered
                end
        end,
    F(Input).

self_escape(Input) ->
    F = fun Loop([]) -> Loop;
            Loop([_|Tail]) -> Loop(Tail)
        end,
    Value = F(Input),
    is_function(Value, 1).

nested_capture(Input) ->
    F = fun Loop(_) ->
                fun() -> Loop end
        end,
    ReadSelf = F(Input),
    is_function(ReadSelf(), 1).

dynamic(Arguments) ->
    F = fun Loop([]) -> done;
            Loop({invoke, Args}) -> erlang:apply(Loop, Args)
        end,
    F({invoke, Arguments}).

wrong_arity() ->
    F = fun Loop(ready) ->
                try Loop() of
                    _ -> unexpected
                catch
                    error:{badarity, _} -> caught
                end
        end,
    F(ready).
