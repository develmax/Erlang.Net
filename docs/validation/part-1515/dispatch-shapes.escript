#!/usr/bin/env escript
-mode(compile).
main([Root, Before, After, Baseline]) ->
    {ok, Text} = file:read_file(Baseline),
    Rows = [report(Root, Before, After, Line) || Line <- binary:split(Text, <<10>>, [global]),
        is_target(Line)],
    [io:format("~0p~n", [Row]) || Row <- Rows],
    Count = length([ok || {_, _, true, _, _, _} <- Rows]),
    io:format("~B of ~B previously identified functions now match normalized OTP instructions.~n",
              [Count, length(Rows)]).

is_target(<<"{same_type_reduced_keys,", _/binary>>) -> true;
is_target(<<"{reference_", _/binary>>) -> true;
is_target(_) -> false.

report(Root, Before, After, Line) ->
    {ok, Tokens, _} = erl_scan:string(string:trim(binary_to_list(Line)) ++ "."),
    {ok, {_, Path, Key, _, _}} = erl_parse:parse_term(Tokens),
    Expected = body(filename:join(Root, Path), Key),
    Old = body(image(Before, Root, Path), Key),
    New = body(image(After, Root, Path), Key),
    {Path, Key, normalize(Expected) =:= normalize(New), shape(Expected), shape(Old), shape(New)}.

shape(Body) ->
    #{select_tables => [length(Keys) div 2 || {select_val, _, _, {list, Keys}} <- Body],
      exact_tests => length([ok || {test, Op, _, _} <- Body, Op =:= is_eq_exact orelse Op =:= is_ne_exact])}.

image(Dir, Root, Path) ->
    Candidate = filename:join(Dir, Path),
    case filelib:is_regular(Candidate) of
        true -> Candidate;
        false -> filename:join(Root, Path)
    end.

body(Path, {Name, Arity}) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    [Code] = [Is || {function, N, A, _, Is} <- Functions, N =:= Name, A =:= Arity],
    Code.

normalize(Code) ->
    Labels = [L || {label, L} <- Code],
    Ids = maps:from_list(lists:zip(Labels, lists:seq(1, length(Labels)))),
    Fix = fun F({label, L}) -> {label, maps:get(L, Ids, L)};
              F({f, L}) -> {f, maps:get(L, Ids, L)};
              F({literal, _} = T) -> T;
              F({make_fun3, M, I, _, D, A}) -> {make_fun3, M, I, uniq, F(D), F(A)};
              F(T) when is_tuple(T) -> list_to_tuple(F(tuple_to_list(T)));
              F([H | T]) -> [F(H) | F(T)];
              F(T) -> T
          end,
    Fix(Code).
