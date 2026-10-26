-module(record_contract496_is_record1_p2).
-export([run/0,run_stacks/0,probe/1]).
-record #native{x=1}.
-record(sample,{x}).
run()->ordsets_core_support:run(?MODULE,cases()).
run_stacks()->ordsets_core_support:profiles(?MODULE,cases()).
cases()->[case_0,case_1,case_2,case_3,case_4,case_5,case_6,case_7,case_8,case_9,case_10,case_11,case_12,case_13,case_14,case_15].
probe(case_0)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{list_to_atom("f"++integer_to_list(I)),I}||I<-lists:seq(1,33)],#{is_exported=>true}))]);
probe(case_1)->erlang:is_record(opaque(records:create(record496,sample,[{list_to_atom("f"++integer_to_list(I)),I}||I<-lists:seq(1,33)],#{is_exported=>true})));
probe(case_2)->guard_probe(opaque(records:create(record496,sample,[{list_to_atom("f"++integer_to_list(I)),I}||I<-lists:seq(1,33)],#{is_exported=>true})));
probe(case_3)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(other496,sample,[{x,1}],#{is_exported=>false}))]);
probe(case_4)->erlang:is_record(opaque(records:create(other496,sample,[{x,1}],#{is_exported=>false})));
probe(case_5)->guard_probe(opaque(records:create(other496,sample,[{x,1}],#{is_exported=>false})));
probe(case_6)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,other,[{x,1}],#{is_exported=>false}))]);
probe(case_7)->erlang:is_record(opaque(records:create(record496,other,[{x,1}],#{is_exported=>false})));
probe(case_8)->guard_probe(opaque(records:create(record496,other,[{x,1}],#{is_exported=>false})));
probe(case_9)->erlang:apply(erlang,opaque(is_record),[opaque(records:create('модуль','запись',[{x,1}],#{is_exported=>false}))]);
probe(case_10)->erlang:is_record(opaque(records:create('модуль','запись',[{x,1}],#{is_exported=>false})));
probe(case_11)->guard_probe(opaque(records:create('модуль','запись',[{x,1}],#{is_exported=>false})));
probe(case_12)->erlang:apply(erlang,opaque(is_record),[opaque(records:create('','',[{x,1}],#{is_exported=>false}))]);
probe(case_13)->erlang:is_record(opaque(records:create('','',[{x,1}],#{is_exported=>false})));
probe(case_14)->guard_probe(opaque(records:create('','',[{x,1}],#{is_exported=>false})));
probe(case_15)->M=maps:from_list([{I,I}||I<-lists:seq(1,256)]),N=hd([C||C<-erts_internal:map_hashmap_children(M),is_map(C)]),erlang:apply(erlang,opaque(is_record),[N]).
guard_probe(V) when erlang:is_record(V)->true;guard_probe(_)->false.
opaque(V)->put(record_contract496_input,V),erase(record_contract496_input).
