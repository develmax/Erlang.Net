-module(beam_constant_tail_chain).
-export([run/1, keep/1]).
run(Input) when is_atom(Input) ->
    step(Input, first),
    step(Input, second),
    done.
keep(Input) when is_atom(Input) ->
    step(Input, first),
    different(Input),
    done.
step(Input, Phase) ->
    case {Input, Phase} of
        {fail, second} -> throw(second_call);
        _ -> ok
    end,
    done.
different(value) -> other;
different(_) -> done.
