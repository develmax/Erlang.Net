-module(numeric371_contract_edges).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [list_wide_ascii_digit,
            list_negative_low_digit,
            list_wide_unicode_digit,
            list_wrapped_zero,
            list_actual_minus_wide_digit,
            list_wrapped_minus_not_sign,
            list_wrapped_zero_then_digit,
            list_small_max_low_digit,
            list_small_min_low_digit,
            list_bignum_positive_not_small,
            list_bignum_negative_not_small,
            list_wide_digit_improper,
            list_wrapped_plus_not_sign,
            list_actual_plus_wide_digit,
            list_native_small_17_wide_digits,
            list_native_big_18_wide_digits,
            list_native_small_17_actual_digits,
            list_native_big_18_actual_digits,
            list_base_upperwide,
            list_base_lowerwide,
            list_base_binarywrappedzero,
            list_base_binarywrappeddigitinvalid,
            list_base_native_small_hexwide,
            list_base_native_big_hexwide,
            float_to_list_scientific_min_int,
            float_to_list_scientific_below_min_int,
            float_to_list_scientific_max_int,
            float_to_list_scientific_above_max_int,
            float_to_list_scientific_wrap_minus_one,
            float_to_list_scientific_wrap_zero,
            float_to_list_scientific_wrap_two,
            float_to_list_scientific_small_max,
            float_to_list_scientific_small_min,
            float_to_list_scientific_bignum_rejected,
            float_to_list_fixed_wrap_two,
            float_to_list_fixed_wrap_negative,
            float_to_list_fixed_small_min,
            float_to_list_short_ignores_negative,
            float_to_list_scientific_overrides_short,
            float_to_list_fixed_overrides_negative,
            float_to_list_scientific_overrides_fixed,
            float_to_list_short_after_huge,
            float_to_binary_scientific_min_int,
            float_to_binary_scientific_below_min_int,
            float_to_binary_scientific_max_int,
            float_to_binary_scientific_above_max_int,
            float_to_binary_scientific_wrap_minus_one,
            float_to_binary_scientific_wrap_zero,
            float_to_binary_scientific_wrap_two,
            float_to_binary_scientific_small_max,
            float_to_binary_scientific_small_min,
            float_to_binary_scientific_bignum_rejected,
            float_to_binary_fixed_wrap_two,
            float_to_binary_fixed_wrap_negative,
            float_to_binary_fixed_small_min,
            float_to_binary_short_ignores_negative,
            float_to_binary_scientific_overrides_short,
            float_to_binary_fixed_overrides_negative,
            float_to_binary_scientific_overrides_fixed,
            float_to_binary_short_after_huge].
run() -> erlang370_pure_support:run(numeric371_contract_edges,cases()).
run_stacks() -> erlang370_pure_support:profiles(numeric371_contract_edges,cases()).
probe(list_wide_ascii_digit) -> Result=erlang370_pure_support:invoke(list_to_integer,[[305]]),{continued,Result};
probe(list_negative_low_digit) -> erlang370_pure_support:invoke(list_to_integer,[[-207]]);
probe(list_wide_unicode_digit) -> Result=erlang370_pure_support:invoke(list_to_integer,[[65585]]),{continued,Result};
probe(list_wrapped_zero) -> erlang370_pure_support:invoke(list_to_integer,[[304]]);
probe(list_actual_minus_wide_digit) -> Result=erlang370_pure_support:invoke(list_to_integer,[[45,305]]),{continued,Result};
probe(list_wrapped_minus_not_sign) -> erlang370_pure_support:invoke(list_to_integer,[[301,49]]);
probe(list_wrapped_zero_then_digit) -> Result=erlang370_pure_support:invoke(list_to_integer,[[304,305]]),{continued,Result};
probe(list_small_max_low_digit) -> erlang370_pure_support:invoke(list_to_integer,[[(1 bsl 59)-207]]);
probe(list_small_min_low_digit) -> Result=erlang370_pure_support:invoke(list_to_integer,[[-(1 bsl 59)+49]]),{continued,Result};
probe(list_bignum_positive_not_small) -> erlang370_pure_support:invoke(list_to_integer,[[(1 bsl 59)+49]]);
probe(list_bignum_negative_not_small) -> Result=erlang370_pure_support:invoke(list_to_integer,[[-(1 bsl 59)-207]]),{continued,Result};
probe(list_wide_digit_improper) -> erlang370_pure_support:invoke(list_to_integer,[[305|tail]]);
probe(list_wrapped_plus_not_sign) -> Result=erlang370_pure_support:invoke(list_to_integer,[[299,49]]),{continued,Result};
probe(list_actual_plus_wide_digit) -> erlang370_pure_support:invoke(list_to_integer,[[43,305]]);
probe(list_native_small_17_wide_digits) -> Result=erlang370_pure_support:invoke(list_to_integer,[lists:duplicate(17,305)]),{continued,Result};
probe(list_native_big_18_wide_digits) -> erlang370_pure_support:invoke(list_to_integer,[lists:duplicate(18,305)]);
probe(list_native_small_17_actual_digits) -> Result=erlang370_pure_support:invoke(list_to_integer,[lists:duplicate(17,49)]),{continued,Result};
probe(list_native_big_18_actual_digits) -> erlang370_pure_support:invoke(list_to_integer,[lists:duplicate(18,49)]);
probe(list_base_upperwide) -> Result=erlang370_pure_support:invoke(list_to_integer,[[322],16]),{continued,Result};
probe(list_base_lowerwide) -> erlang370_pure_support:invoke(list_to_integer,[[354],16]);
probe(list_base_binarywrappedzero) -> Result=erlang370_pure_support:invoke(list_to_integer,[[304,305],2]),{continued,Result};
probe(list_base_binarywrappeddigitinvalid) -> erlang370_pure_support:invoke(list_to_integer,[[306],2]);
probe(list_base_native_small_hexwide) -> Result=erlang370_pure_support:invoke(list_to_integer,[lists:duplicate(14,322),16]),{continued,Result};
probe(list_base_native_big_hexwide) -> erlang370_pure_support:invoke(list_to_integer,[lists:duplicate(15,322),16]);
probe(float_to_list_scientific_min_int) -> Result=erlang370_pure_support:invoke(float_to_list,[1.25,[{scientific,-2147483648}]]),{continued,Result};
probe(float_to_list_scientific_below_min_int) -> erlang370_pure_support:invoke(float_to_list,[1.25,[{scientific,-2147483649}]]);
probe(float_to_list_scientific_max_int) -> Result=erlang370_pure_support:invoke(float_to_list,[1.25,[{scientific,2147483647}]]),{continued,Result};
probe(float_to_list_scientific_above_max_int) -> erlang370_pure_support:invoke(float_to_list,[1.25,[{scientific,2147483648}]]);
probe(float_to_list_scientific_wrap_minus_one) -> Result=erlang370_pure_support:invoke(float_to_list,[1.25,[{scientific,4294967295}]]),{continued,Result};
probe(float_to_list_scientific_wrap_zero) -> erlang370_pure_support:invoke(float_to_list,[1.25,[{scientific,4294967296}]]);
probe(float_to_list_scientific_wrap_two) -> Result=erlang370_pure_support:invoke(float_to_list,[1.25,[{scientific,4294967298}]]),{continued,Result};
probe(float_to_list_scientific_small_max) -> erlang370_pure_support:invoke(float_to_list,[1.25,[{scientific,(1 bsl 59)-1}]]);
probe(float_to_list_scientific_small_min) -> Result=erlang370_pure_support:invoke(float_to_list,[1.25,[{scientific,-(1 bsl 59)}]]),{continued,Result};
probe(float_to_list_scientific_bignum_rejected) -> erlang370_pure_support:invoke(float_to_list,[1.25,[{scientific,1 bsl 59}]]);
probe(float_to_list_fixed_wrap_two) -> Result=erlang370_pure_support:invoke(float_to_list,[1.25,[{decimals,4294967298}]]),{continued,Result};
probe(float_to_list_fixed_wrap_negative) -> erlang370_pure_support:invoke(float_to_list,[1.25,[{decimals,4294967295}]]);
probe(float_to_list_fixed_small_min) -> Result=erlang370_pure_support:invoke(float_to_list,[1.25,[{decimals,-(1 bsl 59)}]]),{continued,Result};
probe(float_to_list_short_ignores_negative) -> erlang370_pure_support:invoke(float_to_list,[1.25,[{scientific,-1},short]]);
probe(float_to_list_scientific_overrides_short) -> Result=erlang370_pure_support:invoke(float_to_list,[1.25,[short,{scientific,-1}]]),{continued,Result};
probe(float_to_list_fixed_overrides_negative) -> erlang370_pure_support:invoke(float_to_list,[1.25,[{scientific,-1},{decimals,2}]]);
probe(float_to_list_scientific_overrides_fixed) -> Result=erlang370_pure_support:invoke(float_to_list,[1.25,[{decimals,-1},{scientific,-1}]]),{continued,Result};
probe(float_to_list_short_after_huge) -> erlang370_pure_support:invoke(float_to_list,[1.25,[{scientific,2147483647},short]]);
probe(float_to_binary_scientific_min_int) -> Result=erlang370_pure_support:invoke(float_to_binary,[1.25,[{scientific,-2147483648}]]),{continued,Result};
probe(float_to_binary_scientific_below_min_int) -> erlang370_pure_support:invoke(float_to_binary,[1.25,[{scientific,-2147483649}]]);
probe(float_to_binary_scientific_max_int) -> Result=erlang370_pure_support:invoke(float_to_binary,[1.25,[{scientific,2147483647}]]),{continued,Result};
probe(float_to_binary_scientific_above_max_int) -> erlang370_pure_support:invoke(float_to_binary,[1.25,[{scientific,2147483648}]]);
probe(float_to_binary_scientific_wrap_minus_one) -> Result=erlang370_pure_support:invoke(float_to_binary,[1.25,[{scientific,4294967295}]]),{continued,Result};
probe(float_to_binary_scientific_wrap_zero) -> erlang370_pure_support:invoke(float_to_binary,[1.25,[{scientific,4294967296}]]);
probe(float_to_binary_scientific_wrap_two) -> Result=erlang370_pure_support:invoke(float_to_binary,[1.25,[{scientific,4294967298}]]),{continued,Result};
probe(float_to_binary_scientific_small_max) -> erlang370_pure_support:invoke(float_to_binary,[1.25,[{scientific,(1 bsl 59)-1}]]);
probe(float_to_binary_scientific_small_min) -> Result=erlang370_pure_support:invoke(float_to_binary,[1.25,[{scientific,-(1 bsl 59)}]]),{continued,Result};
probe(float_to_binary_scientific_bignum_rejected) -> erlang370_pure_support:invoke(float_to_binary,[1.25,[{scientific,1 bsl 59}]]);
probe(float_to_binary_fixed_wrap_two) -> Result=erlang370_pure_support:invoke(float_to_binary,[1.25,[{decimals,4294967298}]]),{continued,Result};
probe(float_to_binary_fixed_wrap_negative) -> erlang370_pure_support:invoke(float_to_binary,[1.25,[{decimals,4294967295}]]);
probe(float_to_binary_fixed_small_min) -> Result=erlang370_pure_support:invoke(float_to_binary,[1.25,[{decimals,-(1 bsl 59)}]]),{continued,Result};
probe(float_to_binary_short_ignores_negative) -> erlang370_pure_support:invoke(float_to_binary,[1.25,[{scientific,-1},short]]);
probe(float_to_binary_scientific_overrides_short) -> Result=erlang370_pure_support:invoke(float_to_binary,[1.25,[short,{scientific,-1}]]),{continued,Result};
probe(float_to_binary_fixed_overrides_negative) -> erlang370_pure_support:invoke(float_to_binary,[1.25,[{scientific,-1},{decimals,2}]]);
probe(float_to_binary_scientific_overrides_fixed) -> Result=erlang370_pure_support:invoke(float_to_binary,[1.25,[{decimals,-1},{scientific,-1}]]),{continued,Result};
probe(float_to_binary_short_after_huge) -> erlang370_pure_support:invoke(float_to_binary,[1.25,[{scientific,2147483647},short]]).
