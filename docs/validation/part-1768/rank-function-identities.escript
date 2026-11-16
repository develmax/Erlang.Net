-mode(compile).

main([Release, Generated]) ->
    Paths = filelib:wildcard("**/*.beam", Generated),
    Counts = lists:foldl(fun(Path, Acc0) ->
        Ref = functions(filename:join(Release, Path)),
        Actual = functions(filename:join(Generated, Path)),
        Extra = ordsets:subtract(Actual, Ref),
        Missing = ordsets:subtract(Ref, Actual),
        lists:foldl(fun(Signature, Acc) ->
            Group = classify(Signature, Extra),
            maps:update_with(Group, fun({Files, N}) ->
                {Files#{Path => true}, N + 1}
            end, {#{Path => true}, 1}, Acc)
        end, Acc0, Missing)
    end, #{}, Paths),
    Ranked = lists:reverse(lists:sort([{map_size(Files), Missing, Group} ||
                                      {Group, {Files, Missing}} <- maps:to_list(Counts)])),
    io:format("Release function identities absent from the generated image. Rows can overlap by module.~n"),
    [io:format("~B modules ~B identities ~0p~n", [Files, Missing, Group]) ||
        {Files, Missing, Group} <- Ranked],
    ok.

classify({Name, Arity}, Extra) ->
    case family(Name) of
        other ->
            case lists:keymember(Name, 1, Extra) of
                true -> other_name_present_with_other_arity;
                false -> other_name_absent
            end;
        {Parent, Kind} ->
            case lists:keymember(Name, 1, Extra) of
                true -> {Kind, same_name_other_arity};
                false ->
                    Siblings = [{Other, A} || {Other, A} <- Extra, family(Other) =:= {Parent, Kind}],
                    Relation = case lists:keymember(Arity, 2, Siblings) of
                        true -> same_parent_same_arity_other_name;
                        false when Siblings =/= [] -> same_parent_other_name_and_arity;
                        false -> no_extra_sibling
                    end,
                    {Kind, Relation}
            end
    end.

family(Name) ->
    case re:run(atom_to_list(Name), "^(.*)-(fun|anonymous|inlined|bc|lc|mc|lbc|mbc)(?:[$].[0-9]+/[0-9]+)?-[0-9]+-$", [{capture, [1, 2], binary}]) of
        {match, [Parent, Kind]} -> {Parent, Kind};
        nomatch -> other
    end.

functions(Path) ->
    {beam_file, _, _, _, _, Code} = beam_disasm:file(Path),
    ordsets:from_list([{Name, Arity} || {function, Name, Arity, _, _} <- Code]).