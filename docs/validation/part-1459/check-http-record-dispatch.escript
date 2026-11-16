#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    {Short, Full, [Failure|_]} = Expected = run(Reference),
    {Short, Full, [Failure|_]} = Actual = run(Generated),
    io:format("OTP ~0p~nC# ~0p~n", [Expected, Actual]),
    io:format("HTTP headers and top badrecord location match.~n").

run(Path) ->
    {ok, Image} = file:read_file(Path),
    {module, http_response} = code:load_binary(http_response, Path, Image),
    Empty = list_to_tuple([http_response_h|lists:duplicate(28, undefined)] ++ [[]]),
    Base = setelement(2, Empty, "no-cache"),
    WithSentinel = setelement(23, Base, "-1"),
    WithLength = setelement(23, Base, "12"),
    [{"cache-control", "no-cache"}] = Short = http_response:header_list(WithSentinel),
    [{"cache-control", "no-cache"}, {"content-length", "12"}] = Full = http_response:header_list(WithLength),
    Failure = try http_response:header_list(invalid) of
        Result -> error({unexpected_return, Result})
    catch
        error:{badrecord, invalid}:Stack ->
            lists:takewhile(fun({Module, _, _, _}) -> Module =:= http_response end, Stack)
    end,
    [{http_response, key_value_tuple, 2, _}|_] = Failure,
    true = code:delete(http_response),
    code:purge(http_response),
    {Short, Full, Failure}.
