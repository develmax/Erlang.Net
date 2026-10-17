-module(make_reference_callback_contract).
-export([load/1,references/1,independent/1,widths/0,run/0]).
load(Path) -> erlang:load_nif(Path,[]).
references(_) -> erlang:nif_error(not_loaded).
independent(_) -> erlang:nif_error(not_loaded).
widths() -> erlang:nif_error(not_loaded).
consume(Rs) -> {length(Rs),length(lists:usort(Rs)),lists:all(fun(R)->is_reference(R) andalso node(R)=:=node() end,Rs)}.
pairs(Mode) -> [A,B,C]=make_reference_callback_contract:independent(Mode),{consume([A,B,C]),A=:=B,A=:=C,B=:=C}.
run() ->
    A=make_reference_callback_contract:references(65),Before=make_ref(),
    B=make_reference_callback_contract:references(65),After=make_ref(),
    R=make_reference_callback_contract:independent(0),
    Rows=[
      {local_counts,[{N,consume(make_reference_callback_contract:references(N))} || N <- [0,1,63,64,65]]},
      {shared_reference_domain,consume([Before,After]++A++B)},
      {call_lifetime,{consume(A),consume(B),R=:=make_reference_callback_contract:independent(0)}},
      {load_environment,make_reference_callback_contract:independent(1)},
      {independent_environment,pairs(2)},
      {independent_clear,pairs(3)},
      {foreign_allocate,pairs(4)},
      {foreign_make,pairs(5)},
      {foreign_handoff,pairs(6)}
    ],
    {make_reference_callback_contract,Rows,make_reference_callback_contract:widths()}.
