-module(binary_all369_support).
-export([run/2,profiles/2,shifted/1,backing/1,compiled_observable/1,
         ordinary_reference/0,wrong_tag_pattern/0,replace_binary/1,replace_iolist/1,
         replace_bad/1,replace_bad_later/1,replace_throw_badopt/1,replace_error/1,replace_exit/1,zero/0]).
run(Module,Names) -> [{Name,value(Module,Name)} || Name <- Names].
value(Module,Name) ->
    put(binary_all369_trace,[]),put(binary_all369_identity,[]),
    try erlang:apply(Module,probe,[Name]) of Value -> {ok,Value,effects()}
    catch Class:Reason -> {error,Class,Reason,effects()}
    after erase(binary_all369_trace),erase(binary_all369_identity) end.
profiles(Module,Names) ->
    Default = {default,[{Name,capture(Module,Name)} || Name <- Names]},
    Old = erlang:system_flag(backtrace_depth,8),
    try [Default | [at_depth(Depth,Module,Names) || Depth <- [0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(Depth,Module,Names) ->
    erlang:system_flag(backtrace_depth,Depth),
    {Depth,[{Name,capture(Module,Name)} || Name <- Names]}.
capture(Module,Name) ->
    put(binary_all369_trace,[]),put(binary_all369_identity,[]),
    try erlang:apply(Module,probe,[Name]) of Value -> {ok,Value,effects()}
    catch Class:Reason:Stack -> {error,Class,Reason,Stack,effects()}
    after erase(binary_all369_trace),erase(binary_all369_identity) end.
effects() -> {lists:reverse(get(binary_all369_trace)),lists:reverse(get(binary_all369_identity))}.
shifted(Binary) ->
    Size = byte_size(Binary), Wrapped = <<0:3,Binary/binary,0:5>>,
    <<_:3,Result:Size/binary,_:5>> = Wrapped, Result.
backing(Binary) -> {byte_size(Binary),binary:referenced_byte_size(Binary)}.
compiled_observable(Pattern) ->
    CP = binary:compile_pattern(Pattern), CP2 = binary:compile_pattern(Pattern),
    {Tag,Reference} = CP, {Tag2,Reference2} = CP2,
    {Tag,is_reference(Reference),Tag2,is_reference(Reference2),Reference =:= Reference2,
     binary:match(<<"abcabc">>,CP),binary:matches(<<"abcabc">>,CP),binary:split(<<"abcabc">>,CP)}.
ordinary_reference() ->
    Reference = make_ref(),put(binary_all369_identity,[{ordinary,Reference}]),{bm,Reference}.
wrong_tag_pattern() ->
    CP = binary:compile_pattern(<<"a">>),{Tag,Reference} = CP,
    Match = binary:match(<<"abcabc">>,CP),
    put(binary_all369_identity,[{compiled,Tag,Reference,<<"a">>,<<"abcabc">>,Match}]),{ac,Reference}.
note(Value) -> put(binary_all369_trace,[Value|get(binary_all369_trace)]),Value.
replace_binary(Value) -> note(Value),<<"X">>.
replace_iolist(Value) -> note(Value),[<<"[">>,Value,<<"]">>].
replace_bad(Value) -> note(Value),[256].
replace_bad_later(Value) ->
    note(Value),case length(get(binary_all369_trace)) of 1 -> [256]; _ -> <<"X">> end.
replace_throw_badopt(Value) -> note(Value),throw(badopt).
replace_error(Value) -> note(Value),erlang:error(callback_error).
replace_exit(Value) -> note(Value),exit(callback_exit).
zero() -> zero.
