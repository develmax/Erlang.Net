-module(record_contract496_is_record1_p1).
-export([run/0,run_stacks/0,probe/1]).
-record #native{x=1}.
-record(sample,{x}).
run()->ordsets_core_support:run(?MODULE,cases()).
run_stacks()->ordsets_core_support:profiles(?MODULE,cases()).
cases()->[case_0,case_1,case_2,case_3,case_4,case_5,case_6,case_7,case_8,case_9,case_10,case_11,case_12,case_13,case_14,case_15,case_16,case_17,case_18,case_19,case_20,case_21,case_22,case_23,case_24,case_25,case_26,case_27,case_28,case_29,case_30,case_31,case_32,case_33,case_34,case_35,case_36,case_37,case_38,case_39,case_40,case_41,case_42,case_43,case_44,case_45,case_46,case_47,case_48,case_49,case_50,case_51,case_52,case_53,case_54,case_55,case_56,case_57,case_58,case_59].
probe(case_0)->erlang:apply(erlang,opaque(is_record),[opaque(<<>>)]);
probe(case_1)->erlang:is_record(opaque(<<>>));
probe(case_2)->guard_probe(opaque(<<>>));
probe(case_3)->erlang:apply(erlang,opaque(is_record),[opaque(<<0,255>>)]);
probe(case_4)->erlang:is_record(opaque(<<0,255>>));
probe(case_5)->guard_probe(opaque(<<0,255>>));
probe(case_6)->erlang:apply(erlang,opaque(is_record),[opaque(<<1:1>>)]);
probe(case_7)->erlang:is_record(opaque(<<1:1>>));
probe(case_8)->guard_probe(opaque(<<1:1>>));
probe(case_9)->erlang:apply(erlang,opaque(is_record),[opaque(fun erlang:abs/1)]);
probe(case_10)->erlang:is_record(opaque(fun erlang:abs/1));
probe(case_11)->guard_probe(opaque(fun erlang:abs/1));
probe(case_12)->erlang:apply(erlang,opaque(is_record),[opaque(fun(X)->X end)]);
probe(case_13)->erlang:is_record(opaque(fun(X)->X end));
probe(case_14)->guard_probe(opaque(fun(X)->X end));
probe(case_15)->erlang:apply(erlang,opaque(is_record),[opaque(list_to_pid("<0.88.0>"))]);
probe(case_16)->erlang:is_record(opaque(list_to_pid("<0.88.0>")));
probe(case_17)->guard_probe(opaque(list_to_pid("<0.88.0>")));
probe(case_18)->erlang:apply(erlang,opaque(is_record),[opaque(#{})]);
probe(case_19)->erlang:is_record(opaque(#{}));
probe(case_20)->guard_probe(opaque(#{}));
probe(case_21)->erlang:apply(erlang,opaque(is_record),[opaque(#{sample=>1})]);
probe(case_22)->erlang:is_record(opaque(#{sample=>1}));
probe(case_23)->guard_probe(opaque(#{sample=>1}));
probe(case_24)->erlang:apply(erlang,opaque(is_record),[opaque(#native{})]);
probe(case_25)->erlang:is_record(opaque(#native{}));
probe(case_26)->guard_probe(opaque(#native{}));
probe(case_27)->erlang:apply(erlang,opaque(is_record),[opaque(#native{x=99})]);
probe(case_28)->erlang:is_record(opaque(#native{x=99}));
probe(case_29)->guard_probe(opaque(#native{x=99}));
probe(case_30)->erlang:apply(erlang,opaque(is_record),[opaque(binary_to_term(<<131,88,119,14,"oracle@fixture",0,0,0,5,0,0,0,2,0,0,0,7>>))]);
probe(case_31)->erlang:is_record(opaque(binary_to_term(<<131,88,119,14,"oracle@fixture",0,0,0,5,0,0,0,2,0,0,0,7>>)));
probe(case_32)->guard_probe(opaque(binary_to_term(<<131,88,119,14,"oracle@fixture",0,0,0,5,0,0,0,2,0,0,0,7>>)));
probe(case_33)->erlang:apply(erlang,opaque(is_record),[opaque(binary_to_term(<<131,89,119,14,"oracle@fixture",0,0,0,5,0,0,0,7>>))]);
probe(case_34)->erlang:is_record(opaque(binary_to_term(<<131,89,119,14,"oracle@fixture",0,0,0,5,0,0,0,7>>)));
probe(case_35)->guard_probe(opaque(binary_to_term(<<131,89,119,14,"oracle@fixture",0,0,0,5,0,0,0,7>>)));
probe(case_36)->erlang:apply(erlang,opaque(is_record),[opaque(binary_to_term(<<131,114,0,3,119,14,"oracle@fixture",7,0,0,0,1,0,0,0,2,0,0,0,3>>))]);
probe(case_37)->erlang:is_record(opaque(binary_to_term(<<131,114,0,3,119,14,"oracle@fixture",7,0,0,0,1,0,0,0,2,0,0,0,3>>)));
probe(case_38)->guard_probe(opaque(binary_to_term(<<131,114,0,3,119,14,"oracle@fixture",7,0,0,0,1,0,0,0,2,0,0,0,3>>)));
probe(case_39)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[],#{is_exported=>false}))]);
probe(case_40)->erlang:is_record(opaque(records:create(record496,sample,[],#{is_exported=>false})));
probe(case_41)->guard_probe(opaque(records:create(record496,sample,[],#{is_exported=>false})));
probe(case_42)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[],#{is_exported=>true}))]);
probe(case_43)->erlang:is_record(opaque(records:create(record496,sample,[],#{is_exported=>true})));
probe(case_44)->guard_probe(opaque(records:create(record496,sample,[],#{is_exported=>true})));
probe(case_45)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false}))]);
probe(case_46)->erlang:is_record(opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})));
probe(case_47)->guard_probe(opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})));
probe(case_48)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>true}))]);
probe(case_49)->erlang:is_record(opaque(records:create(record496,sample,[{x,1}],#{is_exported=>true})));
probe(case_50)->guard_probe(opaque(records:create(record496,sample,[{x,1}],#{is_exported=>true})));
probe(case_51)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{y,2},{x,1}],#{is_exported=>false}))]);
probe(case_52)->erlang:is_record(opaque(records:create(record496,sample,[{y,2},{x,1}],#{is_exported=>false})));
probe(case_53)->guard_probe(opaque(records:create(record496,sample,[{y,2},{x,1}],#{is_exported=>false})));
probe(case_54)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{y,2},{x,1}],#{is_exported=>true}))]);
probe(case_55)->erlang:is_record(opaque(records:create(record496,sample,[{y,2},{x,1}],#{is_exported=>true})));
probe(case_56)->guard_probe(opaque(records:create(record496,sample,[{y,2},{x,1}],#{is_exported=>true})));
probe(case_57)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{list_to_atom("f"++integer_to_list(I)),I}||I<-lists:seq(1,33)],#{is_exported=>false}))]);
probe(case_58)->erlang:is_record(opaque(records:create(record496,sample,[{list_to_atom("f"++integer_to_list(I)),I}||I<-lists:seq(1,33)],#{is_exported=>false})));
probe(case_59)->guard_probe(opaque(records:create(record496,sample,[{list_to_atom("f"++integer_to_list(I)),I}||I<-lists:seq(1,33)],#{is_exported=>false}))).
guard_probe(V) when erlang:is_record(V)->true;guard_probe(_)->false.
opaque(V)->put(record_contract496_input,V),erase(record_contract496_input).
