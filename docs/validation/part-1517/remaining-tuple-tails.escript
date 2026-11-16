#!/usr/bin/env escript
-mode(compile).
main([Root, Candidate, Paths]) ->
    {ok, Data} = file:read_file(Paths),
    Rows = lists:flatmap(fun(Path) -> compare(Root, Candidate, binary_to_list(Path)) end,
                         [P || P <- binary:split(Data, <<10>>, [global]), P =/= <<>>]),
    io:format("~B modules, ~B functions with a larger frame and a tuple match before a tail call~n",
              [length(lists:usort([Path || {Path, _, _, _, _} <- Rows])), length(Rows)]),
    Kinds = lists:usort(lists:append([Shapes || {_, _, _, _, Shapes} <- Rows])),
    [begin
         Matches = [P || {P, _, _, _, Shapes} <- Rows, lists:member(Kind, Shapes)],
         io:format("~0p: ~B functions in ~B modules~n", [Kind, length(Matches), length(lists:usort(Matches))])
     end || Kind <- Kinds],
    [io:format("~0p~n", [Row]) || Row <- Rows].

compare(Root, Candidate, Path) ->
    Release = filename:join(Root, Path),
    case beam_lib:chunks(Release, [abstract_code]) of
        {ok, {_, [{abstract_code, {raw_abstract_v1, Forms}}]}} ->
            Targets = [{{Name, Arity}, Shapes} || {function, _, Name, Arity, Clauses} <- Forms,
                         Shapes <- [lists:usort([clause_shape(Clause) || Clause <- Clauses, tuple_tail(Clause)])],
                         Shapes =/= []],
            compare_frames(Targets, Release, filename:join(Candidate, Path), Path);
        _ -> []
    end.

tuple_tail({clause, _, _Inputs, _, [{match, _, {tuple, _, Fields}, {call, _, _, _}},
                                   {call, _, Target, Args}]}) ->
    Fields =/= [] andalso static_target(Target) andalso
        lists:all(fun({var, _, _}) -> true; (_) -> false end, Fields) andalso
        lists:any(fun({var, _, Name}) -> lists:keymember(Name, 3, Fields); (_) -> false end, Args) andalso
        lists:all(fun({var, _, Name}) -> lists:keymember(Name, 3, Fields);
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
    [{Path, Target, R, C, Shapes} || {Target, Shapes} <- Targets,
      R <- [maps:get(Target, Ref, 0)], C <- [maps:get(Target, Actual, 0)], C > R].

clause_shape({clause, _, Inputs, Guards, [{match, _, _, {call, _, _, Args}}, _]}) ->
    Patterns = lists:usort(lists:append([entry_shapes(P) || P <- Inputs])),
    Entry = case Patterns of [] -> variables; _ -> Patterns end,
    Guard = case Guards of [] -> unguarded; _ -> guarded end,
    Operands = case lists:all(fun simple/1, Args) of true -> simple; false -> computed end,
    {Entry, Guard, Operands}.

entry_shapes({match, _, Left, Right}) -> entry_shapes(Left) ++ entry_shapes(Right);
entry_shapes({bin, _, _}) -> [binary];
entry_shapes({cons, _, _, _}) -> [list];
entry_shapes({nil, _}) -> [list];
entry_shapes({tuple, _, _}) -> [tuple];
entry_shapes({record, _, _, _}) -> [tuple];
entry_shapes({var, _, _}) -> [];
entry_shapes(_) -> [literal].

simple({var, _, _}) -> true;
simple(Arg) -> erl_syntax:is_literal(Arg).

frames(Path) ->
    {beam_file, _, _, _, _, Fs} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, lists:max([0 | [Size || {allocate, Size, _} <- Is] ++
                          [Size || {allocate_heap, Size, _, _} <- Is]])} ||
                       {function, Name, Arity, _, Is} <- Fs]).


