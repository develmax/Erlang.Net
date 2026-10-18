-module(binary369_insert_contract).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [insert_true_no_match,insert_true_match,integer_zero_pin,integer_negative_pin,integer_outside_pin,list_zero_no_pin,no_insert_no_pin].
run() -> binary_all369_support:run(binary369_insert_contract,cases()).
run_stacks() -> binary_all369_support:profiles(binary369_insert_contract,cases()).
probe(insert_true_no_match) -> run_case(true,<<2>>);
probe(insert_true_match) -> run_case(true,<<1>>);
probe(integer_zero_pin) -> run_case(0,<<1>>);
probe(integer_negative_pin) -> run_case(-1,<<1>>);
probe(integer_outside_pin) -> run_case(258,<<1>>);
probe(list_zero_no_pin) -> run_case([0],<<1>>);
probe(no_insert_no_pin) -> run_case([],<<1>>).
run_case(Insert,Subject) ->
    Replacement = append_loop(257,<<>>),
    Before = observe(Replacement),
    Result = try binary:replace(Subject,<<1>>,Replacement,[{insert_replaced,Insert}]) of
        Value -> {ok,Value}
    catch Class:Reason:Stack -> {error,Class,Reason,Stack}
    end,
    {Before,Result,observe(Replacement)}.
observe(Binary) -> {byte_size(Binary),binary:referenced_byte_size(Binary)}.
append_loop(0,Binary) -> Binary;
append_loop(Count,Binary) -> append_loop(Count-1,<<Binary/binary,7>>).
