-module(beam_measured_record).
-export([run/1, bits/1]).
-record(packet, {payload}).
run(Packet) ->
    <<(byte_size(Packet#packet.payload)):16, (Packet#packet.payload)/binary>>.
bits(Packet) ->
    <<(bit_size(Packet#packet.payload)):8, (Packet#packet.payload)/bitstring>>.
