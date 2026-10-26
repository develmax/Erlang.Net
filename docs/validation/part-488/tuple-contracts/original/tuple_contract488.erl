-module(tuple_contract488).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [size_0,
            size_1,
            size_2,
            size_3,
            size_4,
            size_5,
            size_6,
            size_7,
            size_8,
            size_9,
            size_10,
            size_11,
            size_12,
            size_13,
            size_14,
            size_15,
            size_16,
            size_tuple_2,
            size_tuple_33,
            size_tuple_257,
            size_tuple_4096,
            tuple_to_list_0,
            tuple_to_list_1,
            tuple_to_list_2,
            tuple_to_list_3,
            tuple_to_list_4,
            tuple_to_list_5,
            tuple_to_list_6,
            tuple_to_list_7,
            tuple_to_list_8,
            tuple_to_list_9,
            tuple_to_list_10,
            tuple_to_list_11,
            tuple_to_list_12,
            tuple_to_list_13,
            tuple_to_list_14,
            tuple_to_list_15,
            tuple_to_list_16,
            tuple_to_list_tuple_2,
            tuple_to_list_tuple_33,
            tuple_to_list_tuple_257,
            tuple_to_list_tuple_4096,
            element_42,
            element_43,
            element_44,
            element_45,
            element_46,
            element_47,
            element_48,
            element_49,
            element_50,
            element_51,
            element_52,
            element_53,
            element_54,
            element_55,
            element_56,
            element_57,
            element_58,
            element_59,
            element_60,
            element_61,
            element_62,
            element_63,
            element_64,
            element_65,
            element_66,
            element_67,
            element_68,
            element_69,
            element_70,
            element_71,
            element_72,
            element_73,
            element_74,
            element_75,
            element_76,
            element_77,
            element_78,
            element_79,
            element_80,
            element_81,
            element_82,
            element_83,
            element_84,
            element_85,
            element_86,
            element_87,
            element_88,
            element_89,
            element_90,
            element_91,
            element_92,
            element_93,
            element_94,
            element_95,
            element_96,
            element_97,
            element_98,
            element_99,
            element_100,
            element_101,
            element_102,
            element_103,
            element_104,
            element_105,
            element_106,
            element_107,
            element_identity,
            tuple_to_list_identity,
            tuple_to_list_fresh_spines].
probe(size_0) -> erlang:apply(erlang,opaque(size),[a]);
probe(size_1) -> erlang:apply(erlang,opaque(size),[0]);
probe(size_2) -> erlang:apply(erlang,opaque(size),[1.0]);
probe(size_3) -> erlang:apply(erlang,opaque(size),[[]]);
probe(size_4) -> erlang:apply(erlang,opaque(size),[[a]]);
probe(size_5) -> erlang:apply(erlang,opaque(size),[[a|tail]]);
probe(size_6) -> erlang:apply(erlang,opaque(size),[{}]);
probe(size_7) -> erlang:apply(erlang,opaque(size),[{a}]);
probe(size_8) -> erlang:apply(erlang,opaque(size),[{a,b}]);
probe(size_9) -> erlang:apply(erlang,opaque(size),[#{}]);
probe(size_10) -> erlang:apply(erlang,opaque(size),[<<>>]);
probe(size_11) -> erlang:apply(erlang,opaque(size),[<<1:1>>]);
probe(size_12) -> erlang:apply(erlang,opaque(size),[<<1:9>>]);
probe(size_13) -> erlang:apply(erlang,opaque(size),[fun erlang:abs/1]);
probe(size_14) -> erlang:apply(erlang,opaque(size),[binary_to_term(<<131,103,100,0,6,"oracle",0,0,0,1,0,0,0,0,0>>)]);
probe(size_15) -> erlang:apply(erlang,opaque(size),[binary_to_term(<<131,101,100,0,6,"oracle",0,0,0,1,0>>)]);
probe(size_16) -> erlang:apply(erlang,opaque(size),[binary_to_term(<<131,102,100,0,6,"oracle",0,0,0,1,0>>)]);
probe(size_tuple_2) -> erlang:apply(erlang,opaque(size),[erlang:make_tuple(2,{a,<<1:9>>})]);
probe(size_tuple_33) -> erlang:apply(erlang,opaque(size),[erlang:make_tuple(33,{a,<<1:9>>})]);
probe(size_tuple_257) -> erlang:apply(erlang,opaque(size),[erlang:make_tuple(257,{a,<<1:9>>})]);
probe(size_tuple_4096) -> erlang:apply(erlang,opaque(size),[erlang:make_tuple(4096,{a,<<1:9>>})]);
probe(tuple_to_list_0) -> erlang:apply(erlang,opaque(tuple_to_list),[a]);
probe(tuple_to_list_1) -> erlang:apply(erlang,opaque(tuple_to_list),[0]);
probe(tuple_to_list_2) -> erlang:apply(erlang,opaque(tuple_to_list),[1.0]);
probe(tuple_to_list_3) -> erlang:apply(erlang,opaque(tuple_to_list),[[]]);
probe(tuple_to_list_4) -> erlang:apply(erlang,opaque(tuple_to_list),[[a]]);
probe(tuple_to_list_5) -> erlang:apply(erlang,opaque(tuple_to_list),[[a|tail]]);
probe(tuple_to_list_6) -> erlang:apply(erlang,opaque(tuple_to_list),[{}]);
probe(tuple_to_list_7) -> erlang:apply(erlang,opaque(tuple_to_list),[{a}]);
probe(tuple_to_list_8) -> erlang:apply(erlang,opaque(tuple_to_list),[{a,b}]);
probe(tuple_to_list_9) -> erlang:apply(erlang,opaque(tuple_to_list),[#{}]);
probe(tuple_to_list_10) -> erlang:apply(erlang,opaque(tuple_to_list),[<<>>]);
probe(tuple_to_list_11) -> erlang:apply(erlang,opaque(tuple_to_list),[<<1:1>>]);
probe(tuple_to_list_12) -> erlang:apply(erlang,opaque(tuple_to_list),[<<1:9>>]);
probe(tuple_to_list_13) -> erlang:apply(erlang,opaque(tuple_to_list),[fun erlang:abs/1]);
probe(tuple_to_list_14) -> erlang:apply(erlang,opaque(tuple_to_list),[binary_to_term(<<131,103,100,0,6,"oracle",0,0,0,1,0,0,0,0,0>>)]);
probe(tuple_to_list_15) -> erlang:apply(erlang,opaque(tuple_to_list),[binary_to_term(<<131,101,100,0,6,"oracle",0,0,0,1,0>>)]);
probe(tuple_to_list_16) -> erlang:apply(erlang,opaque(tuple_to_list),[binary_to_term(<<131,102,100,0,6,"oracle",0,0,0,1,0>>)]);
probe(tuple_to_list_tuple_2) -> erlang:apply(erlang,opaque(tuple_to_list),[erlang:make_tuple(2,{a,<<1:9>>})]);
probe(tuple_to_list_tuple_33) -> erlang:apply(erlang,opaque(tuple_to_list),[erlang:make_tuple(33,{a,<<1:9>>})]);
probe(tuple_to_list_tuple_257) -> erlang:apply(erlang,opaque(tuple_to_list),[erlang:make_tuple(257,{a,<<1:9>>})]);
probe(tuple_to_list_tuple_4096) -> erlang:apply(erlang,opaque(tuple_to_list),[erlang:make_tuple(4096,{a,<<1:9>>})]);
probe(element_42) -> erlang:apply(erlang,opaque(element),[-1,{}]);
probe(element_43) -> erlang:apply(erlang,opaque(element),[-1,{a}]);
probe(element_44) -> erlang:apply(erlang,opaque(element),[-1,{a,b}]);
probe(element_45) -> erlang:apply(erlang,opaque(element),[-1,[a]]);
probe(element_46) -> erlang:apply(erlang,opaque(element),[-1,#{}]);
probe(element_47) -> erlang:apply(erlang,opaque(element),[-1,<<>>]);
probe(element_48) -> erlang:apply(erlang,opaque(element),[0,{}]);
probe(element_49) -> erlang:apply(erlang,opaque(element),[0,{a}]);
probe(element_50) -> erlang:apply(erlang,opaque(element),[0,{a,b}]);
probe(element_51) -> erlang:apply(erlang,opaque(element),[0,[a]]);
probe(element_52) -> erlang:apply(erlang,opaque(element),[0,#{}]);
probe(element_53) -> erlang:apply(erlang,opaque(element),[0,<<>>]);
probe(element_54) -> erlang:apply(erlang,opaque(element),[1,{}]);
probe(element_55) -> erlang:apply(erlang,opaque(element),[1,{a}]);
probe(element_56) -> erlang:apply(erlang,opaque(element),[1,{a,b}]);
probe(element_57) -> erlang:apply(erlang,opaque(element),[1,[a]]);
probe(element_58) -> erlang:apply(erlang,opaque(element),[1,#{}]);
probe(element_59) -> erlang:apply(erlang,opaque(element),[1,<<>>]);
probe(element_60) -> erlang:apply(erlang,opaque(element),[2,{}]);
probe(element_61) -> erlang:apply(erlang,opaque(element),[2,{a}]);
probe(element_62) -> erlang:apply(erlang,opaque(element),[2,{a,b}]);
probe(element_63) -> erlang:apply(erlang,opaque(element),[2,[a]]);
probe(element_64) -> erlang:apply(erlang,opaque(element),[2,#{}]);
probe(element_65) -> erlang:apply(erlang,opaque(element),[2,<<>>]);
probe(element_66) -> erlang:apply(erlang,opaque(element),[3,{}]);
probe(element_67) -> erlang:apply(erlang,opaque(element),[3,{a}]);
probe(element_68) -> erlang:apply(erlang,opaque(element),[3,{a,b}]);
probe(element_69) -> erlang:apply(erlang,opaque(element),[3,[a]]);
probe(element_70) -> erlang:apply(erlang,opaque(element),[3,#{}]);
probe(element_71) -> erlang:apply(erlang,opaque(element),[3,<<>>]);
probe(element_72) -> erlang:apply(erlang,opaque(element),[2147483648,{}]);
probe(element_73) -> erlang:apply(erlang,opaque(element),[2147483648,{a}]);
probe(element_74) -> erlang:apply(erlang,opaque(element),[2147483648,{a,b}]);
probe(element_75) -> erlang:apply(erlang,opaque(element),[2147483648,[a]]);
probe(element_76) -> erlang:apply(erlang,opaque(element),[2147483648,#{}]);
probe(element_77) -> erlang:apply(erlang,opaque(element),[2147483648,<<>>]);
probe(element_78) -> erlang:apply(erlang,opaque(element),[18446744073709551616,{}]);
probe(element_79) -> erlang:apply(erlang,opaque(element),[18446744073709551616,{a}]);
probe(element_80) -> erlang:apply(erlang,opaque(element),[18446744073709551616,{a,b}]);
probe(element_81) -> erlang:apply(erlang,opaque(element),[18446744073709551616,[a]]);
probe(element_82) -> erlang:apply(erlang,opaque(element),[18446744073709551616,#{}]);
probe(element_83) -> erlang:apply(erlang,opaque(element),[18446744073709551616,<<>>]);
probe(element_84) -> erlang:apply(erlang,opaque(element),[-18446744073709551616,{}]);
probe(element_85) -> erlang:apply(erlang,opaque(element),[-18446744073709551616,{a}]);
probe(element_86) -> erlang:apply(erlang,opaque(element),[-18446744073709551616,{a,b}]);
probe(element_87) -> erlang:apply(erlang,opaque(element),[-18446744073709551616,[a]]);
probe(element_88) -> erlang:apply(erlang,opaque(element),[-18446744073709551616,#{}]);
probe(element_89) -> erlang:apply(erlang,opaque(element),[-18446744073709551616,<<>>]);
probe(element_90) -> erlang:apply(erlang,opaque(element),[1.0,{}]);
probe(element_91) -> erlang:apply(erlang,opaque(element),[1.0,{a}]);
probe(element_92) -> erlang:apply(erlang,opaque(element),[1.0,{a,b}]);
probe(element_93) -> erlang:apply(erlang,opaque(element),[1.0,[a]]);
probe(element_94) -> erlang:apply(erlang,opaque(element),[1.0,#{}]);
probe(element_95) -> erlang:apply(erlang,opaque(element),[1.0,<<>>]);
probe(element_96) -> erlang:apply(erlang,opaque(element),[a,{}]);
probe(element_97) -> erlang:apply(erlang,opaque(element),[a,{a}]);
probe(element_98) -> erlang:apply(erlang,opaque(element),[a,{a,b}]);
probe(element_99) -> erlang:apply(erlang,opaque(element),[a,[a]]);
probe(element_100) -> erlang:apply(erlang,opaque(element),[a,#{}]);
probe(element_101) -> erlang:apply(erlang,opaque(element),[a,<<>>]);
probe(element_102) -> erlang:apply(erlang,opaque(element),[[],{}]);
probe(element_103) -> erlang:apply(erlang,opaque(element),[[],{a}]);
probe(element_104) -> erlang:apply(erlang,opaque(element),[[],{a,b}]);
probe(element_105) -> erlang:apply(erlang,opaque(element),[[],[a]]);
probe(element_106) -> erlang:apply(erlang,opaque(element),[[],#{}]);
probe(element_107) -> erlang:apply(erlang,opaque(element),[[],<<>>]);
probe(element_identity) -> T=opaque({<<1:9>>,fun erlang:abs/1}), V=erlang:apply(erlang,opaque(element),[1,T]), erts_debug:same(V,element(1,T));
probe(tuple_to_list_identity) -> T=opaque({<<1:9>>,fun erlang:abs/1}), L=erlang:apply(erlang,opaque(tuple_to_list),[T]), erts_debug:same(hd(L),element(1,T));
probe(tuple_to_list_fresh_spines) -> T=opaque({a,b}), L=erlang:apply(erlang,opaque(tuple_to_list),[T]), R=erlang:apply(erlang,opaque(tuple_to_list),[T]), erts_debug:same(L,R).
opaque(Value) -> put(tuple_contract488_input,Value),erase(tuple_contract488_input).
