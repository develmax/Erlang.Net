-module(mutex_lifecycle_runner795).
-export([run/6]).
run(Case,Source,ObserverSource,NativeA,NativeB,Output) ->
    W=fun(Name,Value)->ok=file:write_file(filename:join(Output,Name),term_to_binary(Value,[{minor_version,2}])) end,
    try
        {ok,mutex_lifecycle_observer795,Observer}=compile:file(ObserverSource,[binary]),
        ok=file:write_file(filename:join(Output,"observer.beam"),Observer),
        {module,mutex_lifecycle_observer795}=code:load_binary(mutex_lifecycle_observer795,ObserverSource,Observer),
        {ok,mutex_lifecycle795,BeamA}=compile:file(Source,[binary,{d,'BEAM_VERSION795',1}]),
        {ok,mutex_lifecycle795,BeamB}=compile:file(Source,[binary,{d,'BEAM_VERSION795',2}]),
        ok=file:write_file(filename:join(Output,"beam-a.beam"),BeamA),
        ok=file:write_file(filename:join(Output,"beam-b.beam"),BeamB),
        W("metadata.etf",{code:root_dir(),erlang:system_info(version),erlang:system_info(system_architecture)}),
        Value=case Case of
            failed -> mutex_lifecycle_observer795:failed(BeamA,NativeA);
            upgrade -> mutex_lifecycle_observer795:upgrade(BeamA,BeamB,NativeA,NativeB)
        end,
        Whole={returned,Value},
        W("observed.etf",Whole),
        ok=file:write_file(filename:join(Output,"observed-deterministic.etf"),term_to_binary(Whole,[deterministic,{minor_version,2}])),
        W("cleanup.etf",erlang:module_loaded(mutex_lifecycle795)),
        false=erlang:module_loaded(mutex_lifecycle795),
        halt(0)
    catch Class:Reason:Stack -> W("entry-failure.etf",{Class,Reason,Stack}),halt(77) end.
