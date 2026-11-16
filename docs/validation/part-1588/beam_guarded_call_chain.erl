-module(beam_guarded_call_chain).
-export([run/3]).
run(Input, Saved, Mode) when is_atom(Input), Mode =:= run ->
    Value = prepare(Input),
    Checked = prepare(Value),
    finish(Checked, Saved).
prepare(Input) -> {prepared,Input}.
finish(Value, Saved) -> {Value,Saved}.
