-module(binary369_callback_supplement).
-export([cases/0,run/0,run_stacks/0,probe/1,byte_zero/1,byte_255/1,byte_256/1,byte_negative/1,improper_iodata/1,nested_replace/1,live_stack/1,identity/1]).
cases() -> [byte_zero,byte_255,byte_256,byte_negative,improper_binary_tail,nested_replace,live_stack_three,live_stack_four].
run() -> binary_all369_support:run(binary369_callback_supplement,cases()).
run_stacks() -> binary_all369_support:profiles(binary369_callback_supplement,cases()).
probe(byte_zero) -> binary:replace(<<"aaa">>,<<"a">>,fun binary369_callback_supplement:byte_zero/1,[global]);
probe(byte_255) -> binary:replace(<<"aaa">>,<<"a">>,fun binary369_callback_supplement:byte_255/1,[global]);
probe(byte_256) -> binary:replace(<<"aaa">>,<<"a">>,fun binary369_callback_supplement:byte_256/1,[global]);
probe(byte_negative) -> binary:replace(<<"aaa">>,<<"a">>,fun binary369_callback_supplement:byte_negative/1,[global]);
probe(improper_binary_tail) -> binary:replace(<<"aaa">>,<<"a">>,fun binary369_callback_supplement:improper_iodata/1,[global]);
probe(nested_replace) -> binary:replace(<<"aaa">>,<<"a">>,fun binary369_callback_supplement:nested_replace/1,[global]);
probe(live_stack_three) -> binary:replace(<<"aaa">>,<<"a">>,fun binary369_callback_supplement:live_stack/1);
probe(live_stack_four) -> binary:replace(<<"aaa">>,<<"a">>,fun binary369_callback_supplement:live_stack/1,[global]).
note(Value) -> put(binary_all369_trace,[Value|get(binary_all369_trace)]),Value.
byte_zero(Value) -> note(Value),0.
byte_255(Value) -> note(Value),255.
byte_256(Value) -> note(Value),256.
byte_negative(Value) -> note(Value),-1.
improper_iodata(Value) -> note(Value),[1|<<2>>].
nested_replace(Value) -> note({before_nested,Value}),Result = binary:replace(<<"aa">>,<<"a">>,fun binary369_callback_supplement:live_stack/1,[global]),note({after_nested,Value}),Result.
identity(Value) -> note({identity,Value}),Value.
live_stack(Value) -> note(Value),try erlang:error(live_callback_marker) catch Class:Reason:Stack -> note({live_stack,Class,Reason,Stack}),Value end.
