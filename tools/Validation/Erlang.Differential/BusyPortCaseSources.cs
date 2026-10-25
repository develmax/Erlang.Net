namespace Erlang.Differential;

internal static class BusyPortCaseSources
{
    internal const string Case0 = "-module(busy_port_contract). -export([run/0]). run()->S=self(),undefined=erlang:system_monitor(S,[busy_port]),{S,[busy_port]}=erlang:system_monitor(),{S,[busy_port]}=erlang:system_monitor(undefined),done.\n";
    internal const string Case1 = "-module(busy_port_contract). -export([run/0]). run()->S=self(),undefined=erlang:system_monitor({S,[{busy_port,true}]}),{S,[busy_port]}=erlang:system_monitor(),erlang:system_monitor(undefined),done.\n";
    internal const string Case2 = "-module(busy_port_contract). -export([run/0]). run()->S=self(),undefined=erlang:system_monitor(S,[busy_port,{busy_port,false}]),{S,[]}=erlang:system_monitor(),erlang:system_monitor(undefined),done.\n";
    internal const string Case3 = "-module(busy_port_contract). -export([run/0]). run()->S=self(),undefined=erlang:system_monitor(S,[{busy_port,false},busy_port]),{S,[busy_port]}=erlang:system_monitor(),erlang:system_monitor(undefined),done.\n";
    internal const string Case4 = "-module(busy_port_contract). -export([run/0]). run()->S=self(),undefined=erlang:system_monitor(S,[busy_port]),{'EXIT',{badarg,_}}=catch erlang:system_monitor(S,[{busy_port,false},{busy_port,wrong}]),{S,[busy_port]}=erlang:system_monitor(),erlang:system_monitor(undefined),done.\n";
    internal const string Case5 = "-module(busy_port_contract). -export([run/0]). run()->S=self(),undefined=erlang:system_monitor(S,[{long_message_queue,{0,3}},busy_port]),{S,[busy_port,{long_message_queue,{0,3}}]}=erlang:system_monitor(),erlang:system_monitor(undefined),done.\n";
    internal const string Case6 = "-module(busy_port_contract). -export([run/0]). run()->S=self(),P=spawn(fun()->receive stop->ok end end),undefined=erlang:system_monitor(S,[busy_port]),{S,[busy_port]}=erlang:system_monitor({P,[busy_port]}),{P,[busy_port]}=erlang:system_monitor(undefined),undefined=erlang:system_monitor(),P!stop,done.\n";
    internal const string Case7 = "-module(busy_port_contract). -export([run/0]). run()->{P,R}=spawn_monitor(fun()->receive stop->ok end end),undefined=erlang:system_monitor(P,[busy_port]),P!stop,receive {'DOWN',R,process,P,normal}->undefined=erlang:system_monitor() after 5000->error(no_down) end,done.\n";
}
