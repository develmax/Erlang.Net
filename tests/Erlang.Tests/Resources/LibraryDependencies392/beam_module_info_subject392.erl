-module(beam_module_info_subject392).
-export([worker/2,probe/1,info1/1,info2/2,value/0]).
-fixture_metadata([{nested,{one,[2,3]}},{unicode,"metadata"}]).

worker(Parent,Case) ->
    Outcome=try probe(Case) of Value->{returned,Value}
            catch Class:Reason:Stack->{caught,Class,Reason,Stack} end,
    Parent ! {Case,Outcome},ok.

probe(full) -> info1(?MODULE);
probe(module) -> info2(?MODULE,module);
probe(exports) -> info2(?MODULE,exports);
probe(functions) -> info2(?MODULE,functions);
probe(attributes) -> info2(?MODULE,attributes);
probe(compile) -> info2(?MODULE,compile);
probe(md5) -> info2(?MODULE,md5);
probe(nifs) -> info2(?MODULE,nifs);
probe(native) -> info2(?MODULE,native);
probe(native_addresses) -> info2(?MODULE,native_addresses);
probe(generated_full) -> ?MODULE:module_info();
probe(generated_attributes) -> ?MODULE:module_info(attributes);
probe(missing_one) -> info1(beam_module_info_missing392);
probe(missing_two) -> info2(beam_module_info_missing392,exports);
probe(nonatom_one) -> info1({not_module,[]});
probe(nonatom_two) -> info2(42,exports);
probe(invalid_key) -> info2(?MODULE,not_a_metadata_key392);
probe(nonatom_key) -> info2(?MODULE,{exports,[]});
probe(improper_key) -> info2(?MODULE,[exports|not_list]);
probe(generated_bad_key) -> ?MODULE:module_info(not_a_metadata_key392);
probe(missing_generated) -> beam_module_info_missing392:module_info();
probe(missing_function) -> ?MODULE:missing_function392().

info1(Module) -> erlang:get_module_info(Module).
info2(Module,Key) -> erlang:get_module_info(Module,Key).
value() -> {fixture,[1,2,3]}.
