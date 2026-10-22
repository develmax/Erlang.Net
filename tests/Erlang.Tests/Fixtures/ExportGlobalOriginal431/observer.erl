-module(beam_export_global431).
-export([run/0,capture/1,literal_pair/0]).
-compile({no_inline,[dynamic_tuple/1,runtime_pair/1]}).

run() -> [{Name,observe(Name)} || Name <- [loaded,missing_stub]] ++
    [{nested,nested()}].

observe(Name) ->
    Literal=literal(Name),{Module,Function,Arity}=signature(Name),
    Dynamic=erlang:make_fun(Module,Function,Arity),
    Decoded=binary_to_term(term_to_binary(Dynamic)),
    Keep=erts_debug:copy_shared(Dynamic,false),
    Copy=erts_debug:copy_shared(Dynamic,true),
    CopyFalse=erts_debug:copy_shared(Copy,false),
    CopyTrue=erts_debug:copy_shared(Copy,true),
    ImportedCopy=binary_to_term(term_to_binary(Copy)),
    Values=[Literal,Dynamic,Decoded,Keep,Copy,CopyFalse,CopyTrue],
    {erts_debug:same(Literal,Dynamic),erts_debug:same(Dynamic,Decoded),
     erts_debug:same(Dynamic,Keep),erts_debug:same(Dynamic,Copy),
     erts_debug:same(Copy,CopyFalse),erts_debug:same(Copy,CopyTrue),
     erts_debug:same(ImportedCopy,Dynamic),
     [erts_debug:flat_size(Value) || Value <- Values],
     [erts_debug:size_shared(Value) || Value <- Values],
     [erts_internal:term_type(Value) || Value <- Values],
     [invoke(Value,Name) || Value <- Values]}.

nested() ->
    Canonical=erlang:make_fun(erlang,self,0),
    Literal=literal_pair(),
    Dynamic=dynamic_tuple({Canonical,Canonical}),
    Fresh=erts_debug:copy_shared(Canonical,true),
    RepeatedFresh=runtime_pair(Fresh),
    {erts_debug:same(Literal,literal_pair()),
     [{Name,root(Root)} || {Name,Root} <-
         [{literal,Literal},{dynamic,Dynamic},{repeated_fresh,RepeatedFresh}]]}.

dynamic_tuple(Value) -> binary_to_term(term_to_binary(Value)).
runtime_pair(Value) -> {Value,Value}.

literal_pair() -> {fun erlang:self/0,fun erlang:self/0}.

root(Root) ->
    {root_state(Root),
     [{Flag,root_copy(Root,erts_debug:copy_shared(Root,Flag))} || Flag <- [false,true]]}.

root_copy(Source,Copy) ->
    {erts_debug:same(Source,Copy),root_state(Copy)}.

root_state(Root) ->
    First=element(1,Root),Second=element(2,Root),
    {erts_debug:same(First,Second),erts_debug:flat_size(Root),erts_debug:size_shared(Root),
     [erts_debug:flat_size(Child) || Child <- [First,Second]],
     [erts_debug:size_shared(Child) || Child <- [First,Second]],
     [invoke(Child,loaded) || Child <- [First,Second]]}.

invoke(Function,loaded) -> Function()=:=self();
invoke(Function,missing_stub) ->
    try Function() of _ -> unexpected catch error:undef -> undef end.

literal(loaded) -> fun erlang:self/0;
literal(missing_stub) -> fun export_global431_missing:run/0.
signature(loaded) -> {erlang,self,0};
signature(missing_stub) -> {export_global431_missing,run,0}.

capture([Directory]) ->
    ok=prim_file:write_file(Directory++"/profile.etf",term_to_binary({erlang:system_info(wordsize),erlang:system_info(otp_release)},[{minor_version,2}])),
    ok=prim_file:write_file(Directory++"/values.etf",term_to_binary(run(),[{minor_version,2}])),
    erlang:halt(0).
