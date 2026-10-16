-module(lambda328_no_lambda).
-export([undefined_function/3]).
undefined_function(M,F,A)->put(wrong_callback,{M,F,A}),wrong_fallback.
