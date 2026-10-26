-module(beam_application_driver379).
-export([run/0]).

run() ->
    put(application_resource_phase, request),
    try
        [RequestPath] = init:get_plain_arguments(),
        true = filename:pathtype(RequestPath) =:= absolute,
        {ok, RequestBytes} = file:read_file(RequestPath),
        {{application_resource_request, DescriptorPath, Configs, Output}, Used} =
            binary_to_term(RequestBytes, [safe, used]),
        true = Used =:= byte_size(RequestBytes),
        true = text(Output),
        true = filename:pathtype(Output) =:= absolute,
        {ok, Root} = file:get_cwd(),
        false = lists:prefix(filename:split(filename:absname(Root)),
                             filename:split(filename:absname(Output))),
        put(application_resource_output, Output),
        relative(DescriptorPath),
        true = is_list(Configs),
        lists:foreach(fun({Logical, Owned}) -> relative(Logical), relative(Owned) end,
                      Configs),
        put(application_resource_phase, descriptor),
        Descriptor = consult_one(DescriptorPath),
        ParsedConfigs = [begin
                             put(application_resource_phase, {config, Logical}),
                             {Logical, consult_one(Owned)}
                         end || {Logical, Owned} <- Configs],
        Result = {application_resources_parsed, Descriptor, ParsedConfigs},
        finish(Result, 0)
    catch
        Class:Reason:Stack ->
            Failure = {application_resources_failed,
                       get(application_resource_phase), Class, Reason, Stack},
            case get(application_resource_output) of
                undefined -> io:format(standard_error, "~tp~n", [Failure]), halt(1);
                _ -> finish(Failure, 1)
            end
    end.

consult_one(Path) ->
    case file:consult(Path) of
        {ok, [Term]} -> Term;
        {ok, Terms} -> error({expected_one_resource_term, Path, length(Terms)});
        {error, Reason} -> error({resource_consult_failed, Path, Reason})
    end.

relative(Path) ->
    true = text(Path),
    true = filename:pathtype(Path) =:= relative,
    false = lists:any(fun(C) -> C =:= $\\ orelse C =:= $: end, Path),
    Segments = string:split(Path, "/", all),
    true = lists:all(fun(S) -> S =/= [] andalso S =/= "." andalso S =/= ".." end,
                     Segments),
    ok.

text(Value) when is_list(Value), Value =/= [] ->
    lists:all(fun(C) -> is_integer(C) andalso C > 0 andalso C =< 16#10ffff
                       andalso not (C >= 16#d800 andalso C =< 16#dfff) end, Value);
text(_) -> false.

finish(Result, ExitCode) ->
    Bytes = term_to_binary(Result, [{minor_version, 2}]),
    case file:write_file(get(application_resource_output), Bytes, [exclusive]) of
        ok -> halt(ExitCode);
        {error, Reason} ->
            io:format(standard_error, "~tp~n", [{resource_output_failed, Reason}]),
            halt(1)
    end.
