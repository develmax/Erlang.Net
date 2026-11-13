-module(beam_register_send).
-export([reply/2, with_receiver/2, invalid_destination/2]).
reply({Ref, To}, Response) ->
    To ! {{Ref, reply}, Response},
    ok.
with_receiver({Ref, To}, Response) ->
    To ! {{Ref, To}, Response},
    ok.
invalid_destination({Ref, To}, Response) ->
    To ! {Ref, Response},
    ok.
