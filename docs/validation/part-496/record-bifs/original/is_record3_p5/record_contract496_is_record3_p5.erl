-module(record_contract496_is_record3_p5).
-export([run/0,run_stacks/0,probe/1]).
-record #native{x=1}.
-record(sample,{x}).
run()->ordsets_core_support:run(?MODULE,cases()).
run_stacks()->ordsets_core_support:profiles(?MODULE,cases()).
cases()->[case_0,case_1,case_2,case_3,case_4,case_5,case_6,case_7,case_8,case_9,case_10,case_11,case_12,case_13,case_14].
probe(case_0)->erlang:is_record(opaque({sample,1}),opaque(sample),opaque(2));
probe(case_1)->guard_literal_0(opaque({sample,1}));
probe(case_2)->erlang:is_record(opaque({sample,1,2}),opaque(sample),opaque(3));
probe(case_3)->guard_literal_2(opaque({sample,1,2}));
probe(case_4)->erlang:is_record(opaque({sample,1}),opaque(sample),opaque([]));
probe(case_5)->erlang:is_record(opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(record496),opaque(sample));
probe(case_6)->guard_literal_3(opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})));
probe(case_7)->erlang:is_record(opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(sample),opaque(2));
probe(case_8)->guard_literal_0(opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})));
probe(case_9)->erlang:is_record(opaque(#native{}),opaque(?MODULE),opaque(native));
probe(case_10)->guard_literal_4(opaque(#native{}));
probe(case_11)->erlang:is_record(opaque(#native{}),opaque(?MODULE),opaque(2));
probe(case_12)->guard_literal_5(opaque(#native{}));
probe(case_13)->erlang:is_record(opaque(#native{}),opaque(0),opaque(native));
probe(case_14)->M=maps:from_list([{I,I}||I<-lists:seq(1,256)]),N=hd([C||C<-erts_internal:map_hashmap_children(M),is_map(C)]),erlang:apply(erlang,opaque(is_record),[N,sample,2]).
guard_literal_0(V) when erlang:is_record(V,sample,2)->true;guard_literal_0(_)->false.
guard_literal_1(V) when erlang:is_record(V,sample,1)->true;guard_literal_1(_)->false.
guard_literal_2(V) when erlang:is_record(V,sample,3)->true;guard_literal_2(_)->false.
guard_literal_3(V) when erlang:is_record(V,record496,sample)->true;guard_literal_3(_)->false.
guard_literal_4(V) when erlang:is_record(V,?MODULE,native)->true;guard_literal_4(_)->false.
guard_literal_5(V) when erlang:is_record(V,?MODULE,2)->true;guard_literal_5(_)->false.
opaque(V)->put(record_contract496_input,V),erase(record_contract496_input).
