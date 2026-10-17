-module(text_binary_callback_contract).
-export([load/1,observe/2,legacy/1,widths/0,run/0]).
load(Path) -> erlang:load_nif(Path, []).
observe(_,_) -> erlang:nif_error(not_loaded).
legacy(_) -> erlang:nif_error(not_loaded).
widths() -> erlang:nif_error(not_loaded).
wrong_type_tuple() -> {not_atom}.
capture(F) -> try F() of V -> {returned,V} catch C:R -> {raised,C,R} end.
run() ->
    Rows = [
        {tuple_access,[{V,capture(fun()->text_binary_callback_contract:observe(0,V) end)} || V <- [{}, {a,42}, wrong]]},
        {list_length,[{V,capture(fun()->text_binary_callback_contract:observe(1,V) end)} || V <- [[],[1,2], [1|wrong], wrong]]},
        {atom_latin1,[{V,capture(fun()->text_binary_callback_contract:observe(2,V) end)} || V <- [hello, 'é', '🙂', wrong_type_tuple()]]},
        {atom_utf8,[{V,capture(fun()->text_binary_callback_contract:observe(3,V) end)} || V <- [hello, 'é', '🙂', wrong_type_tuple()]]},
        {atom_length_latin1,[{V,capture(fun()->text_binary_callback_contract:observe(4,V) end)} || V <- [hello,'é','🙂',wrong_type_tuple()]]},
        {atom_length_utf8,[{V,capture(fun()->text_binary_callback_contract:observe(5,V) end)} || V <- [hello,'é','🙂',wrong_type_tuple()]]},
        {string_latin1,[{V,capture(fun()->text_binary_callback_contract:observe(6,V) end)} || V <- [[],[65,66,67,68,69],[65,233,66],[65,16#1f642,66],[65|wrong]]]},
        {string_utf8,[{V,capture(fun()->text_binary_callback_contract:observe(7,V) end)} || V <- [[],[65,66,67,68,69],[65,233,66],[65,16#1f642,66],[65|wrong]]]},
        {atom_make,[{V,capture(fun()->text_binary_callback_contract:observe(8,V) end)} || V <- [unused]]},
        {atom_existing,[{V,capture(fun()->text_binary_callback_contract:observe(9,V) end)} || V <- [unused]]},
        {string_make,[{V,capture(fun()->text_binary_callback_contract:observe(10,V) end)} || V <- [unused]]},
        {latewrite_63_64_65,[{V,capture(fun()->text_binary_callback_contract:observe(11,V) end)} || V <- [unused]]},
        {alloc_realloc_transfer,[{V,capture(fun()->text_binary_callback_contract:observe(12,V) end)} || V <- [unused]]},
        {readonly_realloc,[{V,capture(fun()->text_binary_callback_contract:observe(13,V) end)} || V <- [<<>>,<<1,2,3>>,wrong]]},
        {inspect_iolist,[{V,capture(fun()->text_binary_callback_contract:observe(14,V) end)} || V <- [[],[65,<<66,67>>], [65|<<66>>], [65|wrong], <<1:1>>,wrong]]},
        {release,[{V,capture(fun()->text_binary_callback_contract:observe(15,V) end)} || V <- [unused]]}
        ,{pending_exception_latewrite,[{unused,capture(fun()->text_binary_callback_contract:observe(16,unused) end)}]}
        ,{iolist_snapshot_latewrite,[{unused,capture(fun()->text_binary_callback_contract:observe(17,unused) end)}]}
        ,{native_alloc_first_call,[{unused,capture(fun()->text_binary_callback_contract:observe(18,unused) end)}]}
        ,{native_transfer_next_call,[{unused,capture(fun()->text_binary_callback_contract:observe(19,unused) end)}]}
    ] ++ [
        {legacy_utf8,capture(fun()->text_binary_callback_contract:legacy(0) end)},
        {legacy_invalid_utf8,capture(fun()->text_binary_callback_contract:legacy(1) end)},
        {legacy_long_latin1,capture(fun()->text_binary_callback_contract:legacy(2) end)},
        {legacy_long_utf8,capture(fun()->text_binary_callback_contract:legacy(3) end)}
    ],
    {text_binary_callback_contract,Rows,text_binary_callback_contract:widths()}.
