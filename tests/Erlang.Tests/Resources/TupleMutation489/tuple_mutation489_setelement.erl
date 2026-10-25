-module(tuple_mutation489_setelement).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [setelement_0,
            setelement_1,
            setelement_2,
            setelement_3,
            setelement_4,
            setelement_5,
            setelement_6,
            setelement_7,
            setelement_8,
            setelement_9,
            setelement_10,
            setelement_11,
            setelement_12,
            setelement_13,
            setelement_14,
            setelement_15,
            setelement_16,
            setelement_17,
            setelement_18,
            setelement_19,
            setelement_20,
            setelement_21,
            setelement_22,
            setelement_23,
            setelement_24,
            setelement_25,
            setelement_26,
            setelement_27,
            setelement_28,
            setelement_29,
            setelement_30,
            setelement_31,
            setelement_32,
            setelement_33,
            setelement_34,
            setelement_35,
            setelement_36,
            setelement_37,
            setelement_38,
            setelement_39,
            setelement_40,
            setelement_41,
            setelement_42,
            setelement_43,
            setelement_44,
            setelement_45,
            setelement_46,
            setelement_47,
            setelement_48,
            setelement_49,
            setelement_50,
            setelement_51,
            setelement_52,
            setelement_53,
            setelement_54,
            setelement_55,
            setelement_56,
            setelement_57,
            setelement_58,
            setelement_59,
            setelement_60,
            setelement_61,
            setelement_62,
            setelement_63,
            setelement_64,
            setelement_65,
            setelement_66,
            setelement_67,
            setelement_68,
            setelement_69,
            setelement_70,
            setelement_71,
            setelement_72,
            setelement_73,
            setelement_74,
            setelement_75,
            setelement_76,
            setelement_77,
            setelement_78,
            setelement_79].
probe(setelement_0) -> erlang:apply(erlang,opaque(setelement),[-18446744073709551616,{},{new,<<1:9>>}]);
probe(setelement_1) -> erlang:apply(erlang,opaque(setelement),[-18446744073709551616,{a},{new,<<1:9>>}]);
probe(setelement_2) -> erlang:apply(erlang,opaque(setelement),[-18446744073709551616,{a,b},{new,<<1:9>>}]);
probe(setelement_3) -> erlang:apply(erlang,opaque(setelement),[-18446744073709551616,{a,b,c},{new,<<1:9>>}]);
probe(setelement_4) -> erlang:apply(erlang,opaque(setelement),[-18446744073709551616,a,{new,<<1:9>>}]);
probe(setelement_5) -> erlang:apply(erlang,opaque(setelement),[-18446744073709551616,[],{new,<<1:9>>}]);
probe(setelement_6) -> erlang:apply(erlang,opaque(setelement),[-18446744073709551616,#{},{new,<<1:9>>}]);
probe(setelement_7) -> erlang:apply(erlang,opaque(setelement),[-18446744073709551616,<<>>,{new,<<1:9>>}]);
probe(setelement_8) -> erlang:apply(erlang,opaque(setelement),[-1,{},{new,<<1:9>>}]);
probe(setelement_9) -> erlang:apply(erlang,opaque(setelement),[-1,{a},{new,<<1:9>>}]);
probe(setelement_10) -> erlang:apply(erlang,opaque(setelement),[-1,{a,b},{new,<<1:9>>}]);
probe(setelement_11) -> erlang:apply(erlang,opaque(setelement),[-1,{a,b,c},{new,<<1:9>>}]);
probe(setelement_12) -> erlang:apply(erlang,opaque(setelement),[-1,a,{new,<<1:9>>}]);
probe(setelement_13) -> erlang:apply(erlang,opaque(setelement),[-1,[],{new,<<1:9>>}]);
probe(setelement_14) -> erlang:apply(erlang,opaque(setelement),[-1,#{},{new,<<1:9>>}]);
probe(setelement_15) -> erlang:apply(erlang,opaque(setelement),[-1,<<>>,{new,<<1:9>>}]);
probe(setelement_16) -> erlang:apply(erlang,opaque(setelement),[0,{},{new,<<1:9>>}]);
probe(setelement_17) -> erlang:apply(erlang,opaque(setelement),[0,{a},{new,<<1:9>>}]);
probe(setelement_18) -> erlang:apply(erlang,opaque(setelement),[0,{a,b},{new,<<1:9>>}]);
probe(setelement_19) -> erlang:apply(erlang,opaque(setelement),[0,{a,b,c},{new,<<1:9>>}]);
probe(setelement_20) -> erlang:apply(erlang,opaque(setelement),[0,a,{new,<<1:9>>}]);
probe(setelement_21) -> erlang:apply(erlang,opaque(setelement),[0,[],{new,<<1:9>>}]);
probe(setelement_22) -> erlang:apply(erlang,opaque(setelement),[0,#{},{new,<<1:9>>}]);
probe(setelement_23) -> erlang:apply(erlang,opaque(setelement),[0,<<>>,{new,<<1:9>>}]);
probe(setelement_24) -> erlang:apply(erlang,opaque(setelement),[1,{},{new,<<1:9>>}]);
probe(setelement_25) -> erlang:apply(erlang,opaque(setelement),[1,{a},{new,<<1:9>>}]);
probe(setelement_26) -> erlang:apply(erlang,opaque(setelement),[1,{a,b},{new,<<1:9>>}]);
probe(setelement_27) -> erlang:apply(erlang,opaque(setelement),[1,{a,b,c},{new,<<1:9>>}]);
probe(setelement_28) -> erlang:apply(erlang,opaque(setelement),[1,a,{new,<<1:9>>}]);
probe(setelement_29) -> erlang:apply(erlang,opaque(setelement),[1,[],{new,<<1:9>>}]);
probe(setelement_30) -> erlang:apply(erlang,opaque(setelement),[1,#{},{new,<<1:9>>}]);
probe(setelement_31) -> erlang:apply(erlang,opaque(setelement),[1,<<>>,{new,<<1:9>>}]);
probe(setelement_32) -> erlang:apply(erlang,opaque(setelement),[2,{},{new,<<1:9>>}]);
probe(setelement_33) -> erlang:apply(erlang,opaque(setelement),[2,{a},{new,<<1:9>>}]);
probe(setelement_34) -> erlang:apply(erlang,opaque(setelement),[2,{a,b},{new,<<1:9>>}]);
probe(setelement_35) -> erlang:apply(erlang,opaque(setelement),[2,{a,b,c},{new,<<1:9>>}]);
probe(setelement_36) -> erlang:apply(erlang,opaque(setelement),[2,a,{new,<<1:9>>}]);
probe(setelement_37) -> erlang:apply(erlang,opaque(setelement),[2,[],{new,<<1:9>>}]);
probe(setelement_38) -> erlang:apply(erlang,opaque(setelement),[2,#{},{new,<<1:9>>}]);
probe(setelement_39) -> erlang:apply(erlang,opaque(setelement),[2,<<>>,{new,<<1:9>>}]);
probe(setelement_40) -> erlang:apply(erlang,opaque(setelement),[3,{},{new,<<1:9>>}]);
probe(setelement_41) -> erlang:apply(erlang,opaque(setelement),[3,{a},{new,<<1:9>>}]);
probe(setelement_42) -> erlang:apply(erlang,opaque(setelement),[3,{a,b},{new,<<1:9>>}]);
probe(setelement_43) -> erlang:apply(erlang,opaque(setelement),[3,{a,b,c},{new,<<1:9>>}]);
probe(setelement_44) -> erlang:apply(erlang,opaque(setelement),[3,a,{new,<<1:9>>}]);
probe(setelement_45) -> erlang:apply(erlang,opaque(setelement),[3,[],{new,<<1:9>>}]);
probe(setelement_46) -> erlang:apply(erlang,opaque(setelement),[3,#{},{new,<<1:9>>}]);
probe(setelement_47) -> erlang:apply(erlang,opaque(setelement),[3,<<>>,{new,<<1:9>>}]);
probe(setelement_48) -> erlang:apply(erlang,opaque(setelement),[4,{},{new,<<1:9>>}]);
probe(setelement_49) -> erlang:apply(erlang,opaque(setelement),[4,{a},{new,<<1:9>>}]);
probe(setelement_50) -> erlang:apply(erlang,opaque(setelement),[4,{a,b},{new,<<1:9>>}]);
probe(setelement_51) -> erlang:apply(erlang,opaque(setelement),[4,{a,b,c},{new,<<1:9>>}]);
probe(setelement_52) -> erlang:apply(erlang,opaque(setelement),[4,a,{new,<<1:9>>}]);
probe(setelement_53) -> erlang:apply(erlang,opaque(setelement),[4,[],{new,<<1:9>>}]);
probe(setelement_54) -> erlang:apply(erlang,opaque(setelement),[4,#{},{new,<<1:9>>}]);
probe(setelement_55) -> erlang:apply(erlang,opaque(setelement),[4,<<>>,{new,<<1:9>>}]);
probe(setelement_56) -> erlang:apply(erlang,opaque(setelement),[18446744073709551616,{},{new,<<1:9>>}]);
probe(setelement_57) -> erlang:apply(erlang,opaque(setelement),[18446744073709551616,{a},{new,<<1:9>>}]);
probe(setelement_58) -> erlang:apply(erlang,opaque(setelement),[18446744073709551616,{a,b},{new,<<1:9>>}]);
probe(setelement_59) -> erlang:apply(erlang,opaque(setelement),[18446744073709551616,{a,b,c},{new,<<1:9>>}]);
probe(setelement_60) -> erlang:apply(erlang,opaque(setelement),[18446744073709551616,a,{new,<<1:9>>}]);
probe(setelement_61) -> erlang:apply(erlang,opaque(setelement),[18446744073709551616,[],{new,<<1:9>>}]);
probe(setelement_62) -> erlang:apply(erlang,opaque(setelement),[18446744073709551616,#{},{new,<<1:9>>}]);
probe(setelement_63) -> erlang:apply(erlang,opaque(setelement),[18446744073709551616,<<>>,{new,<<1:9>>}]);
probe(setelement_64) -> erlang:apply(erlang,opaque(setelement),[a,{},{new,<<1:9>>}]);
probe(setelement_65) -> erlang:apply(erlang,opaque(setelement),[a,{a},{new,<<1:9>>}]);
probe(setelement_66) -> erlang:apply(erlang,opaque(setelement),[a,{a,b},{new,<<1:9>>}]);
probe(setelement_67) -> erlang:apply(erlang,opaque(setelement),[a,{a,b,c},{new,<<1:9>>}]);
probe(setelement_68) -> erlang:apply(erlang,opaque(setelement),[a,a,{new,<<1:9>>}]);
probe(setelement_69) -> erlang:apply(erlang,opaque(setelement),[a,[],{new,<<1:9>>}]);
probe(setelement_70) -> erlang:apply(erlang,opaque(setelement),[a,#{},{new,<<1:9>>}]);
probe(setelement_71) -> erlang:apply(erlang,opaque(setelement),[a,<<>>,{new,<<1:9>>}]);
probe(setelement_72) -> erlang:apply(erlang,opaque(setelement),[1.0,{},{new,<<1:9>>}]);
probe(setelement_73) -> erlang:apply(erlang,opaque(setelement),[1.0,{a},{new,<<1:9>>}]);
probe(setelement_74) -> erlang:apply(erlang,opaque(setelement),[1.0,{a,b},{new,<<1:9>>}]);
probe(setelement_75) -> erlang:apply(erlang,opaque(setelement),[1.0,{a,b,c},{new,<<1:9>>}]);
probe(setelement_76) -> erlang:apply(erlang,opaque(setelement),[1.0,a,{new,<<1:9>>}]);
probe(setelement_77) -> erlang:apply(erlang,opaque(setelement),[1.0,[],{new,<<1:9>>}]);
probe(setelement_78) -> erlang:apply(erlang,opaque(setelement),[1.0,#{},{new,<<1:9>>}]);
probe(setelement_79) -> erlang:apply(erlang,opaque(setelement),[1.0,<<>>,{new,<<1:9>>}]).
opaque(Value) -> put(tuple_mutation489_input,Value),erase(tuple_mutation489_input).
