-module(beam_call_inside_tail_list).
-export([render/2]).
render(Tag, Pid) when is_pid(Pid) ->
    lists:concat([Tag, " ", pid_to_list(Pid)]);
render(Tag, Atom) when is_atom(Atom) ->
    lists:concat([Tag, " ", Atom]);
render(Tag, Other) ->
    {Tag, Other}.
