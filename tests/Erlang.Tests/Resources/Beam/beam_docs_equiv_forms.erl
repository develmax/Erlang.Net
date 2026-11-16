-module(beam_docs_equiv_forms).
-export([a/0,b/0,c/0,d/0,e/0,f/0,g/0,h/0,i/0]).
-doc(#{equiv => update(HandlerId, #{Key => Value})}).
a() -> ok.
-doc(#{equiv => enable("/etc", "/etc/syslog.conf")}).
b() -> ok.
-doc(#{equiv => print(Term, 1, 80, -1)}).
c() -> ok.
-doc(#{equiv => init(Dir, MaxBytes, MaxFiles, fun(_) -> true end)}).
d() -> ok.
-doc(#{equiv => apply(fun() -> erlang:apply(Module, Function, Args) end)}).
e() -> ok.
-doc(#{equiv => register_name(Name, Pid, fun random_exit_name/3)}).
f() -> ok.
-doc(#{equiv => substr(String, Start, string:length(String) - (Start - 1))}).
g() -> ok.
-doc(#{equiv => pad(String, Length, trailing, $\s)}).
h() -> ok.
-doc(#{equiv => setScrollbars(This, PixelsPerUnitX, PixelsPerUnitY, NoUnitsX, NoUnitsY, [])}).
i() -> ok.
