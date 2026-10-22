-module(beam_internal_scalars413).
-export([run/0,run_stacks/0,worker/2]).
cases()->[bin_decimal,bin_negative,bin_plus,bin_empty,bin_sign_only,bin_zeroes,bin_invalid_digit,bin_bad_base,bin_atom_base,bin_huge_base,bin_nonbinary,bin_partial_bits,bin_unaligned,list_decimal,list_prefix,list_negative,list_plus,list_nil_badbase,list_nonlist_badbase,list_badbase,list_sign_only,list_sign_improper,list_zero_improper,list_value_improper,list_zero_badchar,list_zero_badhead,list_badhead,list_small_byte_wrap,list_negative_byte_wrap,list_wrapped_sign_not_sign,list_large_head,list_zero_large_head,list_leading_zeroes,native_unit,perf_unit,clock_unit_invariants,wrong_binary_arity,wrong_list_arity,wrong_unit_arity,wrong_perf_arity,bin_base2_limit,list_base2_limit,bin_base2_over,list_base2_over,bin_base3_limit,list_base3_limit,bin_base3_over,list_base3_over,bin_base10_limit,list_base10_limit,bin_base10_over,list_base10_over,bin_base16_limit,list_base16_limit,bin_base16_over,list_base16_over,bin_base36_limit,list_base36_limit,bin_base36_over,list_base36_over,bin_over_invalid,list_over_then_invalid].
run()->[{C,capture(C,default)}||C<-cases()].
run_stacks()->[{D,[{C,capture(C,D)}||C<-cases()]}||D<-[default,0,1,2,8]].
capture(C,D)->P=self(),R=make_ref(),{Pid,M}=spawn_monitor(?MODULE,worker,[{P,R},{C,D}]),receive {R,V}->receive {'DOWN',M,process,Pid,normal}->V end end.
worker({P,R},{C,D})->OldDepth=case D of default->default;_->erlang:system_flag(backtrace_depth,D) end,
 V=try probe(C) of X->{value,X} catch K:E:S->{caught,K,E,S} end,
 case OldDepth of default->ok;_->erlang:system_flag(backtrace_depth,OldDepth) end,P!{R,V}.
probe(bin_decimal)->erts_internal:binary_to_integer(<<"123">>,10);
probe(bin_negative)->erts_internal:binary_to_integer(<<"-00123">>,10);
probe(bin_plus)->erts_internal:binary_to_integer(<<"+fF">>,16);
probe(bin_empty)->erts_internal:binary_to_integer(<<>>,10);
probe(bin_sign_only)->erts_internal:binary_to_integer(<<"-">>,10);
probe(bin_zeroes)->erts_internal:binary_to_integer(binary:copy(<<"0">>,2048),10);
probe(bin_invalid_digit)->erts_internal:binary_to_integer(<<"19">>,8);
probe(bin_bad_base)->erts_internal:binary_to_integer(<<"1">>,37);
probe(bin_atom_base)->erts_internal:binary_to_integer(<<"1">>,decimal);
probe(bin_huge_base)->erts_internal:binary_to_integer(<<"1">>,1 bsl 80);
probe(bin_nonbinary)->erts_internal:binary_to_integer("123",10);
probe(bin_partial_bits)->erts_internal:binary_to_integer(<<1:1>>,10);
probe(bin_unaligned)->begin <<_:1,B:3/binary,_:7>> = <<0:1,"123",0:7>>,erts_internal:binary_to_integer(B,10) end;
probe(list_decimal)->erts_internal:list_to_integer("123",10);
probe(list_prefix)->erts_internal:list_to_integer("123x",10);
probe(list_negative)->erts_internal:list_to_integer("-00123x",10);
probe(list_plus)->erts_internal:list_to_integer("+fF!",16);
probe(list_nil_badbase)->erts_internal:list_to_integer([],badbase);
probe(list_nonlist_badbase)->erts_internal:list_to_integer(notlist,badbase);
probe(list_badbase)->erts_internal:list_to_integer("1",badbase);
probe(list_sign_only)->erts_internal:list_to_integer("-",10);
probe(list_sign_improper)->erts_internal:list_to_integer([$+|tail],10);
probe(list_zero_improper)->erts_internal:list_to_integer([$0,$0|tail],10);
probe(list_value_improper)->erts_internal:list_to_integer([$1,$2|tail],10);
probe(list_zero_badchar)->erts_internal:list_to_integer("000!",10);
probe(list_zero_badhead)->erts_internal:list_to_integer([$0,notdigit],10);
probe(list_badhead)->erts_internal:list_to_integer([notdigit],10);
probe(list_small_byte_wrap)->erts_internal:list_to_integer([304,305,306,$x],10);
probe(list_negative_byte_wrap)->erts_internal:list_to_integer([-208,-207,$x],10);
probe(list_wrapped_sign_not_sign)->erts_internal:list_to_integer([-213,$1],10);
probe(list_large_head)->erts_internal:list_to_integer([1 bsl 80,$1],10);
probe(list_zero_large_head)->erts_internal:list_to_integer([$0,1 bsl 80],10);
probe(list_leading_zeroes)->erts_internal:list_to_integer(lists:duplicate(2048,$0)++"12!",10);
probe(native_unit)->erts_internal:time_unit();
probe(perf_unit)->erts_internal:perf_counter_unit();
probe(clock_unit_invariants)->{erts_internal:time_unit()=:=erlang:convert_time_unit(1,second,native),erts_internal:perf_counter_unit()=:=erlang:convert_time_unit(1,second,perf_counter)};
probe(wrong_binary_arity)->apply(erts_internal,binary_to_integer,[<<"1">>]);
probe(wrong_list_arity)->apply(erts_internal,list_to_integer,["1"]);
probe(wrong_unit_arity)->apply(erts_internal,time_unit,[unused]);
probe(wrong_perf_arity)->apply(erts_internal,perf_counter_unit,[unused]);
probe(bin_base2_limit)->erts_internal:binary_to_integer(binary:copy(<<"1">>,59),2);
probe(list_base2_limit)->erts_internal:list_to_integer(lists:duplicate(59,$1),2);
probe(bin_base2_over)->erts_internal:binary_to_integer(binary:copy(<<"1">>,60),2);
probe(list_base2_over)->erts_internal:list_to_integer(lists:duplicate(60,$1),2);
probe(bin_base3_limit)->erts_internal:binary_to_integer(binary:copy(<<"1">>,37),3);
probe(list_base3_limit)->erts_internal:list_to_integer(lists:duplicate(37,$1),3);
probe(bin_base3_over)->erts_internal:binary_to_integer(binary:copy(<<"1">>,38),3);
probe(list_base3_over)->erts_internal:list_to_integer(lists:duplicate(38,$1),3);
probe(bin_base10_limit)->erts_internal:binary_to_integer(binary:copy(<<"1">>,17),10);
probe(list_base10_limit)->erts_internal:list_to_integer(lists:duplicate(17,$1),10);
probe(bin_base10_over)->erts_internal:binary_to_integer(binary:copy(<<"1">>,18),10);
probe(list_base10_over)->erts_internal:list_to_integer(lists:duplicate(18,$1),10);
probe(bin_base16_limit)->erts_internal:binary_to_integer(binary:copy(<<"1">>,14),16);
probe(list_base16_limit)->erts_internal:list_to_integer(lists:duplicate(14,$1),16);
probe(bin_base16_over)->erts_internal:binary_to_integer(binary:copy(<<"1">>,15),16);
probe(list_base16_over)->erts_internal:list_to_integer(lists:duplicate(15,$1),16);
probe(bin_base36_limit)->erts_internal:binary_to_integer(binary:copy(<<"1">>,11),36);
probe(list_base36_limit)->erts_internal:list_to_integer(lists:duplicate(11,$1),36);
probe(bin_base36_over)->erts_internal:binary_to_integer(binary:copy(<<"1">>,12),36);
probe(list_base36_over)->erts_internal:list_to_integer(lists:duplicate(12,$1),36);
probe(bin_over_invalid)->erts_internal:binary_to_integer(<<"xxxxxxxxxxxxxxxxxx">>,10);
probe(list_over_then_invalid)->erts_internal:list_to_integer("111111111111111111x",10).
