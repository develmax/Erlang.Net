-module(beam_tuple_result_append_probe).
-export([options/1, socket_options/2, real_port/2]).

-record(socket_options, {mode, packet, packet_size, header = 0, active}).

options(Options) ->
    {Native, Emulated} = collect(Options, [], default_options()),
    {Native ++ internal_options(), Emulated}.

collect(Native, _, Emulated) ->
    {Native, Emulated}.

default_options() ->
    beam_option_lists:defaults().

internal_options() ->
    beam_option_lists:internal().

socket_options(Values, #socket_options{mode = Mode, packet = Packet, packet_size = PacketSize, active = Active}) ->
    #socket_options{
        mode = proplists:get_value(mode, Values, Mode),
        packet = proplists:get_value(packet, Values, Packet),
        packet_size = proplists:get_value(packet_size, Values, PacketSize),
        active = active_option(Values, Active)
    }.

active_option(Values, Active) ->
    proplists:get_value(active, Values, Active).

real_port(Listener, Port) when is_pid(Listener) andalso is_integer(Port) ->
    case Port of
        0 ->
            {ok, {_, Resolved}} = beam_option_lists:sockname(Listener),
            Resolved;
        _ ->
            Port
    end.
