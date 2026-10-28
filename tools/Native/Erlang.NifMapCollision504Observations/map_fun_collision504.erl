-module(map_fun_collision504).
-export([load/1, run/0, run/3]).

load(Path) -> erlang:load_nif(Path, []).

run(_, _, _) -> erlang:nif_error(not_loaded).

body(Term) ->
    <<131, Encoded/binary>> = term_to_binary(Term, [{minor_version, 2}]),
    Encoded.

% No generated function is invoked. Module/index/old_uniq/environment are fixed
% across both VMs; only NEW_FUN_EXT's 16-byte Unique field changes. The creator
% PID is real, but is neither part of fun equality/hash nor returned by run/0.
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
    {Metadata, run(F0, F1, F2)}.
