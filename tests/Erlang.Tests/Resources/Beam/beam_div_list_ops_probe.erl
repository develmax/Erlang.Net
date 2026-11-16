-module(beam_div_list_ops_probe).
-export([int/2, float/2, lists/2, guard/2, shift/2, remainder/2]).
int(A,B)-> A div B.
float(A,B)-> A / B.
lists(A,B)-> A -- B.
guard(A,B) when A div B > 0 -> yes;
guard(_,_) -> no.
shift(A,B)-> A bsl B.
remainder(A,B)-> A rem B.
