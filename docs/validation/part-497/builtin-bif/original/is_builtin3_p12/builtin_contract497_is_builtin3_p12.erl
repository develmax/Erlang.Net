-module(builtin_contract497_is_builtin3_p12).
-export([run/0,run_stacks/0,probe/1]).
run()->ordsets_core_support:run(?MODULE,cases()).
run_stacks()->ordsets_core_support:profiles(?MODULE,cases()).
cases()->[case_0,case_1,case_2,case_3,case_4,case_5,case_6,case_7,case_8,case_9,case_10,case_11,case_12,case_13,case_14,case_15,case_16,case_17,case_18,case_19,case_20,case_21,case_22,case_23,case_24,case_25,case_26,case_27,case_28,case_29,case_30,case_31,case_32,case_33,case_34,case_35,case_36,case_37,case_38,case_39,case_40,case_41,case_42,case_43,case_44,case_45,case_46,case_47,case_48,case_49,case_50,case_51,case_52,case_53,case_54,case_55,case_56,case_57,case_58,case_59].
probe(case_0)->erlang:apply(erlang,opaque(is_builtin),[opaque(-0.0),opaque(abs),opaque(1)]);
probe(case_1)->erlang:apply(erlang,opaque(is_builtin),[opaque([]),opaque(abs),opaque(1)]);
probe(case_2)->erlang:apply(erlang,opaque(is_builtin),[opaque([1,2]),opaque(abs),opaque(1)]);
probe(case_3)->erlang:apply(erlang,opaque(is_builtin),[opaque([1|tail]),opaque(abs),opaque(1)]);
probe(case_4)->erlang:apply(erlang,opaque(is_builtin),[opaque({}),opaque(abs),opaque(1)]);
probe(case_5)->erlang:apply(erlang,opaque(is_builtin),[opaque({erlang}),opaque(abs),opaque(1)]);
probe(case_6)->erlang:apply(erlang,opaque(is_builtin),[opaque(#{}),opaque(abs),opaque(1)]);
probe(case_7)->erlang:apply(erlang,opaque(is_builtin),[opaque(#{a=>1}),opaque(abs),opaque(1)]);
probe(case_8)->erlang:apply(erlang,opaque(is_builtin),[opaque(<<>>),opaque(abs),opaque(1)]);
probe(case_9)->erlang:apply(erlang,opaque(is_builtin),[opaque(<<1:1>>),opaque(abs),opaque(1)]);
probe(case_10)->erlang:apply(erlang,opaque(is_builtin),[opaque(fun erlang:abs/1),opaque(abs),opaque(1)]);
probe(case_11)->erlang:apply(erlang,opaque(is_builtin),[opaque(fun(X)->X end),opaque(abs),opaque(1)]);
probe(case_12)->erlang:apply(erlang,opaque(is_builtin),[opaque(list_to_pid("<0.88.0>")),opaque(abs),opaque(1)]);
probe(case_13)->erlang:apply(erlang,opaque(is_builtin),[opaque(binary_to_term(<<131,89,119,14,"oracle@fixture",0,0,0,5,0,0,0,7>>)),opaque(abs),opaque(1)]);
probe(case_14)->erlang:apply(erlang,opaque(is_builtin),[opaque(records:create(test,a,[{x,1}],#{is_exported=>false})),opaque(abs),opaque(1)]);
probe(case_15)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(0),opaque(1)]);
probe(case_16)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(1 bsl 128),opaque(1)]);
probe(case_17)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(0.0),opaque(1)]);
probe(case_18)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(-0.0),opaque(1)]);
probe(case_19)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque([]),opaque(1)]);
probe(case_20)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque([1,2]),opaque(1)]);
probe(case_21)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque([1|tail]),opaque(1)]);
probe(case_22)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque({}),opaque(1)]);
probe(case_23)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque({erlang}),opaque(1)]);
probe(case_24)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(#{}),opaque(1)]);
probe(case_25)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(#{a=>1}),opaque(1)]);
probe(case_26)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(<<>>),opaque(1)]);
probe(case_27)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(<<1:1>>),opaque(1)]);
probe(case_28)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(fun erlang:abs/1),opaque(1)]);
probe(case_29)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(fun(X)->X end),opaque(1)]);
probe(case_30)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(list_to_pid("<0.88.0>")),opaque(1)]);
probe(case_31)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(binary_to_term(<<131,89,119,14,"oracle@fixture",0,0,0,5,0,0,0,7>>)),opaque(1)]);
probe(case_32)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(records:create(test,a,[{x,1}],#{is_exported=>false})),opaque(1)]);
probe(case_33)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque(0)]);
probe(case_34)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque(1 bsl 128)]);
probe(case_35)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque(0.0)]);
probe(case_36)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque(-0.0)]);
probe(case_37)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque([])]);
probe(case_38)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque([1,2])]);
probe(case_39)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque([1|tail])]);
probe(case_40)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque({})]);
probe(case_41)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque({erlang})]);
probe(case_42)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque(#{})]);
probe(case_43)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque(#{a=>1})]);
probe(case_44)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque(<<>>)]);
probe(case_45)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque(<<1:1>>)]);
probe(case_46)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque(fun erlang:abs/1)]);
probe(case_47)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque(fun(X)->X end)]);
probe(case_48)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque(list_to_pid("<0.88.0>"))]);
probe(case_49)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque(binary_to_term(<<131,89,119,14,"oracle@fixture",0,0,0,5,0,0,0,7>>))]);
probe(case_50)->erlang:apply(erlang,opaque(is_builtin),[opaque(erlang),opaque(abs),opaque(records:create(test,a,[{x,1}],#{is_exported=>false}))]);
probe(case_51)->erlang:is_builtin(opaque('erlang'),opaque('not_a_bif497'),opaque(0));
probe(case_52)->erlang:is_builtin(opaque('erlang'),opaque('not_a_bif497'),opaque(1));
probe(case_53)->erlang:is_builtin(opaque('erlang'),opaque('not_a_bif497'),opaque(2));
probe(case_54)->erlang:is_builtin(opaque('erlang'),opaque('not_a_bif497'),opaque(3));
probe(case_55)->erlang:is_builtin(opaque(''),opaque(''),opaque(0));
probe(case_56)->erlang:is_builtin(opaque(''),opaque(''),opaque(1));
probe(case_57)->erlang:is_builtin(opaque(''),opaque(''),opaque(2));
probe(case_58)->erlang:is_builtin(opaque(''),opaque(''),opaque(3));
probe(case_59)->erlang:is_builtin(opaque('модуль'),opaque('функция'),opaque(0)).
opaque(V)->put(builtin_contract497_input,V),erase(builtin_contract497_input).
