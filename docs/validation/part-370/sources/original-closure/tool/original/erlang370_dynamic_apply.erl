-module(erlang370_dynamic_apply).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [tail_invalid_module,tail_invalid_function,tail_improper_arguments,tail_arguments_1024,tail_special_error,tail_special_exit,tail_special_throw,non_tail_invalid_module,non_tail_invalid_function,non_tail_improper_arguments,non_tail_arguments_1024,non_tail_special_error,non_tail_special_exit,non_tail_special_throw].
run() -> erlang370_pure_support:run(erlang370_dynamic_apply,cases()).
run_stacks() -> erlang370_pure_support:profiles(erlang370_dynamic_apply,cases()).
probe(tail_invalid_module) -> tail_apply(42,abs,[1]);
probe(tail_invalid_function) -> tail_apply(erlang,42,[1]);
probe(tail_improper_arguments) -> tail_apply(erlang,abs,[1|tail]);
probe(tail_arguments_1024) -> tail_apply(erlang,abs,lists:duplicate(1024,a));
probe(tail_special_error) -> tail_apply(erlang,error,[marker]);
probe(tail_special_exit) -> tail_apply(erlang,exit,[marker]);
probe(tail_special_throw) -> tail_apply(erlang,throw,[marker]);
probe(non_tail_invalid_module) -> non_tail_apply(42,abs,[1]);
probe(non_tail_invalid_function) -> non_tail_apply(erlang,42,[1]);
probe(non_tail_improper_arguments) -> non_tail_apply(erlang,abs,[1|tail]);
probe(non_tail_arguments_1024) -> non_tail_apply(erlang,abs,lists:duplicate(1024,a));
probe(non_tail_special_error) -> non_tail_apply(erlang,error,[marker]);
probe(non_tail_special_exit) -> non_tail_apply(erlang,exit,[marker]);
probe(non_tail_special_throw) -> non_tail_apply(erlang,throw,[marker]).
tail_apply(Module,Function,Arguments) -> erlang:apply(Module,Function,Arguments).
non_tail_apply(Module,Function,Arguments) -> Value=erlang:apply(Module,Function,Arguments), {after_call,Value}.
