-module(beam_grouped_debug_probe).
-export([update/1, field/1, invoke/1]).
-record(state, {value}).
update(Value) -> (make(Value))#state{value = ok}.
field(Value) -> (make(Value))#state.value.
invoke(Value) -> (fun(X) -> {ok, X} end)(Value).
make(Value) -> #state{value = Value}.
