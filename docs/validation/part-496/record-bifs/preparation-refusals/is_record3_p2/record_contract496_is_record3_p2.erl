-module(record_contract496_is_record3_p2).
-export([run/0,run_stacks/0,probe/1]).
-record #native{x=1}.
run()->ordsets_core_support:run(?MODULE,cases()).
run_stacks()->ordsets_core_support:profiles(?MODULE,cases()).
cases()->[case_0,case_1,case_2,case_3,case_4,case_5,case_6,case_7,case_8,case_9,case_10,case_11,case_12,case_13,case_14,case_15,case_16,case_17,case_18,case_19,case_20,case_21,case_22,case_23,case_24,case_25,case_26,case_27,case_28,case_29,case_30,case_31,case_32,case_33,case_34,case_35,case_36,case_37,case_38,case_39,case_40,case_41,case_42,case_43,case_44,case_45,case_46,case_47,case_48,case_49,case_50,case_51,case_52,case_53,case_54,case_55,case_56,case_57,case_58,case_59].
probe(case_0)->erlang:apply(erlang,opaque(is_record),[opaque({}),opaque(sample),opaque(3)]);
probe(case_1)->erlang:apply(erlang,opaque(is_record),[opaque({sample}),opaque(sample),opaque(3)]);
probe(case_2)->erlang:apply(erlang,opaque(is_record),[opaque({sample,1}),opaque(sample),opaque(3)]);
probe(case_3)->erlang:apply(erlang,opaque(is_record),[opaque({sample,1,2}),opaque(sample),opaque(3)]);
probe(case_4)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(sample),opaque(3)]);
probe(case_5)->erlang:apply(erlang,opaque(is_record),[opaque({}),opaque(sample),opaque(4)]);
probe(case_6)->erlang:apply(erlang,opaque(is_record),[opaque({sample}),opaque(sample),opaque(4)]);
probe(case_7)->erlang:apply(erlang,opaque(is_record),[opaque({sample,1}),opaque(sample),opaque(4)]);
probe(case_8)->erlang:apply(erlang,opaque(is_record),[opaque({sample,1,2}),opaque(sample),opaque(4)]);
probe(case_9)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(sample),opaque(4)]);
probe(case_10)->erlang:apply(erlang,opaque(is_record),[opaque({}),opaque(sample),opaque((1 bsl 59)-1)]);
probe(case_11)->erlang:apply(erlang,opaque(is_record),[opaque({sample}),opaque(sample),opaque((1 bsl 59)-1)]);
probe(case_12)->erlang:apply(erlang,opaque(is_record),[opaque({sample,1}),opaque(sample),opaque((1 bsl 59)-1)]);
probe(case_13)->erlang:apply(erlang,opaque(is_record),[opaque({sample,1,2}),opaque(sample),opaque((1 bsl 59)-1)]);
probe(case_14)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(sample),opaque((1 bsl 59)-1)]);
probe(case_15)->erlang:apply(erlang,opaque(is_record),[opaque({}),opaque(sample),opaque(1 bsl 59)]);
probe(case_16)->erlang:apply(erlang,opaque(is_record),[opaque({sample}),opaque(sample),opaque(1 bsl 59)]);
probe(case_17)->erlang:apply(erlang,opaque(is_record),[opaque({sample,1}),opaque(sample),opaque(1 bsl 59)]);
probe(case_18)->erlang:apply(erlang,opaque(is_record),[opaque({sample,1,2}),opaque(sample),opaque(1 bsl 59)]);
probe(case_19)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(sample),opaque(1 bsl 59)]);
probe(case_20)->erlang:apply(erlang,opaque(is_record),[opaque({}),opaque(sample),opaque(1 bsl 128)]);
probe(case_21)->erlang:apply(erlang,opaque(is_record),[opaque({sample}),opaque(sample),opaque(1 bsl 128)]);
probe(case_22)->erlang:apply(erlang,opaque(is_record),[opaque({sample,1}),opaque(sample),opaque(1 bsl 128)]);
probe(case_23)->erlang:apply(erlang,opaque(is_record),[opaque({sample,1,2}),opaque(sample),opaque(1 bsl 128)]);
probe(case_24)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(sample),opaque(1 bsl 128)]);
probe(case_25)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(0),opaque(2)]);
probe(case_26)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(0),opaque(sample)]);
probe(case_27)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(0),opaque([])]);
probe(case_28)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(1 bsl 128),opaque(2)]);
probe(case_29)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(1 bsl 128),opaque(sample)]);
probe(case_30)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(1 bsl 128),opaque([])]);
probe(case_31)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(1.0),opaque(2)]);
probe(case_32)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(1.0),opaque(sample)]);
probe(case_33)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(1.0),opaque([])]);
probe(case_34)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque([]),opaque(2)]);
probe(case_35)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque([]),opaque(sample)]);
probe(case_36)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque([]),opaque([])]);
probe(case_37)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque([tag]),opaque(2)]);
probe(case_38)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque([tag]),opaque(sample)]);
probe(case_39)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque([tag]),opaque([])]);
probe(case_40)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque({sample}),opaque(2)]);
probe(case_41)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque({sample}),opaque(sample)]);
probe(case_42)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque({sample}),opaque([])]);
probe(case_43)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(#{}),opaque(2)]);
probe(case_44)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(#{}),opaque(sample)]);
probe(case_45)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(#{}),opaque([])]);
probe(case_46)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(<<>>),opaque(2)]);
probe(case_47)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(<<>>),opaque(sample)]);
probe(case_48)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(<<>>),opaque([])]);
probe(case_49)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(<<1:1>>),opaque(2)]);
probe(case_50)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(<<1:1>>),opaque(sample)]);
probe(case_51)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(<<1:1>>),opaque([])]);
probe(case_52)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(fun erlang:abs/1),opaque(2)]);
probe(case_53)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(fun erlang:abs/1),opaque(sample)]);
probe(case_54)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(fun erlang:abs/1),opaque([])]);
probe(case_55)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(2)]);
probe(case_56)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(sample)]);
probe(case_57)->erlang:apply(erlang,opaque(is_record),[opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque(records:create(record496,sample,[{x,1}],#{is_exported=>false})),opaque([])]);
probe(case_58)->erlang:apply(erlang,opaque(is_record),[opaque(not_record),opaque(sample),opaque(0)]);
probe(case_59)->erlang:apply(erlang,opaque(is_record),[opaque({sample,1}),opaque(sample),opaque(0)]).
guard_probe(V,Tag,Size) when erlang:is_record(V,Tag,Size)->true;guard_probe(_,_,_)->false.
opaque(V)->put(record_contract496_input,V),erase(record_contract496_input).
