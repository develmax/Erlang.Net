-module(numeric_contract487).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [integer_constant_same,abs_same_big_positive,abs_same_float_positive,abs_same_float_zero,abs_same_float_negative_zero,float_same,abs_0,
            abs_1,
            abs_2,
            abs_3,
            abs_4,
            abs_5,
            abs_6,
            abs_7,
            abs_8,
            abs_9,
            abs_10,
            abs_11,
            abs_12,
            abs_13,
            abs_14,
            abs_15,
            abs_16,
            abs_17,
            abs_18,
            abs_19,
            abs_20,
            abs_21,
            abs_22,
            abs_23,
            abs_24,
            abs_25,
            abs_26,
            abs_27,
            abs_28,
            abs_29,
            abs_30,
            abs_31,
            abs_32,
            abs_33,
            abs_34,
            abs_35,
            abs_36,
            abs_37,
            abs_38,
            abs_39,
            abs_40,
            float_0,
            float_1,
            float_2,
            float_3,
            float_4,
            float_5,
            float_6,
            float_7,
            float_8,
            float_9,
            float_10,
            float_11,
            float_12,
            float_13,
            float_14,
            float_15,
            float_16,
            float_17,
            float_18,
            float_19,
            float_20,
            float_21,
            float_22,
            float_23,
            float_24,
            float_25,
            float_26,
            float_27,
            float_28,
            float_29,
            float_30,
            float_31,
            float_32,
            float_33,
            float_34,
            float_35,
            float_36,
            float_37,
            float_38,
            float_39,
            float_40,
            round_0,
            round_1,
            round_2,
            round_3,
            round_4,
            round_5,
            round_6,
            round_7,
            round_8,
            round_9,
            round_10,
            round_11,
            round_12,
            round_13,
            round_14,
            round_15,
            round_16,
            round_17,
            round_18,
            round_19,
            round_20,
            round_21,
            round_22,
            round_23,
            round_24,
            round_25,
            round_26,
            round_27,
            round_28,
            round_29,
            round_30,
            round_31,
            round_32,
            round_33,
            round_34,
            round_35,
            round_36,
            round_37,
            round_38,
            round_39,
            round_40,
            trunc_0,
            trunc_1,
            trunc_2,
            trunc_3,
            trunc_4,
            trunc_5,
            trunc_6,
            trunc_7,
            trunc_8,
            trunc_9,
            trunc_10,
            trunc_11,
            trunc_12,
            trunc_13,
            trunc_14,
            trunc_15,
            trunc_16,
            trunc_17,
            trunc_18,
            trunc_19,
            trunc_20,
            trunc_21,
            trunc_22,
            trunc_23,
            trunc_24,
            trunc_25,
            trunc_26,
            trunc_27,
            trunc_28,
            trunc_29,
            trunc_30,
            trunc_31,
            trunc_32,
            trunc_33,
            trunc_34,
            trunc_35,
            trunc_36,
            trunc_37,
            trunc_38,
            trunc_39,
            trunc_40,
            floor_0,
            floor_1,
            floor_2,
            floor_3,
            floor_4,
            floor_5,
            floor_6,
            floor_7,
            floor_8,
            floor_9,
            floor_10,
            floor_11,
            floor_12,
            floor_13,
            floor_14,
            floor_15,
            floor_16,
            floor_17,
            floor_18,
            floor_19,
            floor_20,
            floor_21,
            floor_22,
            floor_23,
            floor_24,
            floor_25,
            floor_26,
            floor_27,
            floor_28,
            floor_29,
            floor_30,
            floor_31,
            floor_32,
            floor_33,
            floor_34,
            floor_35,
            floor_36,
            floor_37,
            floor_38,
            floor_39,
            floor_40,
            ceil_0,
            ceil_1,
            ceil_2,
            ceil_3,
            ceil_4,
            ceil_5,
            ceil_6,
            ceil_7,
            ceil_8,
            ceil_9,
            ceil_10,
            ceil_11,
            ceil_12,
            ceil_13,
            ceil_14,
            ceil_15,
            ceil_16,
            ceil_17,
            ceil_18,
            ceil_19,
            ceil_20,
            ceil_21,
            ceil_22,
            ceil_23,
            ceil_24,
            ceil_25,
            ceil_26,
            ceil_27,
            ceil_28,
            ceil_29,
            ceil_30,
            ceil_31,
            ceil_32,
            ceil_33,
            ceil_34,
            ceil_35,
            ceil_36,
            ceil_37,
            ceil_38,
            ceil_39,
            ceil_40].
probe(abs_0) -> erlang:apply(erlang,opaque(abs),[0]);
probe(abs_1) -> erlang:apply(erlang,opaque(abs),[1]);
probe(abs_2) -> erlang:apply(erlang,opaque(abs),[-1]);
probe(abs_3) -> erlang:apply(erlang,opaque(abs),[9007199254740993]);
probe(abs_4) -> erlang:apply(erlang,opaque(abs),[9007199254740995]);
probe(abs_5) -> erlang:apply(erlang,opaque(abs),[-9007199254740993]);
probe(abs_6) -> erlang:apply(erlang,opaque(abs),[1 bsl 63]);
probe(abs_7) -> erlang:apply(erlang,opaque(abs),[-(1 bsl 63)]);
probe(abs_8) -> erlang:apply(erlang,opaque(abs),[1 bsl 1023]);
probe(abs_9) -> erlang:apply(erlang,opaque(abs),[(1 bsl 1024)-(1 bsl 971)]);
probe(abs_10) -> erlang:apply(erlang,opaque(abs),[(1 bsl 1024)-(1 bsl 970)]);
probe(abs_11) -> erlang:apply(erlang,opaque(abs),[1 bsl 1024]);
probe(abs_12) -> erlang:apply(erlang,opaque(abs),[1 bsl 4096]);
probe(abs_13) -> erlang:apply(erlang,opaque(abs),[0.0]);
probe(abs_14) -> erlang:apply(erlang,opaque(abs),[-0.0]);
probe(abs_15) -> erlang:apply(erlang,opaque(abs),[0.5]);
probe(abs_16) -> erlang:apply(erlang,opaque(abs),[-0.5]);
probe(abs_17) -> erlang:apply(erlang,opaque(abs),[0.49999999999999994]);
probe(abs_18) -> erlang:apply(erlang,opaque(abs),[-0.49999999999999994]);
probe(abs_19) -> erlang:apply(erlang,opaque(abs),[0.5000000000000001]);
probe(abs_20) -> erlang:apply(erlang,opaque(abs),[-0.5000000000000001]);
probe(abs_21) -> erlang:apply(erlang,opaque(abs),[1.5]);
probe(abs_22) -> erlang:apply(erlang,opaque(abs),[-1.5]);
probe(abs_23) -> erlang:apply(erlang,opaque(abs),[2.5]);
probe(abs_24) -> erlang:apply(erlang,opaque(abs),[-2.5]);
probe(abs_25) -> erlang:apply(erlang,opaque(abs),[1.0e20]);
probe(abs_26) -> erlang:apply(erlang,opaque(abs),[-1.0e20]);
probe(abs_27) -> erlang:apply(erlang,opaque(abs),[1.0e308]);
probe(abs_28) -> erlang:apply(erlang,opaque(abs),[-1.0e308]);
probe(abs_29) -> erlang:apply(erlang,opaque(abs),[5.0e-324]);
probe(abs_30) -> erlang:apply(erlang,opaque(abs),[-5.0e-324]);
probe(abs_31) -> erlang:apply(erlang,opaque(abs),[2.2250738585072014e-308]);
probe(abs_32) -> erlang:apply(erlang,opaque(abs),[-2.2250738585072014e-308]);
probe(abs_33) -> erlang:apply(erlang,opaque(abs),[a]);
probe(abs_34) -> erlang:apply(erlang,opaque(abs),[[]]);
probe(abs_35) -> erlang:apply(erlang,opaque(abs),[[a]]);
probe(abs_36) -> erlang:apply(erlang,opaque(abs),[[a|tail]]);
probe(abs_37) -> erlang:apply(erlang,opaque(abs),[{}]);
probe(abs_38) -> erlang:apply(erlang,opaque(abs),[#{}]);
probe(abs_39) -> erlang:apply(erlang,opaque(abs),[<<1:1>>]);
probe(abs_40) -> erlang:apply(erlang,opaque(abs),[fun erlang:abs/1]);
probe(float_0) -> erlang:apply(erlang,opaque(float),[0]);
probe(float_1) -> erlang:apply(erlang,opaque(float),[1]);
probe(float_2) -> erlang:apply(erlang,opaque(float),[-1]);
probe(float_3) -> erlang:apply(erlang,opaque(float),[9007199254740993]);
probe(float_4) -> erlang:apply(erlang,opaque(float),[9007199254740995]);
probe(float_5) -> erlang:apply(erlang,opaque(float),[-9007199254740993]);
probe(float_6) -> erlang:apply(erlang,opaque(float),[1 bsl 63]);
probe(float_7) -> erlang:apply(erlang,opaque(float),[-(1 bsl 63)]);
probe(float_8) -> erlang:apply(erlang,opaque(float),[1 bsl 1023]);
probe(float_9) -> erlang:apply(erlang,opaque(float),[(1 bsl 1024)-(1 bsl 971)]);
probe(float_10) -> erlang:apply(erlang,opaque(float),[(1 bsl 1024)-(1 bsl 970)]);
probe(float_11) -> erlang:apply(erlang,opaque(float),[1 bsl 1024]);
probe(float_12) -> erlang:apply(erlang,opaque(float),[1 bsl 4096]);
probe(float_13) -> erlang:apply(erlang,opaque(float),[0.0]);
probe(float_14) -> erlang:apply(erlang,opaque(float),[-0.0]);
probe(float_15) -> erlang:apply(erlang,opaque(float),[0.5]);
probe(float_16) -> erlang:apply(erlang,opaque(float),[-0.5]);
probe(float_17) -> erlang:apply(erlang,opaque(float),[0.49999999999999994]);
probe(float_18) -> erlang:apply(erlang,opaque(float),[-0.49999999999999994]);
probe(float_19) -> erlang:apply(erlang,opaque(float),[0.5000000000000001]);
probe(float_20) -> erlang:apply(erlang,opaque(float),[-0.5000000000000001]);
probe(float_21) -> erlang:apply(erlang,opaque(float),[1.5]);
probe(float_22) -> erlang:apply(erlang,opaque(float),[-1.5]);
probe(float_23) -> erlang:apply(erlang,opaque(float),[2.5]);
probe(float_24) -> erlang:apply(erlang,opaque(float),[-2.5]);
probe(float_25) -> erlang:apply(erlang,opaque(float),[1.0e20]);
probe(float_26) -> erlang:apply(erlang,opaque(float),[-1.0e20]);
probe(float_27) -> erlang:apply(erlang,opaque(float),[1.0e308]);
probe(float_28) -> erlang:apply(erlang,opaque(float),[-1.0e308]);
probe(float_29) -> erlang:apply(erlang,opaque(float),[5.0e-324]);
probe(float_30) -> erlang:apply(erlang,opaque(float),[-5.0e-324]);
probe(float_31) -> erlang:apply(erlang,opaque(float),[2.2250738585072014e-308]);
probe(float_32) -> erlang:apply(erlang,opaque(float),[-2.2250738585072014e-308]);
probe(float_33) -> erlang:apply(erlang,opaque(float),[a]);
probe(float_34) -> erlang:apply(erlang,opaque(float),[[]]);
probe(float_35) -> erlang:apply(erlang,opaque(float),[[a]]);
probe(float_36) -> erlang:apply(erlang,opaque(float),[[a|tail]]);
probe(float_37) -> erlang:apply(erlang,opaque(float),[{}]);
probe(float_38) -> erlang:apply(erlang,opaque(float),[#{}]);
probe(float_39) -> erlang:apply(erlang,opaque(float),[<<1:1>>]);
probe(float_40) -> erlang:apply(erlang,opaque(float),[fun erlang:abs/1]);
probe(round_0) -> erlang:apply(erlang,opaque(round),[0]);
probe(round_1) -> erlang:apply(erlang,opaque(round),[1]);
probe(round_2) -> erlang:apply(erlang,opaque(round),[-1]);
probe(round_3) -> erlang:apply(erlang,opaque(round),[9007199254740993]);
probe(round_4) -> erlang:apply(erlang,opaque(round),[9007199254740995]);
probe(round_5) -> erlang:apply(erlang,opaque(round),[-9007199254740993]);
probe(round_6) -> erlang:apply(erlang,opaque(round),[1 bsl 63]);
probe(round_7) -> erlang:apply(erlang,opaque(round),[-(1 bsl 63)]);
probe(round_8) -> erlang:apply(erlang,opaque(round),[1 bsl 1023]);
probe(round_9) -> erlang:apply(erlang,opaque(round),[(1 bsl 1024)-(1 bsl 971)]);
probe(round_10) -> erlang:apply(erlang,opaque(round),[(1 bsl 1024)-(1 bsl 970)]);
probe(round_11) -> erlang:apply(erlang,opaque(round),[1 bsl 1024]);
probe(round_12) -> erlang:apply(erlang,opaque(round),[1 bsl 4096]);
probe(round_13) -> erlang:apply(erlang,opaque(round),[0.0]);
probe(round_14) -> erlang:apply(erlang,opaque(round),[-0.0]);
probe(round_15) -> erlang:apply(erlang,opaque(round),[0.5]);
probe(round_16) -> erlang:apply(erlang,opaque(round),[-0.5]);
probe(round_17) -> erlang:apply(erlang,opaque(round),[0.49999999999999994]);
probe(round_18) -> erlang:apply(erlang,opaque(round),[-0.49999999999999994]);
probe(round_19) -> erlang:apply(erlang,opaque(round),[0.5000000000000001]);
probe(round_20) -> erlang:apply(erlang,opaque(round),[-0.5000000000000001]);
probe(round_21) -> erlang:apply(erlang,opaque(round),[1.5]);
probe(round_22) -> erlang:apply(erlang,opaque(round),[-1.5]);
probe(round_23) -> erlang:apply(erlang,opaque(round),[2.5]);
probe(round_24) -> erlang:apply(erlang,opaque(round),[-2.5]);
probe(round_25) -> erlang:apply(erlang,opaque(round),[1.0e20]);
probe(round_26) -> erlang:apply(erlang,opaque(round),[-1.0e20]);
probe(round_27) -> erlang:apply(erlang,opaque(round),[1.0e308]);
probe(round_28) -> erlang:apply(erlang,opaque(round),[-1.0e308]);
probe(round_29) -> erlang:apply(erlang,opaque(round),[5.0e-324]);
probe(round_30) -> erlang:apply(erlang,opaque(round),[-5.0e-324]);
probe(round_31) -> erlang:apply(erlang,opaque(round),[2.2250738585072014e-308]);
probe(round_32) -> erlang:apply(erlang,opaque(round),[-2.2250738585072014e-308]);
probe(round_33) -> erlang:apply(erlang,opaque(round),[a]);
probe(round_34) -> erlang:apply(erlang,opaque(round),[[]]);
probe(round_35) -> erlang:apply(erlang,opaque(round),[[a]]);
probe(round_36) -> erlang:apply(erlang,opaque(round),[[a|tail]]);
probe(round_37) -> erlang:apply(erlang,opaque(round),[{}]);
probe(round_38) -> erlang:apply(erlang,opaque(round),[#{}]);
probe(round_39) -> erlang:apply(erlang,opaque(round),[<<1:1>>]);
probe(round_40) -> erlang:apply(erlang,opaque(round),[fun erlang:abs/1]);
probe(trunc_0) -> erlang:apply(erlang,opaque(trunc),[0]);
probe(trunc_1) -> erlang:apply(erlang,opaque(trunc),[1]);
probe(trunc_2) -> erlang:apply(erlang,opaque(trunc),[-1]);
probe(trunc_3) -> erlang:apply(erlang,opaque(trunc),[9007199254740993]);
probe(trunc_4) -> erlang:apply(erlang,opaque(trunc),[9007199254740995]);
probe(trunc_5) -> erlang:apply(erlang,opaque(trunc),[-9007199254740993]);
probe(trunc_6) -> erlang:apply(erlang,opaque(trunc),[1 bsl 63]);
probe(trunc_7) -> erlang:apply(erlang,opaque(trunc),[-(1 bsl 63)]);
probe(trunc_8) -> erlang:apply(erlang,opaque(trunc),[1 bsl 1023]);
probe(trunc_9) -> erlang:apply(erlang,opaque(trunc),[(1 bsl 1024)-(1 bsl 971)]);
probe(trunc_10) -> erlang:apply(erlang,opaque(trunc),[(1 bsl 1024)-(1 bsl 970)]);
probe(trunc_11) -> erlang:apply(erlang,opaque(trunc),[1 bsl 1024]);
probe(trunc_12) -> erlang:apply(erlang,opaque(trunc),[1 bsl 4096]);
probe(trunc_13) -> erlang:apply(erlang,opaque(trunc),[0.0]);
probe(trunc_14) -> erlang:apply(erlang,opaque(trunc),[-0.0]);
probe(trunc_15) -> erlang:apply(erlang,opaque(trunc),[0.5]);
probe(trunc_16) -> erlang:apply(erlang,opaque(trunc),[-0.5]);
probe(trunc_17) -> erlang:apply(erlang,opaque(trunc),[0.49999999999999994]);
probe(trunc_18) -> erlang:apply(erlang,opaque(trunc),[-0.49999999999999994]);
probe(trunc_19) -> erlang:apply(erlang,opaque(trunc),[0.5000000000000001]);
probe(trunc_20) -> erlang:apply(erlang,opaque(trunc),[-0.5000000000000001]);
probe(trunc_21) -> erlang:apply(erlang,opaque(trunc),[1.5]);
probe(trunc_22) -> erlang:apply(erlang,opaque(trunc),[-1.5]);
probe(trunc_23) -> erlang:apply(erlang,opaque(trunc),[2.5]);
probe(trunc_24) -> erlang:apply(erlang,opaque(trunc),[-2.5]);
probe(trunc_25) -> erlang:apply(erlang,opaque(trunc),[1.0e20]);
probe(trunc_26) -> erlang:apply(erlang,opaque(trunc),[-1.0e20]);
probe(trunc_27) -> erlang:apply(erlang,opaque(trunc),[1.0e308]);
probe(trunc_28) -> erlang:apply(erlang,opaque(trunc),[-1.0e308]);
probe(trunc_29) -> erlang:apply(erlang,opaque(trunc),[5.0e-324]);
probe(trunc_30) -> erlang:apply(erlang,opaque(trunc),[-5.0e-324]);
probe(trunc_31) -> erlang:apply(erlang,opaque(trunc),[2.2250738585072014e-308]);
probe(trunc_32) -> erlang:apply(erlang,opaque(trunc),[-2.2250738585072014e-308]);
probe(trunc_33) -> erlang:apply(erlang,opaque(trunc),[a]);
probe(trunc_34) -> erlang:apply(erlang,opaque(trunc),[[]]);
probe(trunc_35) -> erlang:apply(erlang,opaque(trunc),[[a]]);
probe(trunc_36) -> erlang:apply(erlang,opaque(trunc),[[a|tail]]);
probe(trunc_37) -> erlang:apply(erlang,opaque(trunc),[{}]);
probe(trunc_38) -> erlang:apply(erlang,opaque(trunc),[#{}]);
probe(trunc_39) -> erlang:apply(erlang,opaque(trunc),[<<1:1>>]);
probe(trunc_40) -> erlang:apply(erlang,opaque(trunc),[fun erlang:abs/1]);
probe(floor_0) -> erlang:apply(erlang,opaque(floor),[0]);
probe(floor_1) -> erlang:apply(erlang,opaque(floor),[1]);
probe(floor_2) -> erlang:apply(erlang,opaque(floor),[-1]);
probe(floor_3) -> erlang:apply(erlang,opaque(floor),[9007199254740993]);
probe(floor_4) -> erlang:apply(erlang,opaque(floor),[9007199254740995]);
probe(floor_5) -> erlang:apply(erlang,opaque(floor),[-9007199254740993]);
probe(floor_6) -> erlang:apply(erlang,opaque(floor),[1 bsl 63]);
probe(floor_7) -> erlang:apply(erlang,opaque(floor),[-(1 bsl 63)]);
probe(floor_8) -> erlang:apply(erlang,opaque(floor),[1 bsl 1023]);
probe(floor_9) -> erlang:apply(erlang,opaque(floor),[(1 bsl 1024)-(1 bsl 971)]);
probe(floor_10) -> erlang:apply(erlang,opaque(floor),[(1 bsl 1024)-(1 bsl 970)]);
probe(floor_11) -> erlang:apply(erlang,opaque(floor),[1 bsl 1024]);
probe(floor_12) -> erlang:apply(erlang,opaque(floor),[1 bsl 4096]);
probe(floor_13) -> erlang:apply(erlang,opaque(floor),[0.0]);
probe(floor_14) -> erlang:apply(erlang,opaque(floor),[-0.0]);
probe(floor_15) -> erlang:apply(erlang,opaque(floor),[0.5]);
probe(floor_16) -> erlang:apply(erlang,opaque(floor),[-0.5]);
probe(floor_17) -> erlang:apply(erlang,opaque(floor),[0.49999999999999994]);
probe(floor_18) -> erlang:apply(erlang,opaque(floor),[-0.49999999999999994]);
probe(floor_19) -> erlang:apply(erlang,opaque(floor),[0.5000000000000001]);
probe(floor_20) -> erlang:apply(erlang,opaque(floor),[-0.5000000000000001]);
probe(floor_21) -> erlang:apply(erlang,opaque(floor),[1.5]);
probe(floor_22) -> erlang:apply(erlang,opaque(floor),[-1.5]);
probe(floor_23) -> erlang:apply(erlang,opaque(floor),[2.5]);
probe(floor_24) -> erlang:apply(erlang,opaque(floor),[-2.5]);
probe(floor_25) -> erlang:apply(erlang,opaque(floor),[1.0e20]);
probe(floor_26) -> erlang:apply(erlang,opaque(floor),[-1.0e20]);
probe(floor_27) -> erlang:apply(erlang,opaque(floor),[1.0e308]);
probe(floor_28) -> erlang:apply(erlang,opaque(floor),[-1.0e308]);
probe(floor_29) -> erlang:apply(erlang,opaque(floor),[5.0e-324]);
probe(floor_30) -> erlang:apply(erlang,opaque(floor),[-5.0e-324]);
probe(floor_31) -> erlang:apply(erlang,opaque(floor),[2.2250738585072014e-308]);
probe(floor_32) -> erlang:apply(erlang,opaque(floor),[-2.2250738585072014e-308]);
probe(floor_33) -> erlang:apply(erlang,opaque(floor),[a]);
probe(floor_34) -> erlang:apply(erlang,opaque(floor),[[]]);
probe(floor_35) -> erlang:apply(erlang,opaque(floor),[[a]]);
probe(floor_36) -> erlang:apply(erlang,opaque(floor),[[a|tail]]);
probe(floor_37) -> erlang:apply(erlang,opaque(floor),[{}]);
probe(floor_38) -> erlang:apply(erlang,opaque(floor),[#{}]);
probe(floor_39) -> erlang:apply(erlang,opaque(floor),[<<1:1>>]);
probe(floor_40) -> erlang:apply(erlang,opaque(floor),[fun erlang:abs/1]);
probe(ceil_0) -> erlang:apply(erlang,opaque(ceil),[0]);
probe(ceil_1) -> erlang:apply(erlang,opaque(ceil),[1]);
probe(ceil_2) -> erlang:apply(erlang,opaque(ceil),[-1]);
probe(ceil_3) -> erlang:apply(erlang,opaque(ceil),[9007199254740993]);
probe(ceil_4) -> erlang:apply(erlang,opaque(ceil),[9007199254740995]);
probe(ceil_5) -> erlang:apply(erlang,opaque(ceil),[-9007199254740993]);
probe(ceil_6) -> erlang:apply(erlang,opaque(ceil),[1 bsl 63]);
probe(ceil_7) -> erlang:apply(erlang,opaque(ceil),[-(1 bsl 63)]);
probe(ceil_8) -> erlang:apply(erlang,opaque(ceil),[1 bsl 1023]);
probe(ceil_9) -> erlang:apply(erlang,opaque(ceil),[(1 bsl 1024)-(1 bsl 971)]);
probe(ceil_10) -> erlang:apply(erlang,opaque(ceil),[(1 bsl 1024)-(1 bsl 970)]);
probe(ceil_11) -> erlang:apply(erlang,opaque(ceil),[1 bsl 1024]);
probe(ceil_12) -> erlang:apply(erlang,opaque(ceil),[1 bsl 4096]);
probe(ceil_13) -> erlang:apply(erlang,opaque(ceil),[0.0]);
probe(ceil_14) -> erlang:apply(erlang,opaque(ceil),[-0.0]);
probe(ceil_15) -> erlang:apply(erlang,opaque(ceil),[0.5]);
probe(ceil_16) -> erlang:apply(erlang,opaque(ceil),[-0.5]);
probe(ceil_17) -> erlang:apply(erlang,opaque(ceil),[0.49999999999999994]);
probe(ceil_18) -> erlang:apply(erlang,opaque(ceil),[-0.49999999999999994]);
probe(ceil_19) -> erlang:apply(erlang,opaque(ceil),[0.5000000000000001]);
probe(ceil_20) -> erlang:apply(erlang,opaque(ceil),[-0.5000000000000001]);
probe(ceil_21) -> erlang:apply(erlang,opaque(ceil),[1.5]);
probe(ceil_22) -> erlang:apply(erlang,opaque(ceil),[-1.5]);
probe(ceil_23) -> erlang:apply(erlang,opaque(ceil),[2.5]);
probe(ceil_24) -> erlang:apply(erlang,opaque(ceil),[-2.5]);
probe(ceil_25) -> erlang:apply(erlang,opaque(ceil),[1.0e20]);
probe(ceil_26) -> erlang:apply(erlang,opaque(ceil),[-1.0e20]);
probe(ceil_27) -> erlang:apply(erlang,opaque(ceil),[1.0e308]);
probe(ceil_28) -> erlang:apply(erlang,opaque(ceil),[-1.0e308]);
probe(ceil_29) -> erlang:apply(erlang,opaque(ceil),[5.0e-324]);
probe(ceil_30) -> erlang:apply(erlang,opaque(ceil),[-5.0e-324]);
probe(ceil_31) -> erlang:apply(erlang,opaque(ceil),[2.2250738585072014e-308]);
probe(ceil_32) -> erlang:apply(erlang,opaque(ceil),[-2.2250738585072014e-308]);
probe(ceil_33) -> erlang:apply(erlang,opaque(ceil),[a]);
probe(ceil_34) -> erlang:apply(erlang,opaque(ceil),[[]]);
probe(ceil_35) -> erlang:apply(erlang,opaque(ceil),[[a]]);
probe(ceil_36) -> erlang:apply(erlang,opaque(ceil),[[a|tail]]);
probe(ceil_37) -> erlang:apply(erlang,opaque(ceil),[{}]);
probe(ceil_38) -> erlang:apply(erlang,opaque(ceil),[#{}]);
probe(ceil_39) -> erlang:apply(erlang,opaque(ceil),[<<1:1>>]);
probe(ceil_40) -> erlang:apply(erlang,opaque(ceil),[fun erlang:abs/1]);
probe(abs_same_big_positive) -> B=list_to_integer("123456789012345678901234567890"), A=erlang:apply(erlang,opaque(abs),[B]), erts_debug:same(B,A);
probe(abs_same_float_positive) -> B=binary_to_term(<<131,70,63,248,0,0,0,0,0,0>>), A=erlang:apply(erlang,opaque(abs),[B]), erts_debug:same(B,A);
probe(abs_same_float_zero) -> B=binary_to_term(<<131,70,0,0,0,0,0,0,0,0>>), A=erlang:apply(erlang,opaque(abs),[B]), erts_debug:same(B,A);
probe(abs_same_float_negative_zero) -> B=binary_to_term(<<131,70,128,0,0,0,0,0,0,0>>), A=erlang:apply(erlang,opaque(abs),[B]), erts_debug:same(B,A);
probe(float_same) -> B=binary_to_term(<<131,70,63,248,0,0,0,0,0,0>>), A=erlang:apply(erlang,opaque(float),[B]), erts_debug:same(B,A);
probe(integer_constant_same) -> B=list_to_integer("123456789012345678901234567890"), A=erlang:apply(erlang,abs,[B]), erts_debug:same(B,A).

opaque(Value) -> erlang:put(numeric_contract487_arg,Value), erlang:erase(numeric_contract487_arg).
