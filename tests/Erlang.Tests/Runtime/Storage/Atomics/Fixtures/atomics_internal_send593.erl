-module(atomics_internal_send593).
-export([run/1]).

%% contract_worker513:worker/3 contains the actual send. The worker is loaded
%% from its original BEAM or compiled from those exact admitted Erlang bytes.
run(Observer) ->
    {{returned, Fields}, normal} = contract_worker513:run(atomics_magic586),
    check_fields(Fields),
    Observer ! {send_result_held593, self()},
    receive drop593 -> drop_result() end.

check_fields([{wordsize, 8}, {atomics_all_seven, Rows},
              {constructor_errors, _}, {native_errors, Errors},
              {counters_original_default_and_atomics, Counters}]) ->
    check_rows(Rows),
    check_error_ref(Errors),
    check_counters(Counters).

check_rows([{_, _, {returned, _}} | Tail]) -> check_rows(Tail);
check_rows([]) -> ok.

check_error_ref([{get_index_zero,
                  {raised, error, badarg, [{atomics, get, [Ref, 0], _} | _]}} | _])
  when is_reference(Ref) -> ok.

check_counters([{[], {returned, _}}, {[atomics], {returned, _}}]) -> ok.

drop_result() ->
    true = garbage_collect(),
    ok.
