#!/usr/bin/env escript
-mode(compile).
main([Root, Candidate, Paths]) ->
    {ok, Data} = file:read_file(Paths),
    Rows = lists:flatmap(fun(Path) -> compare(Root, Candidate, binary_to_list(Path)) end,
                         [P || P <- binary:split(Data, <<10>>, [global]), P =/= <<>>]),
    io:format("~B modules, ~B functions with an oversized frame after a tuple call followed by a tail call~n",
              [length(lists:usort([Path || {Path, _, _, _} <- Rows])), length(Rows)]),
    [io:format("~0p~n", [Row]) || Row <- Rows].

compare(Root, Candidate, Path) ->
    Release = filename:join(Root, Path),
    case beam_lib:chunks(Release, [abstract_code]) of
        {ok, {_, [{abstract_code, {raw_abstract_v1, Forms}}]}} ->
            Targets = [{Name, Arity} || {function, _, Name, Arity, Clauses} <- Forms,
                                        lists:any(fun tuple_tail/1, Clauses)],
            compare_frames(Targets, Release, filename:join(Candidate, Path), Path);
        _ -> []
    end.

tuple_tail({clause, _, Inputs, _, [{match, _, {tuple, _, Fields}, {call, _, _, _}},
                                   {call, _, Target, Args}]}) ->
    Fields =/= [] andalso static_target(Target) andalso
        lists:all(fun({var, _, _}) -> true; (_) -> false end, Fields) andalso
        lists:any(fun({var, _, Name}) -> lists:keymember(Name, 3, Fields); (_) -> false end, Args) andalso
        lists:all(fun({var, _, Name}) -> lists:keymember(Name, 3, Fields) orelse lists:keymember(Name, 3, Inputs);
                     (Arg) -> erl_syntax:is_literal(Arg)
                  end, Args);
tuple_tail(_) -> false.

static_target({atom, _, _}) -> true;
static_target({remote, _, {atom, _, _}, {atom, _, _}}) -> true;
static_target(_) -> false.

compare_frames([], _, _, _) -> [];
compare_frames(Targets, Release, Candidate, Path) ->
    Ref = frames(Release),
    Actual = frames(Candidate),
    [{Path, Target, R, C} || Target <- Targets,
      R <- [maps:get(Target, Ref, 0)], C <- [maps:get(Target, Actual, 0)], C > R].

frames(Path) ->
    {beam_file, _, _, _, _, Fs} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, lists:max([0 | [Size || {allocate, Size, _} <- Is] ++
                          [Size || {allocate_heap, Size, _, _} <- Is]])} ||
                       {function, Name, Arity, _, Is} <- Fs]).
