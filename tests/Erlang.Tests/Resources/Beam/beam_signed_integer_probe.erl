-module(beam_signed_integer_probe).
-export([integer_bytes/1, signed_width/2, signed_integer/1]).
integer_bytes(Value) when is_integer(Value) ->
    Width = signed_width(Value, 8),
    <<Value:Width/signed>>.

signed_width(Value, Width) ->
    Upper = 1 bsl (Width - 1),
    case Value >= -Upper andalso Value < Upper of
        true -> Width;
        false -> signed_width(Value, Width + 8)
    end.

signed_integer(Bytes) ->
    Width = bit_size(Bytes),
    <<Value:Width/signed>> = Bytes,
    Value.

