-module(atom_contract492_binary_to_existing_atom2_p1).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [c80,c81,c82,c83,c84,c85,c86,c87,c88,c89,c90,c91,c92,c93,c94,c95,c96,c97,c98,c99,c100,c101,c102,c103,c104,c105,c106,c107,c108,c109,c110,c111,c112,c113,c114,c115,c116,c117,c118,c119,c120,c121,c122,c123,c124,c125,c126,c127].
probe(c80) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[binary:copy(<<255>>,1021),latin1]);
probe(c81) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[<<97,255>>,utf8]);
probe(c82) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[<<97,255>>,unicode]);
probe(c83) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<97,255>>,latin1]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<97,255>>,latin1]);
probe(c84) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[<<255,97>>,utf8]);
probe(c85) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[<<255,97>>,unicode]);
probe(c86) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<255,97>>,latin1]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<255,97>>,latin1]);
probe(c87) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[a,utf8]);
probe(c88) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[a,unicode]);
probe(c89) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[a,latin1]);
probe(c90) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[0,utf8]);
probe(c91) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[0,unicode]);
probe(c92) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[0,latin1]);
probe(c93) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[1.0,utf8]);
probe(c94) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[1.0,unicode]);
probe(c95) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[1.0,latin1]);
probe(c96) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[[],utf8]);
probe(c97) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[[],unicode]);
probe(c98) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[[],latin1]);
probe(c99) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[[97],utf8]);
probe(c100) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[[97],unicode]);
probe(c101) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[[97],latin1]);
probe(c102) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[{},utf8]);
probe(c103) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[{},unicode]);
probe(c104) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[{},latin1]);
probe(c105) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[#{},utf8]);
probe(c106) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[#{},unicode]);
probe(c107) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[#{},latin1]);
probe(c108) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<97>>,a]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<97>>,a]);
probe(c109) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<97>>,0]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<97>>,0]);
probe(c110) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<97>>,1.0]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<97>>,1.0]);
probe(c111) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<97>>,[]]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<97>>,[]]);
probe(c112) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<97>>,[utf8]]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<97>>,[utf8]]);
probe(c113) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<97>>,{}]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<97>>,{}]);
probe(c114) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<97>>,<<>>]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<97>>,<<>>]);
probe(c115) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<>>,utf8]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<>>,utf8]);
probe(c116) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<>>,unicode]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<>>,unicode]);
probe(c117) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<>>,latin1]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<>>,latin1]);
probe(c118) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<0>>,utf8]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<0>>,utf8]);
probe(c119) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<0>>,unicode]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<0>>,unicode]);
probe(c120) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<0>>,latin1]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<0>>,latin1]);
probe(c121) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<195,191>>,utf8]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<195,191>>,utf8]);
probe(c122) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<195,191>>,unicode]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<195,191>>,unicode]);
probe(c123) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<195,191>>,latin1]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<195,191>>,latin1]);
probe(c124) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<240,159,152,128>>,utf8]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<240,159,152,128>>,utf8]);
probe(c125) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<240,159,152,128>>,unicode]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<240,159,152,128>>,unicode]);
probe(c126) -> _=erlang:apply(erlang,opaque(binary_to_atom),[<<240,159,152,128>>,latin1]), erlang:apply(erlang,opaque(binary_to_existing_atom),[<<240,159,152,128>>,latin1]);
probe(c127) -> erlang:apply(erlang,opaque(binary_to_existing_atom),[<<"atom_contract492_never_created_89348">>,utf8]).
opaque(Value) -> put(atom_contract492_input,Value),erase(atom_contract492_input).
