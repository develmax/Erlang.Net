-module(beam_math_observer392).
-export([cases/0,run/0,run_stacks/0,worker/1]).

cases() -> [acos_ordinary, acos_positive_zero, acos_negative_zero, acos_integer, acos_negative, acos_large_finite_integer, acos_overflow_integer, acos_atom, acos_improper_list, acosh_ordinary, acosh_positive_zero, acosh_negative_zero, acosh_integer, acosh_negative, acosh_large_finite_integer, acosh_overflow_integer, acosh_atom, acosh_improper_list, asin_ordinary, asin_positive_zero, asin_negative_zero, asin_integer, asin_negative, asin_large_finite_integer, asin_overflow_integer, asin_atom, asin_improper_list, asinh_ordinary, asinh_positive_zero, asinh_negative_zero, asinh_integer, asinh_negative, asinh_large_finite_integer, asinh_overflow_integer, asinh_atom, asinh_improper_list, atan_ordinary, atan_positive_zero, atan_negative_zero, atan_integer, atan_negative, atan_large_finite_integer, atan_overflow_integer, atan_atom, atan_improper_list, atan2_ordinary, atan2_signed_zero, atan2_left_zero, atan2_right_zero, atan2_negative, atan2_left_atom, atan2_right_atom, atan2_left_overflow_first, atan2_left_type_first, atan2_right_overflow, atan2_finite_integer, atanh_ordinary, atanh_positive_zero, atanh_negative_zero, atanh_integer, atanh_negative, atanh_large_finite_integer, atanh_overflow_integer, atanh_atom, atanh_improper_list, ceil_ordinary, ceil_positive_zero, ceil_negative_zero, ceil_integer, ceil_negative, ceil_large_finite_integer, ceil_overflow_integer, ceil_atom, ceil_improper_list, cos_ordinary, cos_positive_zero, cos_negative_zero, cos_integer, cos_negative, cos_large_finite_integer, cos_overflow_integer, cos_atom, cos_improper_list, cosh_ordinary, cosh_positive_zero, cosh_negative_zero, cosh_integer, cosh_negative, cosh_large_finite_integer, cosh_overflow_integer, cosh_atom, cosh_improper_list, erf_ordinary, erf_positive_zero, erf_negative_zero, erf_integer, erf_negative, erf_large_finite_integer, erf_overflow_integer, erf_atom, erf_improper_list, erfc_ordinary, erfc_positive_zero, erfc_negative_zero, erfc_integer, erfc_negative, erfc_large_finite_integer, erfc_overflow_integer, erfc_atom, erfc_improper_list, exp_ordinary, exp_positive_zero, exp_negative_zero, exp_integer, exp_negative, exp_large_finite_integer, exp_overflow_integer, exp_atom, exp_improper_list, floor_ordinary, floor_positive_zero, floor_negative_zero, floor_integer, floor_negative, floor_large_finite_integer, floor_overflow_integer, floor_atom, floor_improper_list, fmod_ordinary, fmod_signed_zero, fmod_left_zero, fmod_right_zero, fmod_negative, fmod_left_atom, fmod_right_atom, fmod_left_overflow_first, fmod_left_type_first, fmod_right_overflow, fmod_finite_integer, log_ordinary, log_positive_zero, log_negative_zero, log_integer, log_negative, log_large_finite_integer, log_overflow_integer, log_atom, log_improper_list, log2_ordinary, log2_positive_zero, log2_negative_zero, log2_integer, log2_negative, log2_large_finite_integer, log2_overflow_integer, log2_atom, log2_improper_list, log10_ordinary, log10_positive_zero, log10_negative_zero, log10_integer, log10_negative, log10_large_finite_integer, log10_overflow_integer, log10_atom, log10_improper_list, pow_ordinary, pow_signed_zero, pow_left_zero, pow_right_zero, pow_negative, pow_left_atom, pow_right_atom, pow_left_overflow_first, pow_left_type_first, pow_right_overflow, pow_finite_integer, sin_ordinary, sin_positive_zero, sin_negative_zero, sin_integer, sin_negative, sin_large_finite_integer, sin_overflow_integer, sin_atom, sin_improper_list, sinh_ordinary, sinh_positive_zero, sinh_negative_zero, sinh_integer, sinh_negative, sinh_large_finite_integer, sinh_overflow_integer, sinh_atom, sinh_improper_list, sqrt_ordinary, sqrt_positive_zero, sqrt_negative_zero, sqrt_integer, sqrt_negative, sqrt_large_finite_integer, sqrt_overflow_integer, sqrt_atom, sqrt_improper_list, tan_ordinary, tan_positive_zero, tan_negative_zero, tan_integer, tan_negative, tan_large_finite_integer, tan_overflow_integer, tan_atom, tan_improper_list, tanh_ordinary, tanh_positive_zero, tanh_negative_zero, tanh_integer, tanh_negative, tanh_large_finite_integer, tanh_overflow_integer, tanh_atom, tanh_improper_list, acos_above, asin_below, acosh_below, atanh_upper, atanh_lower, exp_overflow, exp_underflow, pow_underflow, pow_domain, pow_zero_zero, atan2_quadrant, erf_small, erfc_tail, erfc_negative_tail, fmod_negative_divisor, fmod_zero_result, ceil_tie_even, ceil_above_tie, floor_negative_tie, sqrt_subnormal, sin_large_argument, cos_large_argument, asinh_large, acosh_large, math_pi, math_tau].

run() ->
    {Child, Ref} = erlang:spawn_monitor(?MODULE, worker, [self()]),
    receive
        {Child, Values} ->
            receive
                {'DOWN', Ref, process, Child, normal} -> Values;
                {'DOWN', Ref, process, Child, Reason} -> erlang:error({child_exit, Reason})
            after 5000 -> erlang:error(child_not_settled)
            end;
        {'DOWN', Ref, process, Child, Reason} -> erlang:error({child_exit, Reason})
    after 30000 -> erlang:exit(Child, kill), erlang:error(child_timeout)
    end.

worker(Owner) ->
    Owner ! {self(), rows(cases())},
    ok.

rows([]) -> [];
rows([Case | Rest]) ->
    Outcome = observe(Case),
    [{Case, Outcome} | rows(Rest)].

run_stacks() ->
    Old = erlang:system_flag(backtrace_depth, 8),
    erlang:system_flag(backtrace_depth, Old),
    try
        Default = {default, run()},
        Zero = at_depth(0),
        One = at_depth(1),
        Two = at_depth(2),
        Eight = at_depth(8),
        [Default, Zero, One, Two, Eight]
    after erlang:system_flag(backtrace_depth, Old)
    end.

at_depth(Depth) ->
    erlang:system_flag(backtrace_depth, Depth),
    {Depth, run()}.

observe(Case) ->
    try value(Case) of Value -> {returned, Value}
    catch Class:Reason:Stack -> {caught, Class, Reason, Stack}
    end.

value(acos_ordinary) -> math:acos(0.5);
value(acos_positive_zero) -> math:acos(0.0);
value(acos_negative_zero) -> math:acos(-0.0);
value(acos_integer) -> math:acos(42);
value(acos_negative) -> math:acos(-1.5);
value(acos_large_finite_integer) -> math:acos(1 bsl 1000);
value(acos_overflow_integer) -> math:acos(1 bsl 1024);
value(acos_atom) -> math:acos(bad_math_input);
value(acos_improper_list) -> math:acos([1|2]);
value(acosh_ordinary) -> math:acosh(1.5);
value(acosh_positive_zero) -> math:acosh(0.0);
value(acosh_negative_zero) -> math:acosh(-0.0);
value(acosh_integer) -> math:acosh(42);
value(acosh_negative) -> math:acosh(-1.5);
value(acosh_large_finite_integer) -> math:acosh(1 bsl 1000);
value(acosh_overflow_integer) -> math:acosh(1 bsl 1024);
value(acosh_atom) -> math:acosh(bad_math_input);
value(acosh_improper_list) -> math:acosh([1|2]);
value(asin_ordinary) -> math:asin(0.5);
value(asin_positive_zero) -> math:asin(0.0);
value(asin_negative_zero) -> math:asin(-0.0);
value(asin_integer) -> math:asin(42);
value(asin_negative) -> math:asin(-1.5);
value(asin_large_finite_integer) -> math:asin(1 bsl 1000);
value(asin_overflow_integer) -> math:asin(1 bsl 1024);
value(asin_atom) -> math:asin(bad_math_input);
value(asin_improper_list) -> math:asin([1|2]);
value(asinh_ordinary) -> math:asinh(0.5);
value(asinh_positive_zero) -> math:asinh(0.0);
value(asinh_negative_zero) -> math:asinh(-0.0);
value(asinh_integer) -> math:asinh(42);
value(asinh_negative) -> math:asinh(-1.5);
value(asinh_large_finite_integer) -> math:asinh(1 bsl 1000);
value(asinh_overflow_integer) -> math:asinh(1 bsl 1024);
value(asinh_atom) -> math:asinh(bad_math_input);
value(asinh_improper_list) -> math:asinh([1|2]);
value(atan_ordinary) -> math:atan(0.5);
value(atan_positive_zero) -> math:atan(0.0);
value(atan_negative_zero) -> math:atan(-0.0);
value(atan_integer) -> math:atan(42);
value(atan_negative) -> math:atan(-1.5);
value(atan_large_finite_integer) -> math:atan(1 bsl 1000);
value(atan_overflow_integer) -> math:atan(1 bsl 1024);
value(atan_atom) -> math:atan(bad_math_input);
value(atan_improper_list) -> math:atan([1|2]);
value(atan2_ordinary) -> math:atan2(1.5,2.0);
value(atan2_signed_zero) -> math:atan2(-0.0,-0.0);
value(atan2_left_zero) -> math:atan2(0.0,-1.0);
value(atan2_right_zero) -> math:atan2(1.0,0.0);
value(atan2_negative) -> math:atan2(-5.5,2.0);
value(atan2_left_atom) -> math:atan2(bad_math_input,1);
value(atan2_right_atom) -> math:atan2(1,bad_math_input);
value(atan2_left_overflow_first) -> math:atan2(1 bsl 1024,bad_math_input);
value(atan2_left_type_first) -> math:atan2(bad_math_input,1 bsl 1024);
value(atan2_right_overflow) -> math:atan2(1,1 bsl 1024);
value(atan2_finite_integer) -> math:atan2(1 bsl 1000,3);
value(atanh_ordinary) -> math:atanh(0.5);
value(atanh_positive_zero) -> math:atanh(0.0);
value(atanh_negative_zero) -> math:atanh(-0.0);
value(atanh_integer) -> math:atanh(42);
value(atanh_negative) -> math:atanh(-1.5);
value(atanh_large_finite_integer) -> math:atanh(1 bsl 1000);
value(atanh_overflow_integer) -> math:atanh(1 bsl 1024);
value(atanh_atom) -> math:atanh(bad_math_input);
value(atanh_improper_list) -> math:atanh([1|2]);
value(ceil_ordinary) -> math:ceil(0.5);
value(ceil_positive_zero) -> math:ceil(0.0);
value(ceil_negative_zero) -> math:ceil(-0.0);
value(ceil_integer) -> math:ceil(42);
value(ceil_negative) -> math:ceil(-1.5);
value(ceil_large_finite_integer) -> math:ceil(1 bsl 1000);
value(ceil_overflow_integer) -> math:ceil(1 bsl 1024);
value(ceil_atom) -> math:ceil(bad_math_input);
value(ceil_improper_list) -> math:ceil([1|2]);
value(cos_ordinary) -> math:cos(0.5);
value(cos_positive_zero) -> math:cos(0.0);
value(cos_negative_zero) -> math:cos(-0.0);
value(cos_integer) -> math:cos(42);
value(cos_negative) -> math:cos(-1.5);
value(cos_large_finite_integer) -> math:cos(1 bsl 1000);
value(cos_overflow_integer) -> math:cos(1 bsl 1024);
value(cos_atom) -> math:cos(bad_math_input);
value(cos_improper_list) -> math:cos([1|2]);
value(cosh_ordinary) -> math:cosh(0.5);
value(cosh_positive_zero) -> math:cosh(0.0);
value(cosh_negative_zero) -> math:cosh(-0.0);
value(cosh_integer) -> math:cosh(42);
value(cosh_negative) -> math:cosh(-1.5);
value(cosh_large_finite_integer) -> math:cosh(1 bsl 1000);
value(cosh_overflow_integer) -> math:cosh(1 bsl 1024);
value(cosh_atom) -> math:cosh(bad_math_input);
value(cosh_improper_list) -> math:cosh([1|2]);
value(erf_ordinary) -> math:erf(0.5);
value(erf_positive_zero) -> math:erf(0.0);
value(erf_negative_zero) -> math:erf(-0.0);
value(erf_integer) -> math:erf(42);
value(erf_negative) -> math:erf(-1.5);
value(erf_large_finite_integer) -> math:erf(1 bsl 1000);
value(erf_overflow_integer) -> math:erf(1 bsl 1024);
value(erf_atom) -> math:erf(bad_math_input);
value(erf_improper_list) -> math:erf([1|2]);
value(erfc_ordinary) -> math:erfc(0.5);
value(erfc_positive_zero) -> math:erfc(0.0);
value(erfc_negative_zero) -> math:erfc(-0.0);
value(erfc_integer) -> math:erfc(42);
value(erfc_negative) -> math:erfc(-1.5);
value(erfc_large_finite_integer) -> math:erfc(1 bsl 1000);
value(erfc_overflow_integer) -> math:erfc(1 bsl 1024);
value(erfc_atom) -> math:erfc(bad_math_input);
value(erfc_improper_list) -> math:erfc([1|2]);
value(exp_ordinary) -> math:exp(0.5);
value(exp_positive_zero) -> math:exp(0.0);
value(exp_negative_zero) -> math:exp(-0.0);
value(exp_integer) -> math:exp(42);
value(exp_negative) -> math:exp(-1.5);
value(exp_large_finite_integer) -> math:exp(1 bsl 1000);
value(exp_overflow_integer) -> math:exp(1 bsl 1024);
value(exp_atom) -> math:exp(bad_math_input);
value(exp_improper_list) -> math:exp([1|2]);
value(floor_ordinary) -> math:floor(0.5);
value(floor_positive_zero) -> math:floor(0.0);
value(floor_negative_zero) -> math:floor(-0.0);
value(floor_integer) -> math:floor(42);
value(floor_negative) -> math:floor(-1.5);
value(floor_large_finite_integer) -> math:floor(1 bsl 1000);
value(floor_overflow_integer) -> math:floor(1 bsl 1024);
value(floor_atom) -> math:floor(bad_math_input);
value(floor_improper_list) -> math:floor([1|2]);
value(fmod_ordinary) -> math:fmod(1.5,2.0);
value(fmod_signed_zero) -> math:fmod(-0.0,-0.0);
value(fmod_left_zero) -> math:fmod(0.0,-1.0);
value(fmod_right_zero) -> math:fmod(1.0,0.0);
value(fmod_negative) -> math:fmod(-5.5,2.0);
value(fmod_left_atom) -> math:fmod(bad_math_input,1);
value(fmod_right_atom) -> math:fmod(1,bad_math_input);
value(fmod_left_overflow_first) -> math:fmod(1 bsl 1024,bad_math_input);
value(fmod_left_type_first) -> math:fmod(bad_math_input,1 bsl 1024);
value(fmod_right_overflow) -> math:fmod(1,1 bsl 1024);
value(fmod_finite_integer) -> math:fmod(1 bsl 1000,3);
value(log_ordinary) -> math:log(2.0);
value(log_positive_zero) -> math:log(0.0);
value(log_negative_zero) -> math:log(-0.0);
value(log_integer) -> math:log(42);
value(log_negative) -> math:log(-1.5);
value(log_large_finite_integer) -> math:log(1 bsl 1000);
value(log_overflow_integer) -> math:log(1 bsl 1024);
value(log_atom) -> math:log(bad_math_input);
value(log_improper_list) -> math:log([1|2]);
value(log2_ordinary) -> math:log2(2.0);
value(log2_positive_zero) -> math:log2(0.0);
value(log2_negative_zero) -> math:log2(-0.0);
value(log2_integer) -> math:log2(42);
value(log2_negative) -> math:log2(-1.5);
value(log2_large_finite_integer) -> math:log2(1 bsl 1000);
value(log2_overflow_integer) -> math:log2(1 bsl 1024);
value(log2_atom) -> math:log2(bad_math_input);
value(log2_improper_list) -> math:log2([1|2]);
value(log10_ordinary) -> math:log10(2.0);
value(log10_positive_zero) -> math:log10(0.0);
value(log10_negative_zero) -> math:log10(-0.0);
value(log10_integer) -> math:log10(42);
value(log10_negative) -> math:log10(-1.5);
value(log10_large_finite_integer) -> math:log10(1 bsl 1000);
value(log10_overflow_integer) -> math:log10(1 bsl 1024);
value(log10_atom) -> math:log10(bad_math_input);
value(log10_improper_list) -> math:log10([1|2]);
value(pow_ordinary) -> math:pow(1.5,2.0);
value(pow_signed_zero) -> math:pow(-0.0,-0.0);
value(pow_left_zero) -> math:pow(0.0,-1.0);
value(pow_right_zero) -> math:pow(1.0,0.0);
value(pow_negative) -> math:pow(-5.5,2.0);
value(pow_left_atom) -> math:pow(bad_math_input,1);
value(pow_right_atom) -> math:pow(1,bad_math_input);
value(pow_left_overflow_first) -> math:pow(1 bsl 1024,bad_math_input);
value(pow_left_type_first) -> math:pow(bad_math_input,1 bsl 1024);
value(pow_right_overflow) -> math:pow(1,1 bsl 1024);
value(pow_finite_integer) -> math:pow(1 bsl 1000,3);
value(sin_ordinary) -> math:sin(0.5);
value(sin_positive_zero) -> math:sin(0.0);
value(sin_negative_zero) -> math:sin(-0.0);
value(sin_integer) -> math:sin(42);
value(sin_negative) -> math:sin(-1.5);
value(sin_large_finite_integer) -> math:sin(1 bsl 1000);
value(sin_overflow_integer) -> math:sin(1 bsl 1024);
value(sin_atom) -> math:sin(bad_math_input);
value(sin_improper_list) -> math:sin([1|2]);
value(sinh_ordinary) -> math:sinh(0.5);
value(sinh_positive_zero) -> math:sinh(0.0);
value(sinh_negative_zero) -> math:sinh(-0.0);
value(sinh_integer) -> math:sinh(42);
value(sinh_negative) -> math:sinh(-1.5);
value(sinh_large_finite_integer) -> math:sinh(1 bsl 1000);
value(sinh_overflow_integer) -> math:sinh(1 bsl 1024);
value(sinh_atom) -> math:sinh(bad_math_input);
value(sinh_improper_list) -> math:sinh([1|2]);
value(sqrt_ordinary) -> math:sqrt(2.0);
value(sqrt_positive_zero) -> math:sqrt(0.0);
value(sqrt_negative_zero) -> math:sqrt(-0.0);
value(sqrt_integer) -> math:sqrt(42);
value(sqrt_negative) -> math:sqrt(-1.5);
value(sqrt_large_finite_integer) -> math:sqrt(1 bsl 1000);
value(sqrt_overflow_integer) -> math:sqrt(1 bsl 1024);
value(sqrt_atom) -> math:sqrt(bad_math_input);
value(sqrt_improper_list) -> math:sqrt([1|2]);
value(tan_ordinary) -> math:tan(0.5);
value(tan_positive_zero) -> math:tan(0.0);
value(tan_negative_zero) -> math:tan(-0.0);
value(tan_integer) -> math:tan(42);
value(tan_negative) -> math:tan(-1.5);
value(tan_large_finite_integer) -> math:tan(1 bsl 1000);
value(tan_overflow_integer) -> math:tan(1 bsl 1024);
value(tan_atom) -> math:tan(bad_math_input);
value(tan_improper_list) -> math:tan([1|2]);
value(tanh_ordinary) -> math:tanh(0.5);
value(tanh_positive_zero) -> math:tanh(0.0);
value(tanh_negative_zero) -> math:tanh(-0.0);
value(tanh_integer) -> math:tanh(42);
value(tanh_negative) -> math:tanh(-1.5);
value(tanh_large_finite_integer) -> math:tanh(1 bsl 1000);
value(tanh_overflow_integer) -> math:tanh(1 bsl 1024);
value(tanh_atom) -> math:tanh(bad_math_input);
value(tanh_improper_list) -> math:tanh([1|2]);
value(acos_above) -> math:acos(1.0000000000000002);
value(asin_below) -> math:asin(-1.0000000000000002);
value(acosh_below) -> math:acosh(0.9999999999999999);
value(atanh_upper) -> math:atanh(1.0);
value(atanh_lower) -> math:atanh(-1.0);
value(exp_overflow) -> math:exp(1000.0);
value(exp_underflow) -> math:exp(-1000.0);
value(pow_underflow) -> math:pow(1.0e-200,2.0);
value(pow_domain) -> math:pow(-1.0,0.5);
value(pow_zero_zero) -> math:pow(0.0,0.0);
value(atan2_quadrant) -> math:atan2(-0.0,-1.0);
value(erf_small) -> math:erf(1.0e-300);
value(erfc_tail) -> math:erfc(27.0);
value(erfc_negative_tail) -> math:erfc(-27.0);
value(fmod_negative_divisor) -> math:fmod(5.5,-2.0);
value(fmod_zero_result) -> math:fmod(-4.0,2.0);
value(ceil_tie_even) -> math:ceil((1 bsl 60)+128);
value(ceil_above_tie) -> math:ceil((1 bsl 60)+129);
value(floor_negative_tie) -> math:floor(-((1 bsl 60)+128));
value(sqrt_subnormal) -> math:sqrt(5.0e-324);
value(sin_large_argument) -> math:sin(1.0e300);
value(cos_large_argument) -> math:cos(1.0e300);
value(asinh_large) -> math:asinh(1.0e300);
value(acosh_large) -> math:acosh(1.0e300);
value(math_pi) -> math:pi();
value(math_tau) -> math:tau().
