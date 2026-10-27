-module(map_reentry505).
-export([load/1, run/0, build/3, observe/4]).

load(Path) -> erlang:load_nif(Path, []).

build(_, _, _) -> erlang:nif_error(not_loaded).

observe(_, _, _, _) -> erlang:nif_error(not_loaded).

body(Term) ->
    <<131, Encoded/binary>> = term_to_binary(Term, [{minor_version, 2}]),
    Encoded.

function(UniqueByte) ->
    Module = body(map_collision504_identity),
    OldIndex = body(7),
    OldUnique = body(321),
    Creator = body(self()),
    Environment = body(17),
    Unique = binary:copy(<<UniqueByte>>, 16),
    Payload = <<0, Unique/binary, 7:32, 1:32, Module/binary,
                OldIndex/binary, OldUnique/binary, Creator/binary, Environment/binary>>,
    binary_to_term(<<131, 112, (byte_size(Payload) + 4):32, Payload/binary>>).

run() ->
    F0 = function(17),
    F1 = function(29),
    F2 = function(43),
    Metadata = [{erlang:fun_info(F, module), erlang:fun_info(F, arity),
                 erlang:fun_info(F, index), erlang:fun_info(F, uniq),
                 erlang:fun_info(F, new_index), erlang:fun_info(F, new_uniq),
                 erlang:fun_info(F, env)} || F <- [F0, F1, F2]],
    % Two separate native invocations: build's environment has retired before
    % observe sees this materialized map. Opaque map/funs are never serialized.
    {Map, Role0, Role1, Role2} = build(F0, F1, F2),
    erlang:garbage_collect(),
    {Metadata, observe(Map, Role0, Role1, Role2)}.
