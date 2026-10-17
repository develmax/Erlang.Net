namespace Erlang.Differential;

internal static class MonitorOptionOrderCaseSources
{
    internal const string Order = """
        -module(monitor_option_order).
        -export([run/0,run/1,child/0]).
        run() -> run(node()).
        run(Node) ->
            lists:foreach(fun(CustomLast) ->
                Custom={monitor,[{tag,custom_monitor_order}]},
                Options=case CustomLast of true->[monitor,Custom]; false->[Custom,monitor] end,
                Expected=case CustomLast of true->custom_monitor_order; false->'DOWN' end,
                {Pid,Ref}=spawn_opt(Node,monitor_option_order,child,[],Options),
                Pid!release,
                receive {Expected,Ref,process,Pid,normal}->ok after 5000->error(wrong_spawn_tag) end,
                Request=spawn_request(Node,monitor_option_order,child,[],Options),
                receive {spawn_reply,Request,ok,Requested}->
                    Requested!release,
                    receive {Expected,Request,process,Requested,normal}->ok after 5000->error(wrong_request_tag) end
                after 5000->error(missing_request_reply) end
            end,[false,true]), done.
        child() -> receive release->ok end.
        """;
}
