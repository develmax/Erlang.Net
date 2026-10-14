-module(loader_iodata_wrapper).
-export([run/3]).

run(Name,Root,Out) ->
    put(out,Out),put(sequence,0),
    save("stage.txt",<<"entered">>),
    try
        {ok,Beam}=file:read_file(filename:join(Root,"tests/Erlang.Tests/Resources/Beam/beam_fixture_constant.beam")),
        Gzip=zlib:gzip(Beam),
        {Module,Code0,Selected,Domain}=input(Name,Beam,Gzip),
        save("original-beam.bin",Beam),save("original-gzip.bin",Gzip),
        term("input-arguments",{Module,Code0}),
        term("original-code0",Code0),
        term("independent-selected-code",{Domain,Selected}),
        Gunzip=capture("gunzip-observed",fun()->zlib:gunzip(Code0) end),
        validate_selected(Gunzip,Code0,Selected,Domain),
        Raw=capture("prepare-loading",fun()->erlang:prepare_loading(Module,Code0) end),
        Actual=project(Raw,Module,Code0,Selected,Domain),
        Expected=expected(Name),
        term("actual",Actual),term("expected",Expected),
        true=(Actual=:=Expected),
        save("stage.txt",<<"completed">>),halt(0)
    catch
        Class:Reason:Stack ->
            term("failure",{Class,Reason,Stack}),
            save("stage.txt",<<"failed">>),halt(3)
    end.

capture(Name,Function) ->
    Result=try Function() of Value -> {value,Value}
           catch Class:Reason:Stack -> {raised,Class,Reason,Stack} end,
    Sequence=get(sequence),put(sequence,Sequence+1),
    term(integer_to_list(Sequence)++"-"++Name,Result),
    Result.

term(Name,Value) ->
    save(Name++".etf",term_to_binary(Value)),
    save(Name++".txt",unicode:characters_to_binary(io_lib:format("~tp~n",[Value]))).

save(Name,Bytes) -> ok=file:write_file(filename:join(get(out),Name),Bytes).

validate_selected({value,Value},_Code0,Selected,decompressed_code) -> true=(Value=:=Selected);
validate_selected({raised,_,_,_},Code0,Selected,original_code) -> true=(Code0=:=Selected);
validate_selected(Observed,Code0,Selected,Domain) -> error({unexpected_gunzip_domain,Domain,Observed,Code0,Selected}).

project({value,Prepared},_Module,_Code0,_Selected,_Domain) when is_reference(Prepared) ->
    {value,Flag}=capture("flag",fun()->erlang:has_prepared_code_on_load(Prepared) end),
    {value,Finish}=capture("finish",fun()->erlang:finish_loading([Prepared]) end),
    {value,Invoke}=capture("invoke",fun()->beam_fixture_constant:run() end),
    {true,Flag,Finish,Invoke};
project({value,Value},_Module,_Code0,_Selected,_Domain) -> Value;
project({raised,error,badarg,Stack},Module,Code0,Selected,Domain) ->
    [{erlang,prepare_loading,[Module,StackCode],Location}|_]=Stack,
    term("selected-stack-arguments",{Module,StackCode}),
    term("raw-error-info",proplists:get_value(error_info,Location)),
    true=(StackCode=:=Selected),
    case Domain of
        original_code -> true=(StackCode=:=Code0);
        decompressed_code -> true=is_binary(StackCode)
    end,
    #{module:=erl_erts_errors}=proplists:get_value(error_info,Location),
    {error,badarg,Domain,erl_erts_errors};
project(Observed,Module,Code0,Selected,Domain) -> error({unexpected_prepare_result,Observed,Module,Code0,Selected,Domain}).

input(Name,Beam,Gzip) ->
    <<First:8,Second:8,Tail/binary>>=Gzip,
    Size=byte_size(Gzip),
    Prefix=binary:part(Gzip,0,2),
    Middle=binary:part(Gzip,2,Size-3),
    Suffix=binary:part(Gzip,Size-1,1),
    Flat=binary_to_list(Gzip),
    Nested=[[],[binary_to_list(Prefix),[Middle,[]]],binary_to_list(Suffix)],
    Improper=[First,Second|Tail],
    case Name of
        "gzip-proper-byte-list" -> {beam_fixture_constant,Flat,Beam,decompressed_code};
        "gzip-nested-iodata" -> {beam_fixture_constant,Nested,Beam,decompressed_code};
        "gzip-improper-binary-tail" -> {beam_fixture_constant,Improper,Beam,decompressed_code};
        "empty-list-fallback" -> {beam_fixture_constant,[],[],original_code};
        "truncated-gzip-list-fallback" -> C=binary_to_list(Prefix),{beam_fixture_constant,C,C,original_code};
        "nonmagic-raw-beam-list-fallback" -> C=binary_to_list(Beam),{beam_fixture_constant,C,C,original_code};
        "empty-binary-fallback" -> {beam_fixture_constant,<<>>,<<>>,original_code};
        "truncated-gzip-binary-fallback" -> {beam_fixture_constant,Prefix,Prefix,original_code};
        "invalid-iodata-leaf-fallback" -> C=[256|Flat],{beam_fixture_constant,C,C,original_code};
        "invalid-improper-atom-tail-fallback" -> C=[First|invalid_tail],{beam_fixture_constant,C,C,original_code};
        "invalid-module-gzip-proper-list" -> {7,Flat,Beam,decompressed_code};
        "invalid-module-gzip-nested" -> {7,Nested,Beam,decompressed_code};
        "invalid-module-gzip-binary-tail" -> {7,Improper,Beam,decompressed_code};
        "invalid-module-truncated-list" -> C=binary_to_list(Prefix),{7,C,C,original_code};
        "wrong-module-gzip-proper-list" -> {loader_gzip_wrong_module,Flat,Beam,decompressed_code};
        "gzip-empty-payload-list" -> C=binary_to_list(zlib:gzip(<<>>)),{beam_fixture_constant,C,<<>>,decompressed_code}
    end.

expected("gzip-proper-byte-list") -> {true,false,ok,{ok,42,[alpha,beta]}};
expected("gzip-nested-iodata") -> {true,false,ok,{ok,42,[alpha,beta]}};
expected("gzip-improper-binary-tail") -> {true,false,ok,{ok,42,[alpha,beta]}};
expected("empty-list-fallback") -> {error,badarg,original_code,erl_erts_errors};
expected("truncated-gzip-list-fallback") -> {error,badarg,original_code,erl_erts_errors};
expected("nonmagic-raw-beam-list-fallback") -> {error,badarg,original_code,erl_erts_errors};
expected("empty-binary-fallback") -> {error,badfile};
expected("truncated-gzip-binary-fallback") -> {error,badfile};
expected("invalid-iodata-leaf-fallback") -> {error,badarg,original_code,erl_erts_errors};
expected("invalid-improper-atom-tail-fallback") -> {error,badarg,original_code,erl_erts_errors};
expected("invalid-module-gzip-proper-list") -> {error,badarg,decompressed_code,erl_erts_errors};
expected("invalid-module-gzip-nested") -> {error,badarg,decompressed_code,erl_erts_errors};
expected("invalid-module-gzip-binary-tail") -> {error,badarg,decompressed_code,erl_erts_errors};
expected("invalid-module-truncated-list") -> {error,badarg,original_code,erl_erts_errors};
expected("wrong-module-gzip-proper-list") -> {error,badfile};
expected("gzip-empty-payload-list") -> {error,badfile}.
