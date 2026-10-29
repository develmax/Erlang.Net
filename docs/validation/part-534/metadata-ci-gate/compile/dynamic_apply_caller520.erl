-module(dynamic_apply_caller520).
-export([worker/1, tail0/2, tail1/3, tail2/4, after0/2, after1/3, after2/4, call3/5]).

worker({Parent, LoaderModule, LoaderImage}) ->
    Observations = observations(cases()) ++ loader_cases(LoaderModule, LoaderImage),
    Parent ! {dynamic_apply520, self(), Observations}.

observations([]) -> [];
observations([Case|Rest]) -> [observe(Case)|observations(Rest)].

observe({Tag, Style, Module, Function, Arguments}) ->
    try dispatch(Style, Module, Function, Arguments) of
        Value -> {Tag, {returned, Value}}
    catch Class:Reason:Stack ->
        {Tag, {raised, Class, Reason, Stack}}
    end.

dispatch(tail, M, F, []) -> tail0(M, F);
dispatch(tail, M, F, [A]) -> tail1(M, F, A);
dispatch(tail, M, F, [A,B]) -> tail2(M, F, A, B);
dispatch(non_tail, M, F, []) -> after0(M, F);
dispatch(non_tail, M, F, [A]) -> after1(M, F, A);
dispatch(non_tail, M, F, [A,B]) -> after2(M, F, A, B).

tail0(M, F) -> M:F().
tail1(M, F, A) -> M:F(A).
tail2(M, F, A, B) -> M:F(A, B).
after0(M, F) -> {non_tail, M:F()}.
after1(M, F, A) -> {non_tail, M:F(A)}.
after2(M, F, A, B) -> {non_tail, M:F(A, B)}.

cases() ->
    [{tail_ok, tail, dynamic_apply_target520, echo, [{value,520}]},
     {non_tail_ok, non_tail, dynamic_apply_target520, echo, [{value,520}]},
     {tail_unknown_module, tail, dynamic_apply_missing520, echo, [520]},
     {non_tail_unknown_module, non_tail, dynamic_apply_missing520, echo, [520]},
     {tail_unknown_function, tail, dynamic_apply_target520, absent, [520]},
     {non_tail_unknown_function, non_tail, dynamic_apply_target520, absent, [520]},
     {tail_bad_module, tail, 520, echo, [520]},
     {non_tail_bad_module, non_tail, 520, echo, [520]},
     {tail_bad_function, tail, dynamic_apply_target520, {not_an_atom}, [520]},
     {non_tail_bad_function, non_tail, dynamic_apply_target520, {not_an_atom}, [520]},
     {tail_wrong_zero_arity, tail, dynamic_apply_target520, echo, []},
     {non_tail_wrong_zero_arity, non_tail, dynamic_apply_target520, echo, []},
     {tail_wrong_two_arity, tail, dynamic_apply_target520, echo, [520,521]},
     {non_tail_wrong_two_arity, non_tail, dynamic_apply_target520, echo, [520,521]},
     {tail_error, tail, dynamic_apply_target520, raise_error, [520]},
     {non_tail_error, non_tail, dynamic_apply_target520, raise_error, [520]},
     {tail_throw, tail, dynamic_apply_target520, raise_throw, [520]},
     {non_tail_throw, non_tail, dynamic_apply_target520, raise_throw, [520]},
     {tail_exit, tail, dynamic_apply_target520, raise_exit, [520]},
     {non_tail_exit, non_tail, dynamic_apply_target520, raise_exit, [520]}].

call3(M, F, A, B, C) -> M:F(A, B, C).

loader_cases(Module, Image) ->
    [{loader_valid, capture_loader(fun() -> erlang:load_module(Module, Image) end)},
     {loader_code_change, capture_loader(fun() -> call3(Module, code_change, old, state, extra) end)},
     {loader_bad_module_type, capture_loader(fun() -> erlang:load_module(520, Image) end)},
     {loader_bad_binary_type, capture_loader(fun() -> erlang:load_module(Module, not_binary) end)},
     {loader_malformed_binary, capture_loader(fun() -> erlang:load_module(Module, <<1,2,3>>) end)},
     {loader_module_mismatch, capture_loader(fun() -> erlang:load_module(wrong_module520, Image) end)}].

capture_loader(Fun) ->
    try Fun() of Value -> {returned, Value}
    catch Class:Reason:Stack -> {raised, Class, Reason, Stack}
    end.
