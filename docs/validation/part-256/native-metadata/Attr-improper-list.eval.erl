try
    {ok,Input}=file:read_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/beam_fixture_constant.beam"),
    <<"FOR1",_:32/big,"BEAM",Body/binary>>=Input,
    Parse=fun Parse(<<>>,Acc)->lists:reverse(Acc);
        Parse(<<Id:4/binary,Size:32/big,Data:Size/binary,Rest/binary>>,Acc)->
            Pad=(4-(Size rem 4)) rem 4,
            <<_:Pad/binary,Tail/binary>>=Rest,
            Parse(Tail,[{Id,Data}|Acc])
    end,
    Chunks=Parse(Body,[]),
    Chunk = <<"Attr">>,
    Payload = term_to_binary([73|custom]),
    Fresh=case false of
        true->[C || C={Id,_}<-Chunks,Id=/=Chunk];
        false->[case Id=:=Chunk of true->{Id,Payload};false->{Id,Data} end || {Id,Data}<-Chunks]
    end,
    true=lists:keymember(Chunk,1,Chunks),
    Encoded=iolist_to_binary([begin N=byte_size(Data),Pad=(4-(N rem 4)) rem 4,<<Id/binary,N:32/big,Data/binary,0:(Pad*8)>> end || {Id,Data}<-Fresh]),
    Size=byte_size(Encoded)+4,
    Image = <<"FOR1",Size:32/big,"BEAM",Encoded/binary>>,
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/metadata-mutations/Attr-improper-list.beam",Image),
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/metadata-mutations/Attr-improper-list.payload.bin",Payload),
    {ok,{beam_fixture_constant,OriginalMd5}}=beam_lib:md5(Input),
    Load=code:load_binary(beam_fixture_constant,"/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/metadata-mutations/Attr-improper-list.beam",Image),
    {Result,Details}=case Load of
        {module,beam_fixture_constant}->
            Value=erlang:get_module_info(beam_fixture_constant,attributes),
            BodyResult=apply(beam_fixture_constant,run,[]),
            Md5=erlang:get_module_info(beam_fixture_constant,md5),
            {{accepted,Value,BodyResult,Md5=:=OriginalMd5},{Load,erlang:get_module_info(beam_fixture_constant,attributes),erlang:get_module_info(beam_fixture_constant,compile),erlang:get_module_info(beam_fixture_constant,exports),erlang:get_module_info(beam_fixture_constant,functions)}};
        Error->{Error,{Load}}
    end,
    Wanted={accepted,[73|custom],{ok,42,[alpha,beta]},true},
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/metadata-mutations/Attr-improper-list.actual.etf",term_to_binary(Result)),
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/metadata-mutations/Attr-improper-list.expected.etf",term_to_binary(Wanted)),
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/metadata-mutations/Attr-improper-list.actual.term",unicode:characters_to_binary(io_lib:format("~tp.~n",[{Result,Details}]))),
    case Result=:=Wanted of true->halt(0);false->halt(6) end
catch Class:Reason:Stack->io:format(standard_error,"~tp~n",[{Class,Reason,Stack}]),halt(4) end.
