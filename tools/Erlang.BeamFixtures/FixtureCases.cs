namespace BeamFixtureExport;

internal static class FixtureCases
{
    public static IReadOnlyList<FixtureDefinition> All { get; } =
    [
        new(
            "beam_fixture_constant",
            """
            -module(beam_fixture_constant).
            -export([run/0]).
            run() -> {ok,42,[alpha,beta]}.
            """,
            "Constant tuple, list and atom result",
            "{ok,42,[alpha,beta]}"
        ),
        new(
            "beam_fixture_add",
            """
            -module(beam_fixture_add).
            -export([run/0,add/2]).
            run() -> add(17,25).
            add(A,B) when is_integer(A), is_integer(B) -> A+B.
            """,
            "Integer guards, arithmetic and an exported function",
            "42"
        ),
        new(
            "beam_fixture_local",
            """
            -module(beam_fixture_local).
            -export([run/0]).
            run() -> loop(3,[]).
            loop(0,Acc) -> {ok,Acc};
            loop(N,Acc) -> loop(N-1,[N|Acc]).
            """,
            "Private local recursion and tail call",
            "{ok,[1,2,3]}"
        ),
        new(
            "beam_fixture_receive",
            """
            -module(beam_fixture_receive).
            -export([run/0]).
            run() -> self() ! ping, receive ping -> {ok,ping} after 1000 -> timeout end.
            """,
            "Separate scope probe: send, receive and timeout instructions",
            "{ok,ping}"
        ),
        new(
            "beam_fixture_on_load",
            """
            -module(beam_fixture_on_load).
            -on_load(init/0).
            -export([run/0]).
            init() -> put(beam_fixture_marker,loaded), ok.
            run() -> {ok,on_load_fixture}.
            """,
            "Separate scope probe: actual on_load entry and process-dictionary effect",
            null
        )
    ];
}
