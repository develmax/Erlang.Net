-module(ordsets_core_observe).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [new_empty,
            is_set_0,
            is_set_1,
            is_set_2,
            is_set_3,
            is_set_4,
            is_set_5,
            is_set_6,
            is_set_7,
            is_set_8,
            is_set_9,
            is_set_10,
            is_set_11,
            is_set_12,
            is_set_13,
            is_set_14,
            is_set_15,
            size_0,
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
            is_empty_0,
            is_empty_1,
            is_empty_2,
            is_empty_3,
            is_empty_4,
            is_empty_5,
            is_empty_6,
            is_empty_7,
            is_empty_8,
            is_empty_9,
            is_empty_10,
            is_empty_11,
            is_empty_12,
            is_empty_13,
            is_empty_14,
            is_empty_15,
            to_list_0,
            to_list_1,
            to_list_2,
            to_list_3,
            to_list_4,
            to_list_5,
            to_list_6,
            to_list_7,
            to_list_8,
            to_list_9,
            to_list_10,
            to_list_11,
            to_list_12,
            to_list_13,
            to_list_14,
            to_list_15,
            is_equal_0,
            is_equal_1,
            is_equal_2,
            is_equal_3,
            is_equal_4,
            is_equal_5,
            is_equal_6,
            is_equal_7,
            is_equal_8,
            is_equal_9,
            is_equal_10,
            is_equal_11,
            is_equal_12,
            is_element_0,
            is_element_1,
            is_element_2,
            is_element_3,
            is_element_4,
            is_element_5,
            is_element_6,
            is_element_7,
            is_element_8,
            is_element_9,
            is_element_10,
            is_element_11,
            is_element_12,
            is_element_13,
            is_element_14,
            is_element_15,
            is_element_16,
            add_element_0,
            add_element_1,
            add_element_2,
            add_element_3,
            add_element_4,
            add_element_5,
            add_element_6,
            add_element_7,
            add_element_8,
            add_element_9,
            add_element_10,
            add_element_11,
            add_element_12,
            add_element_13,
            add_element_14,
            add_element_15,
            add_element_16].
run() -> ordsets_core_support:run(ordsets_core_observe,cases()).
run_stacks() -> ordsets_core_support:profiles(ordsets_core_observe,cases()).
probe(new_empty) -> erlang:apply(ordsets,new,[]);
probe(is_set_0) -> erlang:apply(ordsets,is_set,[[]]);
probe(is_set_1) -> erlang:apply(ordsets,is_set,[[a]]);
probe(is_set_2) -> erlang:apply(ordsets,is_set,[[a,b,c]]);
probe(is_set_3) -> erlang:apply(ordsets,is_set,[[c,b,a]]);
probe(is_set_4) -> erlang:apply(ordsets,is_set,[[a,a]]);
probe(is_set_5) -> erlang:apply(ordsets,is_set,[[1,1.0]]);
probe(is_set_6) -> erlang:apply(ordsets,is_set,[[1.0,2]]);
probe(is_set_7) -> erlang:apply(ordsets,is_set,[[a|tail]]);
probe(is_set_8) -> erlang:apply(ordsets,is_set,[[a,b|tail]]);
probe(is_set_9) -> erlang:apply(ordsets,is_set,[[b,a|tail]]);
probe(is_set_10) -> erlang:apply(ordsets,is_set,[[a,a|tail]]);
probe(is_set_11) -> erlang:apply(ordsets,is_set,[tail]);
probe(is_set_12) -> erlang:apply(ordsets,is_set,[{}]);
probe(is_set_13) -> erlang:apply(ordsets,is_set,[<<1>>]);
probe(is_set_14) -> erlang:apply(ordsets,is_set,[7]);
probe(is_set_15) -> erlang:apply(ordsets,is_set,[[-2,0,1.5,a,{a},#{a=>1},[],[a],<<>>]]);
probe(size_0) -> erlang:apply(ordsets,size,[[]]);
probe(size_1) -> erlang:apply(ordsets,size,[[a]]);
probe(size_2) -> erlang:apply(ordsets,size,[[a,b,c]]);
probe(size_3) -> erlang:apply(ordsets,size,[[c,b,a]]);
probe(size_4) -> erlang:apply(ordsets,size,[[a,a]]);
probe(size_5) -> erlang:apply(ordsets,size,[[1,1.0]]);
probe(size_6) -> erlang:apply(ordsets,size,[[1.0,2]]);
probe(size_7) -> erlang:apply(ordsets,size,[[a|tail]]);
probe(size_8) -> erlang:apply(ordsets,size,[[a,b|tail]]);
probe(size_9) -> erlang:apply(ordsets,size,[[b,a|tail]]);
probe(size_10) -> erlang:apply(ordsets,size,[[a,a|tail]]);
probe(size_11) -> erlang:apply(ordsets,size,[tail]);
probe(size_12) -> erlang:apply(ordsets,size,[{}]);
probe(size_13) -> erlang:apply(ordsets,size,[<<1>>]);
probe(size_14) -> erlang:apply(ordsets,size,[7]);
probe(size_15) -> erlang:apply(ordsets,size,[[-2,0,1.5,a,{a},#{a=>1},[],[a],<<>>]]);
probe(is_empty_0) -> erlang:apply(ordsets,is_empty,[[]]);
probe(is_empty_1) -> erlang:apply(ordsets,is_empty,[[a]]);
probe(is_empty_2) -> erlang:apply(ordsets,is_empty,[[a,b,c]]);
probe(is_empty_3) -> erlang:apply(ordsets,is_empty,[[c,b,a]]);
probe(is_empty_4) -> erlang:apply(ordsets,is_empty,[[a,a]]);
probe(is_empty_5) -> erlang:apply(ordsets,is_empty,[[1,1.0]]);
probe(is_empty_6) -> erlang:apply(ordsets,is_empty,[[1.0,2]]);
probe(is_empty_7) -> erlang:apply(ordsets,is_empty,[[a|tail]]);
probe(is_empty_8) -> erlang:apply(ordsets,is_empty,[[a,b|tail]]);
probe(is_empty_9) -> erlang:apply(ordsets,is_empty,[[b,a|tail]]);
probe(is_empty_10) -> erlang:apply(ordsets,is_empty,[[a,a|tail]]);
probe(is_empty_11) -> erlang:apply(ordsets,is_empty,[tail]);
probe(is_empty_12) -> erlang:apply(ordsets,is_empty,[{}]);
probe(is_empty_13) -> erlang:apply(ordsets,is_empty,[<<1>>]);
probe(is_empty_14) -> erlang:apply(ordsets,is_empty,[7]);
probe(is_empty_15) -> erlang:apply(ordsets,is_empty,[[-2,0,1.5,a,{a},#{a=>1},[],[a],<<>>]]);
probe(to_list_0) -> erlang:apply(ordsets,to_list,[[]]);
probe(to_list_1) -> erlang:apply(ordsets,to_list,[[a]]);
probe(to_list_2) -> erlang:apply(ordsets,to_list,[[a,b,c]]);
probe(to_list_3) -> erlang:apply(ordsets,to_list,[[c,b,a]]);
probe(to_list_4) -> erlang:apply(ordsets,to_list,[[a,a]]);
probe(to_list_5) -> erlang:apply(ordsets,to_list,[[1,1.0]]);
probe(to_list_6) -> erlang:apply(ordsets,to_list,[[1.0,2]]);
probe(to_list_7) -> erlang:apply(ordsets,to_list,[[a|tail]]);
probe(to_list_8) -> erlang:apply(ordsets,to_list,[[a,b|tail]]);
probe(to_list_9) -> erlang:apply(ordsets,to_list,[[b,a|tail]]);
probe(to_list_10) -> erlang:apply(ordsets,to_list,[[a,a|tail]]);
probe(to_list_11) -> erlang:apply(ordsets,to_list,[tail]);
probe(to_list_12) -> erlang:apply(ordsets,to_list,[{}]);
probe(to_list_13) -> erlang:apply(ordsets,to_list,[<<1>>]);
probe(to_list_14) -> erlang:apply(ordsets,to_list,[7]);
probe(to_list_15) -> erlang:apply(ordsets,to_list,[[-2,0,1.5,a,{a},#{a=>1},[],[a],<<>>]]);
probe(is_equal_0) -> erlang:apply(ordsets,is_equal,[[],[]]);
probe(is_equal_1) -> erlang:apply(ordsets,is_equal,[[a,b],[a,b]]);
probe(is_equal_2) -> erlang:apply(ordsets,is_equal,[[a,b],[b,a]]);
probe(is_equal_3) -> erlang:apply(ordsets,is_equal,[[1],[1.0]]);
probe(is_equal_4) -> erlang:apply(ordsets,is_equal,[[{1,[2]}],[{1.0,[2.0]}]]);
probe(is_equal_5) -> erlang:apply(ordsets,is_equal,[[a|tail],[a|tail]]);
probe(is_equal_6) -> erlang:apply(ordsets,is_equal,[[1|2],[1.0|2.0]]);
probe(is_equal_7) -> erlang:apply(ordsets,is_equal,[[a|tail],[a|other]]);
probe(is_equal_8) -> erlang:apply(ordsets,is_equal,[bad,[]]);
probe(is_equal_9) -> erlang:apply(ordsets,is_equal,[[],bad]);
probe(is_equal_10) -> erlang:apply(ordsets,is_equal,[{},{}]);
probe(is_equal_11) -> erlang:apply(ordsets,is_equal,[<<>>,[]]);
probe(is_equal_12) -> erlang:apply(ordsets,is_equal,[[a],[]]);
probe(is_element_0) -> erlang:apply(ordsets,is_element,[a,[]]);
probe(is_element_1) -> erlang:apply(ordsets,is_element,[a,[a,b]]);
probe(is_element_2) -> erlang:apply(ordsets,is_element,[b,[a,b]]);
probe(is_element_3) -> erlang:apply(ordsets,is_element,[c,[a,b]]);
probe(is_element_4) -> erlang:apply(ordsets,is_element,[0,[1,2]]);
probe(is_element_5) -> erlang:apply(ordsets,is_element,[1.0,[1,2]]);
probe(is_element_6) -> erlang:apply(ordsets,is_element,[1,[1.0,2]]);
probe(is_element_7) -> erlang:apply(ordsets,is_element,[{1},[{1.0}]]);
probe(is_element_8) -> erlang:apply(ordsets,is_element,[a,[a|tail]]);
probe(is_element_9) -> erlang:apply(ordsets,is_element,[a,[b|tail]]);
probe(is_element_10) -> erlang:apply(ordsets,is_element,[c,[a,b|tail]]);
probe(is_element_11) -> erlang:apply(ordsets,is_element,[a,tail]);
probe(is_element_12) -> erlang:apply(ordsets,is_element,[a,{}]);
probe(is_element_13) -> erlang:apply(ordsets,is_element,[a,7]);
probe(is_element_14) -> erlang:apply(ordsets,is_element,[a,[b,a]]);
probe(is_element_15) -> erlang:apply(ordsets,is_element,[a,[a,a]]);
probe(is_element_16) -> erlang:apply(ordsets,is_element,[z,[a,b,c,d,e,f,g,h,i,j|tail]]);
probe(add_element_0) -> erlang:apply(ordsets,add_element,[a,[]]);
probe(add_element_1) -> erlang:apply(ordsets,add_element,[a,[a,b]]);
probe(add_element_2) -> erlang:apply(ordsets,add_element,[b,[a,b]]);
probe(add_element_3) -> erlang:apply(ordsets,add_element,[c,[a,b]]);
probe(add_element_4) -> erlang:apply(ordsets,add_element,[0,[1,2]]);
probe(add_element_5) -> erlang:apply(ordsets,add_element,[1.0,[1,2]]);
probe(add_element_6) -> erlang:apply(ordsets,add_element,[1,[1.0,2]]);
probe(add_element_7) -> erlang:apply(ordsets,add_element,[{1},[{1.0}]]);
probe(add_element_8) -> erlang:apply(ordsets,add_element,[a,[a|tail]]);
probe(add_element_9) -> erlang:apply(ordsets,add_element,[a,[b|tail]]);
probe(add_element_10) -> erlang:apply(ordsets,add_element,[c,[a,b|tail]]);
probe(add_element_11) -> erlang:apply(ordsets,add_element,[a,tail]);
probe(add_element_12) -> erlang:apply(ordsets,add_element,[a,{}]);
probe(add_element_13) -> erlang:apply(ordsets,add_element,[a,7]);
probe(add_element_14) -> erlang:apply(ordsets,add_element,[a,[b,a]]);
probe(add_element_15) -> erlang:apply(ordsets,add_element,[a,[a,a]]);
probe(add_element_16) -> erlang:apply(ordsets,add_element,[z,[a,b,c,d,e,f,g,h,i,j|tail]]).
