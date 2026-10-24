-module(otp_compiler465_observer).
-file("otp_compiler465_observer.erl", 1).
-export([worker/1, run/0]).

worker(Parent) ->
    Parent ! {finished, run()}.

run() ->
    [observe(format_number, fun() -> beam_opcodes:format_number() end),
     observe(label, fun() -> beam_opcodes:opcode(label, 1) end),
     observe(call, fun() -> beam_opcodes:opcode(call, 2) end),
     observe(return, fun() -> beam_opcodes:opcode(return, 0) end),
     observe(label_name, fun() -> beam_opcodes:opname(1) end),
     observe(return_name, fun() -> beam_opcodes:opname(19) end),
     observe(bad_instruction, fun() -> beam_opcodes:opcode(missing, 1) end),
     observe(bad_arity, fun() -> beam_opcodes:opcode(label, 2) end),
     observe(zero_opcode, fun() -> beam_opcodes:opname(0) end),
     observe(large_opcode, fun() -> beam_opcodes:opname(1000) end),
     observe(atom_opcode, fun() -> beam_opcodes:opname(invalid) end),
     observe(pure_abs, fun() -> erl_bifs:is_pure(erlang, abs, 1) end),
     observe(impure_self, fun() -> erl_bifs:is_pure(erlang, self, 0) end),
     observe(foreign_module, fun() -> erl_bifs:is_pure(other, abs, 1) end),
     observe(safe_atom, fun() -> erl_bifs:is_safe(erlang, is_atom, 1) end),
     observe(unsafe_hd, fun() -> erl_bifs:is_safe(erlang, hd, 1) end),
     observe(exit_error, fun() -> erl_bifs:is_exit_bif(erlang, error, 1) end),
     observe(exit_exit, fun() -> erl_bifs:is_exit_bif(erlang, exit, 1) end),
     observe(non_exit, fun() -> erl_bifs:is_exit_bif(erlang, abs, 1) end),
     observe(unrecognised_arguments, fun() -> erl_bifs:is_pure(42, [], bad) end)].

observe(Name, Function) ->
    try Function() of
        Value -> {Name, {returned, Value}}
    catch
        Class:Reason:Stack -> {Name, {caught, Class, Reason, Stack}}
    end.
