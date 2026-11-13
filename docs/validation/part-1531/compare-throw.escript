#!/usr/bin/env escript
-mode(compile).
main([Reference, Candidate]) ->
    [{find, raise, 3}, {observed, throw, 1}] = tail_calls(Reference),
    [{find, raise, 3}, {observed, throw, 1}] = tail_calls(Candidate),
    Expected = observe(Reference),
    Expected = observe(Candidate),
    {absent, {absent, Stack}, value, value} = Expected,
    [{beam_case_tail_raise, observed, 2, _} | _] = Stack,
    io:format("Both paths match OTP, including the observed throw stack.~n").

tail_calls(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    [{Name, Call, Arity} ||
        {function, Name, 2, _, Instructions} <- Functions,
        Name =:= find orelse Name =:= observed,
        {call_ext_last, Arity, {extfunc, erlang, Call, Arity}, _} <- Instructions].

observe(Path) ->
    Module = beam_case_tail_raise,
    {ok, Image} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Image),
    try
        {absent, Stack} = Module:require(key, []),
        {Module:lookup(key, []), {absent, [Frame || {M, _, _, _} = Frame <- Stack, M =:= Module]},
         Module:lookup(key, [{key,value}]), Module:require(key, [{key,value}])}
    after
        code:delete(Module),
        code:purge(Module)
    end.
