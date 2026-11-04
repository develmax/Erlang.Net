-module(tail_rejection595).
-export([run/0,capture/1]).
run() ->
    {ok,Bytes}=file:read_file("artifacts/ci119-tail-oracle/beam_opcode_edges373.beam"),
    {module,beam_opcode_edges373}=code:load_binary(beam_opcode_edges373,"synthetic.beam",Bytes),
    Parent=self(),
    spawn(fun() -> Parent ! {result,[{badfun,capture(0)},{badarity,capture(fun(_)->ok end)}]} end),
    receive {result,Result} ->
        ok=file:write_file("artifacts/ci119-tail-oracle/worker-original.etf",term_to_binary(Result)),
        ok=file:write_file("artifacts/ci119-tail-oracle/worker-original.txt",io_lib:format("~p.~n",[Result])),
        io:format("~p~n",[Result])
    after 10000 -> error(timeout)
    end.
capture(Function) ->
    try beam_opcode_edges373:run(Function) of Value -> {returned,Value}
    catch Class:Reason:Stack -> {Class,Reason,Stack}
    end.
