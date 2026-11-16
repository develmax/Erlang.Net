#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Parts = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Groups = lists:foldl(fun(Path, Acc) ->
        Relative = filename:join(lists:nthtail(length(Parts), filename:split(filename:absname(Path)))),
        Ref = functions(filename:join(Reference, Relative)),
        Actual = functions(Path),
        maps:fold(fun(Key, Body, G0) ->
            Expected = x_tests(maps:get(Key, Ref, [])),
            Kinds = lists:usort(spill_sources(Body, Expected)),
            lists:foldl(fun(Kind, G) ->
                maps:update_with(Kind, fun(Pairs) -> [{Relative, Key}|Pairs] end, [{Relative, Key}], G)
            end, G0, Kinds)
        end, Acc, Actual)
    end, #{}, Files),
    Rows = maps:fold(fun(Kind, Pairs, Acc) ->
        Count = length(lists:usort([Path || {Path, _} <- Pairs])),
        [{Count, length(Pairs), Kind, lists:sublist(lists:sort(Pairs), 5)}|Acc]
    end, [], Groups),
    lists:foreach(fun({Images, Functions, Kind, Examples}) ->
        io:format("~p: ~p images, ~p functions.~n", [Kind, Images, Functions]),
        lists:foreach(fun(Example) -> io:format("~0p~n", [Example]) end, Examples)
    end, lists:reverse(lists:sort(Rows))).

functions(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, Body} || {function, Name, Arity, _, Body} <- Functions]).

x_tests(Body) ->
    [{Arity, Tag} || {test, is_tagged_tuple, _, [{x, _}, Arity, {atom, Tag}]} <- Body].

spill_sources([Previous, {move, {x, _} = Source, {y, _} = Register},
               {test, is_tagged_tuple, _, [Register, Arity, {atom, Tag}]} | Rest], Expected) ->
    More = spill_sources(Rest, Expected),
    case lists:member({Arity, Tag}, Expected) of
        true -> [source(Previous, Source)|More];
        false -> More
    end;
spill_sources([_|Rest], Expected) ->
    spill_sources(Rest, Expected);
spill_sources([], _) ->
    [].

source({call, _, _}, {x, 0}) -> local_call;
source({call_ext, _, _}, {x, 0}) -> remote_call;
source({bif, Name, _, _, Register}, Register) -> {bif, Name};
source({gc_bif, Name, _, _, _, Register}, Register) -> {gc_bif, Name};
source({get_tuple_element, _, _, Register}, Register) -> tuple_field;
source({label, _}, _) -> branch_entry;
source(Instruction, _) when is_tuple(Instruction) -> element(1, Instruction);
source(Instruction, _) -> Instruction.
