-module(contract_reaudit487).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [bit_size_bits_0,
            bit_size_bits_1,
            bit_size_bits_7,
            bit_size_bits_8,
            bit_size_bits_9,
            bit_size_bits_15,
            bit_size_bits_16,
            bit_size_bits_17,
            bit_size_bits_63,
            bit_size_bits_64,
            bit_size_bits_65,
            bit_size_bits_1024,
            bit_size_invalid_0,
            bit_size_invalid_1,
            bit_size_invalid_2,
            bit_size_invalid_3,
            bit_size_invalid_4,
            bit_size_invalid_5,
            bit_size_invalid_6,
            bit_size_invalid_7,
            bit_size_invalid_8,
            bit_size_invalid_9,
            bit_size_invalid_10,
            bit_size_invalid_11,
            bit_size_subbinary,
            bit_size_unaligned,
            byte_size_bits_0,
            byte_size_bits_1,
            byte_size_bits_7,
            byte_size_bits_8,
            byte_size_bits_9,
            byte_size_bits_15,
            byte_size_bits_16,
            byte_size_bits_17,
            byte_size_bits_63,
            byte_size_bits_64,
            byte_size_bits_65,
            byte_size_bits_1024,
            byte_size_invalid_0,
            byte_size_invalid_1,
            byte_size_invalid_2,
            byte_size_invalid_3,
            byte_size_invalid_4,
            byte_size_invalid_5,
            byte_size_invalid_6,
            byte_size_invalid_7,
            byte_size_invalid_8,
            byte_size_invalid_9,
            byte_size_invalid_10,
            byte_size_invalid_11,
            byte_size_subbinary,
            byte_size_unaligned,
            function_52,
            function_53,
            function_54,
            function_55,
            function_56,
            function_57,
            function_58,
            function_59,
            function_60,
            function_61,
            function_62,
            function_63,
            function_64,
            function_65,
            function_66,
            function_67,
            function_68,
            function_69,
            function_invalid_70,
            function_invalid_71,
            function_invalid_72,
            function_invalid_73,
            function_invalid_74,
            function_invalid_75,
            function_invalid_76,
            function_invalid_77,
            function_invalid_78,
            function_invalid_79,
            function_invalid_80,
            function_invalid_81,
            function_invalid_82,
            function_invalid_83].
probe(bit_size_bits_0) -> erlang:apply(erlang,opaque(bit_size),[<<0:0>>]);
probe(bit_size_bits_1) -> erlang:apply(erlang,opaque(bit_size),[<<0:1>>]);
probe(bit_size_bits_7) -> erlang:apply(erlang,opaque(bit_size),[<<0:7>>]);
probe(bit_size_bits_8) -> erlang:apply(erlang,opaque(bit_size),[<<0:8>>]);
probe(bit_size_bits_9) -> erlang:apply(erlang,opaque(bit_size),[<<0:9>>]);
probe(bit_size_bits_15) -> erlang:apply(erlang,opaque(bit_size),[<<0:15>>]);
probe(bit_size_bits_16) -> erlang:apply(erlang,opaque(bit_size),[<<0:16>>]);
probe(bit_size_bits_17) -> erlang:apply(erlang,opaque(bit_size),[<<0:17>>]);
probe(bit_size_bits_63) -> erlang:apply(erlang,opaque(bit_size),[<<0:63>>]);
probe(bit_size_bits_64) -> erlang:apply(erlang,opaque(bit_size),[<<0:64>>]);
probe(bit_size_bits_65) -> erlang:apply(erlang,opaque(bit_size),[<<0:65>>]);
probe(bit_size_bits_1024) -> erlang:apply(erlang,opaque(bit_size),[<<0:1024>>]);
probe(bit_size_invalid_0) -> erlang:apply(erlang,opaque(bit_size),[a]);
probe(bit_size_invalid_1) -> erlang:apply(erlang,opaque(bit_size),[0]);
probe(bit_size_invalid_2) -> erlang:apply(erlang,opaque(bit_size),[1.0]);
probe(bit_size_invalid_3) -> erlang:apply(erlang,opaque(bit_size),[[]]);
probe(bit_size_invalid_4) -> erlang:apply(erlang,opaque(bit_size),[[a]]);
probe(bit_size_invalid_5) -> erlang:apply(erlang,opaque(bit_size),[[a|tail]]);
probe(bit_size_invalid_6) -> erlang:apply(erlang,opaque(bit_size),[{}]);
probe(bit_size_invalid_7) -> erlang:apply(erlang,opaque(bit_size),[#{}]);
probe(bit_size_invalid_8) -> erlang:apply(erlang,opaque(bit_size),[binary_to_term(<<131,103,100,0,6,"oracle",0,0,0,1,0,0,0,0,0>>)]);
probe(bit_size_invalid_9) -> erlang:apply(erlang,opaque(bit_size),[binary_to_term(<<131,101,100,0,6,"oracle",0,0,0,1,0>>)]);
probe(bit_size_invalid_10) -> erlang:apply(erlang,opaque(bit_size),[fun erlang:abs/1]);
probe(bit_size_invalid_11) -> erlang:apply(erlang,opaque(bit_size),[binary_to_term(<<131,102,100,0,6,"oracle",0,0,0,1,0>>)]);
probe(bit_size_subbinary) -> B = <<0,1,2,3,4>>, erlang:apply(erlang,opaque(bit_size),[binary:part(B,1,3)]);
probe(bit_size_unaligned) -> <<_:3,B:13/bitstring,_/bitstring>> = <<1,2,3>>, erlang:apply(erlang,opaque(bit_size),[B]);
probe(byte_size_bits_0) -> erlang:apply(erlang,opaque(byte_size),[<<0:0>>]);
probe(byte_size_bits_1) -> erlang:apply(erlang,opaque(byte_size),[<<0:1>>]);
probe(byte_size_bits_7) -> erlang:apply(erlang,opaque(byte_size),[<<0:7>>]);
probe(byte_size_bits_8) -> erlang:apply(erlang,opaque(byte_size),[<<0:8>>]);
probe(byte_size_bits_9) -> erlang:apply(erlang,opaque(byte_size),[<<0:9>>]);
probe(byte_size_bits_15) -> erlang:apply(erlang,opaque(byte_size),[<<0:15>>]);
probe(byte_size_bits_16) -> erlang:apply(erlang,opaque(byte_size),[<<0:16>>]);
probe(byte_size_bits_17) -> erlang:apply(erlang,opaque(byte_size),[<<0:17>>]);
probe(byte_size_bits_63) -> erlang:apply(erlang,opaque(byte_size),[<<0:63>>]);
probe(byte_size_bits_64) -> erlang:apply(erlang,opaque(byte_size),[<<0:64>>]);
probe(byte_size_bits_65) -> erlang:apply(erlang,opaque(byte_size),[<<0:65>>]);
probe(byte_size_bits_1024) -> erlang:apply(erlang,opaque(byte_size),[<<0:1024>>]);
probe(byte_size_invalid_0) -> erlang:apply(erlang,opaque(byte_size),[a]);
probe(byte_size_invalid_1) -> erlang:apply(erlang,opaque(byte_size),[0]);
probe(byte_size_invalid_2) -> erlang:apply(erlang,opaque(byte_size),[1.0]);
probe(byte_size_invalid_3) -> erlang:apply(erlang,opaque(byte_size),[[]]);
probe(byte_size_invalid_4) -> erlang:apply(erlang,opaque(byte_size),[[a]]);
probe(byte_size_invalid_5) -> erlang:apply(erlang,opaque(byte_size),[[a|tail]]);
probe(byte_size_invalid_6) -> erlang:apply(erlang,opaque(byte_size),[{}]);
probe(byte_size_invalid_7) -> erlang:apply(erlang,opaque(byte_size),[#{}]);
probe(byte_size_invalid_8) -> erlang:apply(erlang,opaque(byte_size),[binary_to_term(<<131,103,100,0,6,"oracle",0,0,0,1,0,0,0,0,0>>)]);
probe(byte_size_invalid_9) -> erlang:apply(erlang,opaque(byte_size),[binary_to_term(<<131,101,100,0,6,"oracle",0,0,0,1,0>>)]);
probe(byte_size_invalid_10) -> erlang:apply(erlang,opaque(byte_size),[fun erlang:abs/1]);
probe(byte_size_invalid_11) -> erlang:apply(erlang,opaque(byte_size),[binary_to_term(<<131,102,100,0,6,"oracle",0,0,0,1,0>>)]);
probe(byte_size_subbinary) -> B = <<0,1,2,3,4>>, erlang:apply(erlang,opaque(byte_size),[binary:part(B,1,3)]);
probe(byte_size_unaligned) -> <<_:3,B:13/bitstring,_/bitstring>> = <<1,2,3>>, erlang:apply(erlang,opaque(byte_size),[B]);
probe(function_52) -> erlang:apply(erlang,opaque(is_function),[fun() -> ok end,0]);
probe(function_53) -> erlang:apply(erlang,opaque(is_function),[fun() -> ok end,1]);
probe(function_54) -> erlang:apply(erlang,opaque(is_function),[fun() -> ok end,2]);
probe(function_55) -> erlang:apply(erlang,opaque(is_function),[fun() -> ok end,255]);
probe(function_56) -> erlang:apply(erlang,opaque(is_function),[fun() -> ok end,256]);
probe(function_57) -> erlang:apply(erlang,opaque(is_function),[fun() -> ok end,18446744073709551616]);
probe(function_58) -> erlang:apply(erlang,opaque(is_function),[fun erlang:abs/1,0]);
probe(function_59) -> erlang:apply(erlang,opaque(is_function),[fun erlang:abs/1,1]);
probe(function_60) -> erlang:apply(erlang,opaque(is_function),[fun erlang:abs/1,2]);
probe(function_61) -> erlang:apply(erlang,opaque(is_function),[fun erlang:abs/1,255]);
probe(function_62) -> erlang:apply(erlang,opaque(is_function),[fun erlang:abs/1,256]);
probe(function_63) -> erlang:apply(erlang,opaque(is_function),[fun erlang:abs/1,18446744073709551616]);
probe(function_64) -> erlang:apply(erlang,opaque(is_function),[a,0]);
probe(function_65) -> erlang:apply(erlang,opaque(is_function),[a,1]);
probe(function_66) -> erlang:apply(erlang,opaque(is_function),[a,2]);
probe(function_67) -> erlang:apply(erlang,opaque(is_function),[a,255]);
probe(function_68) -> erlang:apply(erlang,opaque(is_function),[a,256]);
probe(function_69) -> erlang:apply(erlang,opaque(is_function),[a,18446744073709551616]);
probe(function_invalid_70) -> erlang:apply(erlang,opaque(is_function),[a,-1]);
probe(function_invalid_71) -> erlang:apply(erlang,opaque(is_function),[a,-18446744073709551616]);
probe(function_invalid_72) -> erlang:apply(erlang,opaque(is_function),[a,a]);
probe(function_invalid_73) -> erlang:apply(erlang,opaque(is_function),[a,1.0]);
probe(function_invalid_74) -> erlang:apply(erlang,opaque(is_function),[a,[]]);
probe(function_invalid_75) -> erlang:apply(erlang,opaque(is_function),[a,{}]);
probe(function_invalid_76) -> erlang:apply(erlang,opaque(is_function),[a,#{}]);
probe(function_invalid_77) -> erlang:apply(erlang,opaque(is_function),[fun erlang:abs/1,-1]);
probe(function_invalid_78) -> erlang:apply(erlang,opaque(is_function),[fun erlang:abs/1,-18446744073709551616]);
probe(function_invalid_79) -> erlang:apply(erlang,opaque(is_function),[fun erlang:abs/1,a]);
probe(function_invalid_80) -> erlang:apply(erlang,opaque(is_function),[fun erlang:abs/1,1.0]);
probe(function_invalid_81) -> erlang:apply(erlang,opaque(is_function),[fun erlang:abs/1,[]]);
probe(function_invalid_82) -> erlang:apply(erlang,opaque(is_function),[fun erlang:abs/1,{}]);
probe(function_invalid_83) -> erlang:apply(erlang,opaque(is_function),[fun erlang:abs/1,#{}]).

opaque(Value) -> erlang:put(contract_reaudit487_arg,Value), erlang:erase(contract_reaudit487_arg).
