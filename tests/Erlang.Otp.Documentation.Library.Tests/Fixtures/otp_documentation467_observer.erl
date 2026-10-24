-module(otp_documentation467_observer).
-file("otp_documentation467_observer.erl", 1).
-export([worker/1, run/0]).

worker(Parent) ->
    Parent ! {finished, run()}.

run() ->
    [observe(eunit_empty_string, fun() -> eunit_lib:is_string([]) end),
     observe(eunit_unicode, fun() -> eunit_lib:is_string([65, 16#1f600]) end),
     observe(eunit_bad_codepoint, fun() -> eunit_lib:is_string([16#110000]) end),
     observe(eunit_improper, fun() -> eunit_lib:is_string([65 | invalid]) end),
     observe(eunit_unique, fun() -> eunit_lib:uniq([a,a,b,b,a]) end),
     observe(eunit_unique_bad, fun() -> eunit_lib:uniq(invalid) end),
     observe(eunit_deep_list, fun() -> eunit_lib:dlist_next([[], [a], b]) end),
     observe(eunit_node, fun() -> eunit_lib:split_node('guest@host') end),
     observe(eunit_default_host, fun() -> eunit_lib:split_node(guest) end),
     observe(eunit_node_bad, fun() -> eunit_lib:split_node(42) end),
     observe(eunit_trie_empty, fun() -> eunit_lib:trie_match([a], eunit_lib:trie_new()) end),
     observe(eunit_trie_exact, fun() -> eunit_lib:trie_match([a,b], trie()) end),
     observe(eunit_trie_prefix, fun() -> eunit_lib:trie_match([a,b,c], trie()) end),
     observe(eunit_trie_missing, fun() -> eunit_lib:trie_match([a,c], trie()) end),
     observe(edoc_count, fun() -> edoc_lib:count(a, [a,b,a,c,a]) end),
     observe(edoc_lines, fun() -> edoc_lib:lines("one\ntwo\n") end),
     observe(edoc_split, fun() -> edoc_lib:split_at("a=b=c", $=) end),
     observe(edoc_sentence, fun() -> edoc_lib:split_at_stop("First. Second") end),
     observe(edoc_segment, fun() -> edoc_lib:segment([a,b,c,d,e], 2) end),
     observe(edoc_transpose, fun() -> edoc_lib:transpose([[1,2],[3,4]]) end),
     observe(edoc_name, fun() -> edoc_lib:is_name("valid_name1") end),
     observe(edoc_bad_name, fun() -> edoc_lib:is_name("Invalid") end),
     observe(edoc_bad_count, fun() -> edoc_lib:count(a, invalid) end),
     observe(edoc_bad_lines, fun() -> edoc_lib:lines(invalid) end),
     observe(xml_text, fun() -> xmerl_lib:export_text(["<&", <<">">>]) end),
     observe(xml_attribute, fun() -> xmerl_lib:export_attribute("a\"b&c<") end),
     observe(xml_flatten, fun() -> xmerl_lib:flatten_text(["a", <<"bc">>]) end),
     observe(xml_lower, fun() -> xmerl_lib:to_lower("AbC") end),
     observe(xml_name_char, fun() -> xmerl_lib:is_namechar($a) end),
     observe(xml_bad_text, fun() -> xmerl_lib:export_text(invalid) end)].

trie() ->
    eunit_lib:trie_store([a,b], eunit_lib:trie_new()).

observe(Name, Function) ->
    try Function() of
        Value -> {Name, {returned, Value}}
    catch
        Class:Reason:Stack -> {Name, {caught, Class, Reason, Stack}}
    end.
