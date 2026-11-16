-module(beam_string_sigils).
-export([raw/0,escaped/0,list/0,triple/0,default/0,paired/0,escaped_triple/0]).
raw() -> ~B"\n".
escaped() -> ~b"\n".
list() -> ~s`"word"`.
triple() -> ~S"""
    alpha\n
    beta
    """.
default() -> ~'a"b'.
paired() -> ~b[one\]two].
escaped_triple() -> ~b"""
    escaped\ntext
    """.
