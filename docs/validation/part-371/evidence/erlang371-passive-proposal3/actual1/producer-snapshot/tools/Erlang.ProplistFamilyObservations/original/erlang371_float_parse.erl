-module(erlang371_float_parse).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [list_to_float_input_0,list_to_float_input_1,list_to_float_input_2,list_to_float_input_3,list_to_float_input_4,list_to_float_input_5,list_to_float_input_6,list_to_float_input_7,list_to_float_input_8,list_to_float_input_9,list_to_float_input_10,list_to_float_input_11,list_to_float_input_12,list_to_float_input_13,list_to_float_input_14,list_to_float_input_15,list_to_float_input_16,list_to_float_input_17,list_to_float_input_18,list_to_float_input_19,list_to_float_input_20,list_to_float_input_21,list_to_float_input_22,list_to_float_input_23,list_to_float_input_24,list_to_float_input_25,list_to_float_input_26,list_to_float_badtype_0,list_to_float_badtype_1,list_to_float_badtype_2,list_to_float_badtype_3,list_to_float_badtype_4,list_to_float_badtype_5,binary_to_float_input_0,binary_to_float_input_1,binary_to_float_input_2,binary_to_float_input_3,binary_to_float_input_4,binary_to_float_input_5,binary_to_float_input_6,binary_to_float_input_7,binary_to_float_input_8,binary_to_float_input_9,binary_to_float_input_10,binary_to_float_input_11,binary_to_float_input_12,binary_to_float_input_13,binary_to_float_input_14,binary_to_float_input_15,binary_to_float_input_16,binary_to_float_input_17,binary_to_float_input_18,binary_to_float_input_19,binary_to_float_input_20,binary_to_float_input_21,binary_to_float_input_22,binary_to_float_input_23,binary_to_float_badtype_0,binary_to_float_badtype_1,binary_to_float_badtype_2,binary_to_float_badtype_3,binary_to_float_badtype_4,binary_to_float_badtype_5].
run() -> erlang370_pure_support:run(erlang371_float_parse,cases()).
run_stacks() -> erlang370_pure_support:profiles(erlang371_float_parse,cases()).
probe(list_to_float_input_0) -> Result=erlang370_pure_support:invoke(list_to_float,["0.0"]),{continued,Result};
probe(list_to_float_input_1) -> erlang370_pure_support:invoke(list_to_float,["-0.0"]);
probe(list_to_float_input_2) -> Result=erlang370_pure_support:invoke(list_to_float,["+0.0"]),{continued,Result};
probe(list_to_float_input_3) -> erlang370_pure_support:invoke(list_to_float,["1.25"]);
probe(list_to_float_input_4) -> Result=erlang370_pure_support:invoke(list_to_float,["-1.25"]),{continued,Result};
probe(list_to_float_input_5) -> erlang370_pure_support:invoke(list_to_float,["1.0e3"]);
probe(list_to_float_input_6) -> Result=erlang370_pure_support:invoke(list_to_float,["1.0E+3"]),{continued,Result};
probe(list_to_float_input_7) -> erlang370_pure_support:invoke(list_to_float,["1.0e-3"]);
probe(list_to_float_input_8) -> Result=erlang370_pure_support:invoke(list_to_float,["1.7976931348623157e308"]),{continued,Result};
probe(list_to_float_input_9) -> erlang370_pure_support:invoke(list_to_float,["2.2250738585072014e-308"]);
probe(list_to_float_input_10) -> Result=erlang370_pure_support:invoke(list_to_float,["4.9406564584124654e-324"]),{continued,Result};
probe(list_to_float_input_11) -> erlang370_pure_support:invoke(list_to_float,["1.0e-9999"]);
probe(list_to_float_input_12) -> Result=erlang370_pure_support:invoke(list_to_float,["1.0e309"]),{continued,Result};
probe(list_to_float_input_13) -> erlang370_pure_support:invoke(list_to_float,[""]);
probe(list_to_float_input_14) -> Result=erlang370_pure_support:invoke(list_to_float,["1"]),{continued,Result};
probe(list_to_float_input_15) -> erlang370_pure_support:invoke(list_to_float,["1e2"]);
probe(list_to_float_input_16) -> Result=erlang370_pure_support:invoke(list_to_float,[".5"]),{continued,Result};
probe(list_to_float_input_17) -> erlang370_pure_support:invoke(list_to_float,["1."]);
probe(list_to_float_input_18) -> Result=erlang370_pure_support:invoke(list_to_float,[" 1.0"]),{continued,Result};
probe(list_to_float_input_19) -> erlang370_pure_support:invoke(list_to_float,["1.0 "]);
probe(list_to_float_input_20) -> Result=erlang370_pure_support:invoke(list_to_float,["nan"]),{continued,Result};
probe(list_to_float_input_21) -> erlang370_pure_support:invoke(list_to_float,["inf"]);
probe(list_to_float_input_22) -> Result=erlang370_pure_support:invoke(list_to_float,["-infinity"]),{continued,Result};
probe(list_to_float_input_23) -> erlang370_pure_support:invoke(list_to_float,["1.0x"]);
probe(list_to_float_input_24) -> Result=erlang370_pure_support:invoke(list_to_float,[[49,46,48,0]]),{continued,Result};
probe(list_to_float_input_25) -> erlang370_pure_support:invoke(list_to_float,[[49,46,48|tail]]);
probe(list_to_float_input_26) -> Result=erlang370_pure_support:invoke(list_to_float,[[49,46,256]]),{continued,Result};
probe(list_to_float_badtype_0) -> Result=erlang370_pure_support:invoke(list_to_float,[a]),{continued,Result};
probe(list_to_float_badtype_1) -> Result=erlang370_pure_support:invoke(list_to_float,[42]),{continued,Result};
probe(list_to_float_badtype_2) -> Result=erlang370_pure_support:invoke(list_to_float,[1.0]),{continued,Result};
probe(list_to_float_badtype_3) -> Result=erlang370_pure_support:invoke(list_to_float,[{}]),{continued,Result};
probe(list_to_float_badtype_4) -> Result=erlang370_pure_support:invoke(list_to_float,[[]]),{continued,Result};
probe(list_to_float_badtype_5) -> Result=erlang370_pure_support:invoke(list_to_float,[<<1:1>>]),{continued,Result};
probe(binary_to_float_input_0) -> Result=erlang370_pure_support:invoke(binary_to_float,[list_to_binary("0.0")]),{continued,Result};
probe(binary_to_float_input_1) -> erlang370_pure_support:invoke(binary_to_float,[list_to_binary("-0.0")]);
probe(binary_to_float_input_2) -> Result=erlang370_pure_support:invoke(binary_to_float,[list_to_binary("+0.0")]),{continued,Result};
probe(binary_to_float_input_3) -> erlang370_pure_support:invoke(binary_to_float,[list_to_binary("1.25")]);
probe(binary_to_float_input_4) -> Result=erlang370_pure_support:invoke(binary_to_float,[list_to_binary("-1.25")]),{continued,Result};
probe(binary_to_float_input_5) -> erlang370_pure_support:invoke(binary_to_float,[list_to_binary("1.0e3")]);
probe(binary_to_float_input_6) -> Result=erlang370_pure_support:invoke(binary_to_float,[list_to_binary("1.0E+3")]),{continued,Result};
probe(binary_to_float_input_7) -> erlang370_pure_support:invoke(binary_to_float,[list_to_binary("1.0e-3")]);
probe(binary_to_float_input_8) -> Result=erlang370_pure_support:invoke(binary_to_float,[list_to_binary("1.7976931348623157e308")]),{continued,Result};
probe(binary_to_float_input_9) -> erlang370_pure_support:invoke(binary_to_float,[list_to_binary("2.2250738585072014e-308")]);
probe(binary_to_float_input_10) -> Result=erlang370_pure_support:invoke(binary_to_float,[list_to_binary("4.9406564584124654e-324")]),{continued,Result};
probe(binary_to_float_input_11) -> erlang370_pure_support:invoke(binary_to_float,[list_to_binary("1.0e-9999")]);
probe(binary_to_float_input_12) -> Result=erlang370_pure_support:invoke(binary_to_float,[list_to_binary("1.0e309")]),{continued,Result};
probe(binary_to_float_input_13) -> erlang370_pure_support:invoke(binary_to_float,[list_to_binary("")]);
probe(binary_to_float_input_14) -> Result=erlang370_pure_support:invoke(binary_to_float,[list_to_binary("1")]),{continued,Result};
probe(binary_to_float_input_15) -> erlang370_pure_support:invoke(binary_to_float,[list_to_binary("1e2")]);
probe(binary_to_float_input_16) -> Result=erlang370_pure_support:invoke(binary_to_float,[list_to_binary(".5")]),{continued,Result};
probe(binary_to_float_input_17) -> erlang370_pure_support:invoke(binary_to_float,[list_to_binary("1.")]);
probe(binary_to_float_input_18) -> Result=erlang370_pure_support:invoke(binary_to_float,[list_to_binary(" 1.0")]),{continued,Result};
probe(binary_to_float_input_19) -> erlang370_pure_support:invoke(binary_to_float,[list_to_binary("1.0 ")]);
probe(binary_to_float_input_20) -> Result=erlang370_pure_support:invoke(binary_to_float,[list_to_binary("nan")]),{continued,Result};
probe(binary_to_float_input_21) -> erlang370_pure_support:invoke(binary_to_float,[list_to_binary("inf")]);
probe(binary_to_float_input_22) -> Result=erlang370_pure_support:invoke(binary_to_float,[list_to_binary("-infinity")]),{continued,Result};
probe(binary_to_float_input_23) -> erlang370_pure_support:invoke(binary_to_float,[list_to_binary("1.0x")]);
probe(binary_to_float_badtype_0) -> Result=erlang370_pure_support:invoke(binary_to_float,[a]),{continued,Result};
probe(binary_to_float_badtype_1) -> Result=erlang370_pure_support:invoke(binary_to_float,[42]),{continued,Result};
probe(binary_to_float_badtype_2) -> Result=erlang370_pure_support:invoke(binary_to_float,[1.0]),{continued,Result};
probe(binary_to_float_badtype_3) -> Result=erlang370_pure_support:invoke(binary_to_float,[{}]),{continued,Result};
probe(binary_to_float_badtype_4) -> Result=erlang370_pure_support:invoke(binary_to_float,[[]]),{continued,Result};
probe(binary_to_float_badtype_5) -> Result=erlang370_pure_support:invoke(binary_to_float,[<<1:1>>]),{continued,Result}.
