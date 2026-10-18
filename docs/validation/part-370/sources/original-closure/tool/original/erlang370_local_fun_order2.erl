-module(erlang370_local_fun_order2).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [local_fun_olduniq_equal,local_fun_olduniq_notequal,local_fun_olduniq_exact,local_fun_olduniq_notexact,local_fun_olduniq_less,local_fun_olduniq_greater,local_fun_olduniq_lessequal,local_fun_olduniq_greaterequal,local_fun_olduniq_minimum,local_fun_olduniq_maximum,local_fun_equal_olduniq_equal,local_fun_equal_olduniq_notequal,local_fun_equal_olduniq_exact,local_fun_equal_olduniq_notexact,local_fun_equal_olduniq_less,local_fun_equal_olduniq_greater,local_fun_equal_olduniq_lessequal,local_fun_equal_olduniq_greaterequal,local_fun_equal_olduniq_minimum,local_fun_equal_olduniq_maximum].
run() -> erlang370_pure_support:run(erlang370_local_fun_order2,cases()).
run_stacks() -> erlang370_pure_support:profiles(erlang370_local_fun_order2,cases()).
probe(local_fun_olduniq_equal) -> compare('==',2);
probe(local_fun_olduniq_notequal) -> compare('/=',2);
probe(local_fun_olduniq_exact) -> compare('=:=',2);
probe(local_fun_olduniq_notexact) -> compare('=/=',2);
probe(local_fun_olduniq_less) -> compare('<',2);
probe(local_fun_olduniq_greater) -> compare('>',2);
probe(local_fun_olduniq_lessequal) -> compare('=<',2);
probe(local_fun_olduniq_greaterequal) -> compare('>=',2);
probe(local_fun_olduniq_minimum) -> compare('min',2);
probe(local_fun_olduniq_maximum) -> compare('max',2);
probe(local_fun_equal_olduniq_equal) -> compare('==',1);
probe(local_fun_equal_olduniq_notequal) -> compare('/=',1);
probe(local_fun_equal_olduniq_exact) -> compare('=:=',1);
probe(local_fun_equal_olduniq_notexact) -> compare('=/=',1);
probe(local_fun_equal_olduniq_less) -> compare('<',1);
probe(local_fun_equal_olduniq_greater) -> compare('>',1);
probe(local_fun_equal_olduniq_lessequal) -> compare('=<',1);
probe(local_fun_equal_olduniq_greaterequal) -> compare('>=',1);
probe(local_fun_equal_olduniq_minimum) -> compare('min',1);
probe(local_fun_equal_olduniq_maximum) -> compare('max',1).
compare(Function,SecondOldUnique) ->
    StubModule = stub_module(SecondOldUnique),
    A = local_stub(StubModule,255,1),
    B = local_stub(StubModule,0,SecondOldUnique),
    {uniq,1} = erlang:fun_info(A,uniq),
    {uniq,SecondOldUnique} = erlang:fun_info(B,uniq),
    Result = erlang370_pure_support:invoke(Function,[A,B]),
    case Function of
        min -> {Result,Result =:= A,Result =:= B,erlang:fun_info(Result)};
        max -> {Result,Result =:= A,Result =:= B,erlang:fun_info(Result)};
        _ -> Result
    end.
stub_module(2) -> erlang370_reversed_input_stub2;
stub_module(1) -> erlang370_equal_input_stub2.
local_stub(ModuleName,Byte,OldUnique) ->
    Unique = binary:copy(<<Byte>>,16),
    <<131,Module/binary>> = erlang:term_to_binary(ModuleName),
    <<131,OldIndex/binary>> = erlang:term_to_binary(0),
    <<131,OldUniq/binary>> = erlang:term_to_binary(OldUnique),
    <<131,Owner/binary>> = erlang:term_to_binary(self()),
    Body = <<0:8,Unique/binary,0:32,0:32,Module/binary,OldIndex/binary,OldUniq/binary,Owner/binary>>,
    Size = byte_size(Body)+4,
    erlang:binary_to_term(<<131,112,Size:32,Body/binary>>).
