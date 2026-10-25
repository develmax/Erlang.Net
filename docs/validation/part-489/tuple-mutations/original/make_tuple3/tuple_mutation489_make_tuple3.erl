-module(tuple_mutation489_make_tuple3).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [make_init_268,
            make_init_269,
            make_init_270,
            make_init_271,
            make_init_272,
            make_init_273,
            make_init_275,
            make_init_276,
            make_init_277,
            make_init_278,
            make_init_279,
            make_init_280,
            make_init_282,
            make_init_283,
            make_init_284,
            make_init_285,
            make_init_286,
            make_init_287,
            make_init_289,
            make_init_290,
            make_init_291,
            make_init_292,
            make_init_293,
            make_init_294,
            make_init_296,
            make_init_297,
            make_init_298,
            make_init_299,
            make_init_300,
            make_init_301,
            make_init_303,
            make_init_304,
            make_init_305,
            make_init_306,
            make_init_307,
            make_init_308,
            make_init_310,
            make_init_311,
            make_init_312,
            make_init_313,
            make_init_314,
            make_init_315,
            make_init_317,
            make_init_318,
            make_init_319,
            make_init_320,
            make_init_321,
            make_init_322,
            make_init_324,
            make_init_325,
            make_init_326,
            make_init_327,
            make_init_328,
            make_init_329,
            make_init_331,
            make_init_332,
            make_init_333,
            make_init_334,
            make_init_335,
            make_init_336,
            make_init_338,
            make_init_339,
            make_init_340,
            make_init_341,
            make_init_342,
            make_init_343].
probe(make_init_268) -> erlang:apply(erlang,opaque(make_tuple),[-18446744073709551616,{default,<<1:9>>},[]]);
probe(make_init_269) -> erlang:apply(erlang,opaque(make_tuple),[-18446744073709551616,{default,<<1:9>>},[{1,first},{1,last}]]);
probe(make_init_270) -> erlang:apply(erlang,opaque(make_tuple),[-18446744073709551616,{default,<<1:9>>},[{0,bad}]]);
probe(make_init_271) -> erlang:apply(erlang,opaque(make_tuple),[-18446744073709551616,{default,<<1:9>>},[{2,second}]]);
probe(make_init_272) -> erlang:apply(erlang,opaque(make_tuple),[-18446744073709551616,{default,<<1:9>>},[a]]);
probe(make_init_273) -> erlang:apply(erlang,opaque(make_tuple),[-18446744073709551616,{default,<<1:9>>},[{1,first}|tail]]);
probe(make_init_275) -> erlang:apply(erlang,opaque(make_tuple),[-1,{default,<<1:9>>},[]]);
probe(make_init_276) -> erlang:apply(erlang,opaque(make_tuple),[-1,{default,<<1:9>>},[{1,first},{1,last}]]);
probe(make_init_277) -> erlang:apply(erlang,opaque(make_tuple),[-1,{default,<<1:9>>},[{0,bad}]]);
probe(make_init_278) -> erlang:apply(erlang,opaque(make_tuple),[-1,{default,<<1:9>>},[{2,second}]]);
probe(make_init_279) -> erlang:apply(erlang,opaque(make_tuple),[-1,{default,<<1:9>>},[a]]);
probe(make_init_280) -> erlang:apply(erlang,opaque(make_tuple),[-1,{default,<<1:9>>},[{1,first}|tail]]);
probe(make_init_282) -> erlang:apply(erlang,opaque(make_tuple),[0,{default,<<1:9>>},[]]);
probe(make_init_283) -> erlang:apply(erlang,opaque(make_tuple),[0,{default,<<1:9>>},[{1,first},{1,last}]]);
probe(make_init_284) -> erlang:apply(erlang,opaque(make_tuple),[0,{default,<<1:9>>},[{0,bad}]]);
probe(make_init_285) -> erlang:apply(erlang,opaque(make_tuple),[0,{default,<<1:9>>},[{2,second}]]);
probe(make_init_286) -> erlang:apply(erlang,opaque(make_tuple),[0,{default,<<1:9>>},[a]]);
probe(make_init_287) -> erlang:apply(erlang,opaque(make_tuple),[0,{default,<<1:9>>},[{1,first}|tail]]);
probe(make_init_289) -> erlang:apply(erlang,opaque(make_tuple),[1,{default,<<1:9>>},[]]);
probe(make_init_290) -> erlang:apply(erlang,opaque(make_tuple),[1,{default,<<1:9>>},[{1,first},{1,last}]]);
probe(make_init_291) -> erlang:apply(erlang,opaque(make_tuple),[1,{default,<<1:9>>},[{0,bad}]]);
probe(make_init_292) -> erlang:apply(erlang,opaque(make_tuple),[1,{default,<<1:9>>},[{2,second}]]);
probe(make_init_293) -> erlang:apply(erlang,opaque(make_tuple),[1,{default,<<1:9>>},[a]]);
probe(make_init_294) -> erlang:apply(erlang,opaque(make_tuple),[1,{default,<<1:9>>},[{1,first}|tail]]);
probe(make_init_296) -> erlang:apply(erlang,opaque(make_tuple),[2,{default,<<1:9>>},[]]);
probe(make_init_297) -> erlang:apply(erlang,opaque(make_tuple),[2,{default,<<1:9>>},[{1,first},{1,last}]]);
probe(make_init_298) -> erlang:apply(erlang,opaque(make_tuple),[2,{default,<<1:9>>},[{0,bad}]]);
probe(make_init_299) -> erlang:apply(erlang,opaque(make_tuple),[2,{default,<<1:9>>},[{2,second}]]);
probe(make_init_300) -> erlang:apply(erlang,opaque(make_tuple),[2,{default,<<1:9>>},[a]]);
probe(make_init_301) -> erlang:apply(erlang,opaque(make_tuple),[2,{default,<<1:9>>},[{1,first}|tail]]);
probe(make_init_303) -> erlang:apply(erlang,opaque(make_tuple),[33,{default,<<1:9>>},[]]);
probe(make_init_304) -> erlang:apply(erlang,opaque(make_tuple),[33,{default,<<1:9>>},[{1,first},{1,last}]]);
probe(make_init_305) -> erlang:apply(erlang,opaque(make_tuple),[33,{default,<<1:9>>},[{0,bad}]]);
probe(make_init_306) -> erlang:apply(erlang,opaque(make_tuple),[33,{default,<<1:9>>},[{2,second}]]);
probe(make_init_307) -> erlang:apply(erlang,opaque(make_tuple),[33,{default,<<1:9>>},[a]]);
probe(make_init_308) -> erlang:apply(erlang,opaque(make_tuple),[33,{default,<<1:9>>},[{1,first}|tail]]);
probe(make_init_310) -> erlang:apply(erlang,opaque(make_tuple),[257,{default,<<1:9>>},[]]);
probe(make_init_311) -> erlang:apply(erlang,opaque(make_tuple),[257,{default,<<1:9>>},[{1,first},{1,last}]]);
probe(make_init_312) -> erlang:apply(erlang,opaque(make_tuple),[257,{default,<<1:9>>},[{0,bad}]]);
probe(make_init_313) -> erlang:apply(erlang,opaque(make_tuple),[257,{default,<<1:9>>},[{2,second}]]);
probe(make_init_314) -> erlang:apply(erlang,opaque(make_tuple),[257,{default,<<1:9>>},[a]]);
probe(make_init_315) -> erlang:apply(erlang,opaque(make_tuple),[257,{default,<<1:9>>},[{1,first}|tail]]);
probe(make_init_317) -> erlang:apply(erlang,opaque(make_tuple),[16777216,{default,<<1:9>>},[]]);
probe(make_init_318) -> erlang:apply(erlang,opaque(make_tuple),[16777216,{default,<<1:9>>},[{1,first},{1,last}]]);
probe(make_init_319) -> erlang:apply(erlang,opaque(make_tuple),[16777216,{default,<<1:9>>},[{0,bad}]]);
probe(make_init_320) -> erlang:apply(erlang,opaque(make_tuple),[16777216,{default,<<1:9>>},[{2,second}]]);
probe(make_init_321) -> erlang:apply(erlang,opaque(make_tuple),[16777216,{default,<<1:9>>},[a]]);
probe(make_init_322) -> erlang:apply(erlang,opaque(make_tuple),[16777216,{default,<<1:9>>},[{1,first}|tail]]);
probe(make_init_324) -> erlang:apply(erlang,opaque(make_tuple),[18446744073709551616,{default,<<1:9>>},[]]);
probe(make_init_325) -> erlang:apply(erlang,opaque(make_tuple),[18446744073709551616,{default,<<1:9>>},[{1,first},{1,last}]]);
probe(make_init_326) -> erlang:apply(erlang,opaque(make_tuple),[18446744073709551616,{default,<<1:9>>},[{0,bad}]]);
probe(make_init_327) -> erlang:apply(erlang,opaque(make_tuple),[18446744073709551616,{default,<<1:9>>},[{2,second}]]);
probe(make_init_328) -> erlang:apply(erlang,opaque(make_tuple),[18446744073709551616,{default,<<1:9>>},[a]]);
probe(make_init_329) -> erlang:apply(erlang,opaque(make_tuple),[18446744073709551616,{default,<<1:9>>},[{1,first}|tail]]);
probe(make_init_331) -> erlang:apply(erlang,opaque(make_tuple),[a,{default,<<1:9>>},[]]);
probe(make_init_332) -> erlang:apply(erlang,opaque(make_tuple),[a,{default,<<1:9>>},[{1,first},{1,last}]]);
probe(make_init_333) -> erlang:apply(erlang,opaque(make_tuple),[a,{default,<<1:9>>},[{0,bad}]]);
probe(make_init_334) -> erlang:apply(erlang,opaque(make_tuple),[a,{default,<<1:9>>},[{2,second}]]);
probe(make_init_335) -> erlang:apply(erlang,opaque(make_tuple),[a,{default,<<1:9>>},[a]]);
probe(make_init_336) -> erlang:apply(erlang,opaque(make_tuple),[a,{default,<<1:9>>},[{1,first}|tail]]);
probe(make_init_338) -> erlang:apply(erlang,opaque(make_tuple),[1.0,{default,<<1:9>>},[]]);
probe(make_init_339) -> erlang:apply(erlang,opaque(make_tuple),[1.0,{default,<<1:9>>},[{1,first},{1,last}]]);
probe(make_init_340) -> erlang:apply(erlang,opaque(make_tuple),[1.0,{default,<<1:9>>},[{0,bad}]]);
probe(make_init_341) -> erlang:apply(erlang,opaque(make_tuple),[1.0,{default,<<1:9>>},[{2,second}]]);
probe(make_init_342) -> erlang:apply(erlang,opaque(make_tuple),[1.0,{default,<<1:9>>},[a]]);
probe(make_init_343) -> erlang:apply(erlang,opaque(make_tuple),[1.0,{default,<<1:9>>},[{1,first}|tail]]).
opaque(Value) -> put(tuple_mutation489_input,Value),erase(tuple_mutation489_input).
