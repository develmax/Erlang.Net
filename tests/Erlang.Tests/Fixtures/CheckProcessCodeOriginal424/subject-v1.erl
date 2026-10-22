-module(beam_check_code_subject423).
-export([version/0,check/0,wait/1,tail/1,retain/1,make_fun/0,trace/0,wait_tail_native/1]).
version()->v1.
check()->erts_internal:check_process_code(?MODULE).
wait(Parent)->Parent!{ready,self()},receive {probe,From}->From!{answer,check()},wait(Parent);{tail,From}->tail(From);{retain,From}->retain(From);stop->ok end.
tail(From)->beam_check_code423:reply(From).
retain(From)->V=beam_check_code423:probe(),From!{answer,V},version().
make_fun()->fun()->version()end.
trace()->try erlang:error(saved_code_trace423) catch error:saved_code_trace423:S->S end.
wait_tail_native(P)->P!{ready,self()},receive go->erts_internal:check_process_code(?MODULE) end.
