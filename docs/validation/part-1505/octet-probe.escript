#!/usr/bin/env escript
-mode(compile).

main([Source, Before, After]) ->
    {ok, Module, Reference} = compile:file(Source, [binary, deterministic]),
    {ok, Old} = file:read_file(Before),
    {ok, New} = file:read_file(After),
    lists:foreach(fun(Name) -> compare_types(Name, Reference, Old, New) end, [negative, octets, any_octets]),
    Values = [0, 1, 127, 128, 255, 256, 1 bsl 80, 0.0, 128.5, invalid, [], <<>>],
    Calls = [{Name, [Value]} || Name <- [signed, unsigned, unguarded], Value <- Values] ++
        [{Name, [Value]} || Name <- [signed, unsigned], Value <- [-1, -128, -129, -0.5, -(1 bsl 80)]],
    check(Module, Reference, Old, New, Calls);
main(["release", Root, Before, After]) ->
    Module = 'ECPrivateKey',
    Path = "lib/public_key-1.21.7/ebin/ECPrivateKey.beam",
    [Reference, Old, New] = [begin
        {ok, Image} = file:read_file(filename:join(Directory, Path)),
        Image
    end || Directory <- [Root, Before, After]],
    lists:foreach(fun(Name) -> compare_types(Name, Reference, Old, New) end,
                  [encode_integer_neg, minimum_octets]),
    Versions = [ecPrivkeyVer1, -129, -128, -1, 0, 1, 127, 128, 1 bsl 80, 0.5, -0.5, invalid],
    Keys = [binary:copy(<<1>>, Length) || Length <- [0, 1, 127, 128, 255, 256]],
    Calls = [{encode, [Module, {Module, Version, Key, asn1_NOVALUE, asn1_NOVALUE, asn1_NOVALUE}]}
             || Version <- Versions, Key <- Keys],
    check(Module, Reference, Old, New, Calls).

check(Module, Reference, Old, New, Calls) ->
    Expected = run(Module, Reference, Calls),
    Expected = run(Module, Old, Calls),
    Expected = run(Module, New, Calls),
    io:format("~B matching return and exception observations~n", [length(Calls)]).

compare_types(Name, Reference, Old, New) ->
    Expected = types(Reference, Name),
    Current = types(New, Name),
    io:format("~p/2 OTP ~0p, previous ~0p, current ~0p~n", [Name, Expected, types(Old, Name), Current]),
    case Name of
        any_octets ->
            %% Unknown entry must stay untyped at the comparison and shift.
            [untyped, untyped, _] = Current;
        _ ->
            Expected = Current
    end.

types(Binary, Name) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Binary),
    [Code] = [Body || {function, N, 2, _, Body} <- Functions, N =:= Name],
    [type(Operand) || Instruction <- Code, Operand <- octet_operand(Instruction)].

octet_operand({test, is_eq_exact, _, [Operand, {integer, _}]}) -> [Operand];
octet_operand({gc_bif, Op, _, _, [Operand, _], _}) when Op =:= 'bsr'; Op =:= 'band' -> [Operand];
octet_operand(_) -> [].

type({tr, _, Type}) -> Type;
type(_) -> untyped.

run(Module, Binary, Calls) ->
    code:purge(Module),
    {module, Module} = code:load_binary(Module, "octet-probe.beam", Binary),
    [try apply(Module, Name, Arguments) of
         Result -> {ok, without_probe_frames(Result)}
     catch Class:Reason -> {Class, Reason}
     end || {Name, Arguments} <- Calls].

without_probe_frames({error, {asn1, {Reason, Stack}}}) when is_list(Stack) ->
    {error, {asn1, {Reason, lists:takewhile(fun(Frame) -> element(1, Frame) =/= ?MODULE end, Stack)}}};
without_probe_frames(Value) -> Value.
