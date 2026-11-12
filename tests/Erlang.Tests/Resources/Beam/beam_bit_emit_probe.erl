-module(beam_bit_emit_probe).
-export([byte/1, size/2, mixed/2, utf/2, suffix/1, floats/1]).
byte(X)-><<X>>.
size(X,N)-><<X:N/little-signed-unit:2>>.
mixed(X,B)-><<17:5,X:12/little,B/bitstring>>.
utf(A,B)-><<A/utf8,B/utf16-little>>.
suffix(B)-><<B/binary,"x">>.
floats(X)-><<X:32/float-little>>.
