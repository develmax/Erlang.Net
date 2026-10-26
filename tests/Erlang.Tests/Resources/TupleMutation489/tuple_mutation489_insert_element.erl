-module(tuple_mutation489_insert_element).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [insert_element_80,
            insert_element_81,
            insert_element_82,
            insert_element_83,
            insert_element_84,
            insert_element_85,
            insert_element_86,
            insert_element_87,
            insert_element_88,
            insert_element_89,
            insert_element_90,
            insert_element_91,
            insert_element_92,
            insert_element_93,
            insert_element_94,
            insert_element_95,
            insert_element_96,
            insert_element_97,
            insert_element_98,
            insert_element_99,
            insert_element_100,
            insert_element_101,
            insert_element_102,
            insert_element_103,
            insert_element_104,
            insert_element_105,
            insert_element_106,
            insert_element_107,
            insert_element_108,
            insert_element_109,
            insert_element_110,
            insert_element_111,
            insert_element_112,
            insert_element_113,
            insert_element_114,
            insert_element_115,
            insert_element_116,
            insert_element_117,
            insert_element_118,
            insert_element_119,
            insert_element_120,
            insert_element_121,
            insert_element_122,
            insert_element_123,
            insert_element_124,
            insert_element_125,
            insert_element_126,
            insert_element_127,
            insert_element_128,
            insert_element_129,
            insert_element_130,
            insert_element_131,
            insert_element_132,
            insert_element_133,
            insert_element_134,
            insert_element_135,
            insert_element_136,
            insert_element_137,
            insert_element_138,
            insert_element_139,
            insert_element_140,
            insert_element_141,
            insert_element_142,
            insert_element_143,
            insert_element_144,
            insert_element_145,
            insert_element_146,
            insert_element_147,
            insert_element_148,
            insert_element_149,
            insert_element_150,
            insert_element_151,
            insert_element_152,
            insert_element_153,
            insert_element_154,
            insert_element_155,
            insert_element_156,
            insert_element_157,
            insert_element_158,
            insert_element_159].
probe(insert_element_80) -> erlang:apply(erlang,opaque(insert_element),[-18446744073709551616,{},{new,<<1:9>>}]);
probe(insert_element_81) -> erlang:apply(erlang,opaque(insert_element),[-18446744073709551616,{a},{new,<<1:9>>}]);
probe(insert_element_82) -> erlang:apply(erlang,opaque(insert_element),[-18446744073709551616,{a,b},{new,<<1:9>>}]);
probe(insert_element_83) -> erlang:apply(erlang,opaque(insert_element),[-18446744073709551616,{a,b,c},{new,<<1:9>>}]);
probe(insert_element_84) -> erlang:apply(erlang,opaque(insert_element),[-18446744073709551616,a,{new,<<1:9>>}]);
probe(insert_element_85) -> erlang:apply(erlang,opaque(insert_element),[-18446744073709551616,[],{new,<<1:9>>}]);
probe(insert_element_86) -> erlang:apply(erlang,opaque(insert_element),[-18446744073709551616,#{},{new,<<1:9>>}]);
probe(insert_element_87) -> erlang:apply(erlang,opaque(insert_element),[-18446744073709551616,<<>>,{new,<<1:9>>}]);
probe(insert_element_88) -> erlang:apply(erlang,opaque(insert_element),[-1,{},{new,<<1:9>>}]);
probe(insert_element_89) -> erlang:apply(erlang,opaque(insert_element),[-1,{a},{new,<<1:9>>}]);
probe(insert_element_90) -> erlang:apply(erlang,opaque(insert_element),[-1,{a,b},{new,<<1:9>>}]);
probe(insert_element_91) -> erlang:apply(erlang,opaque(insert_element),[-1,{a,b,c},{new,<<1:9>>}]);
probe(insert_element_92) -> erlang:apply(erlang,opaque(insert_element),[-1,a,{new,<<1:9>>}]);
probe(insert_element_93) -> erlang:apply(erlang,opaque(insert_element),[-1,[],{new,<<1:9>>}]);
probe(insert_element_94) -> erlang:apply(erlang,opaque(insert_element),[-1,#{},{new,<<1:9>>}]);
probe(insert_element_95) -> erlang:apply(erlang,opaque(insert_element),[-1,<<>>,{new,<<1:9>>}]);
probe(insert_element_96) -> erlang:apply(erlang,opaque(insert_element),[0,{},{new,<<1:9>>}]);
probe(insert_element_97) -> erlang:apply(erlang,opaque(insert_element),[0,{a},{new,<<1:9>>}]);
probe(insert_element_98) -> erlang:apply(erlang,opaque(insert_element),[0,{a,b},{new,<<1:9>>}]);
probe(insert_element_99) -> erlang:apply(erlang,opaque(insert_element),[0,{a,b,c},{new,<<1:9>>}]);
probe(insert_element_100) -> erlang:apply(erlang,opaque(insert_element),[0,a,{new,<<1:9>>}]);
probe(insert_element_101) -> erlang:apply(erlang,opaque(insert_element),[0,[],{new,<<1:9>>}]);
probe(insert_element_102) -> erlang:apply(erlang,opaque(insert_element),[0,#{},{new,<<1:9>>}]);
probe(insert_element_103) -> erlang:apply(erlang,opaque(insert_element),[0,<<>>,{new,<<1:9>>}]);
probe(insert_element_104) -> erlang:apply(erlang,opaque(insert_element),[1,{},{new,<<1:9>>}]);
probe(insert_element_105) -> erlang:apply(erlang,opaque(insert_element),[1,{a},{new,<<1:9>>}]);
probe(insert_element_106) -> erlang:apply(erlang,opaque(insert_element),[1,{a,b},{new,<<1:9>>}]);
probe(insert_element_107) -> erlang:apply(erlang,opaque(insert_element),[1,{a,b,c},{new,<<1:9>>}]);
probe(insert_element_108) -> erlang:apply(erlang,opaque(insert_element),[1,a,{new,<<1:9>>}]);
probe(insert_element_109) -> erlang:apply(erlang,opaque(insert_element),[1,[],{new,<<1:9>>}]);
probe(insert_element_110) -> erlang:apply(erlang,opaque(insert_element),[1,#{},{new,<<1:9>>}]);
probe(insert_element_111) -> erlang:apply(erlang,opaque(insert_element),[1,<<>>,{new,<<1:9>>}]);
probe(insert_element_112) -> erlang:apply(erlang,opaque(insert_element),[2,{},{new,<<1:9>>}]);
probe(insert_element_113) -> erlang:apply(erlang,opaque(insert_element),[2,{a},{new,<<1:9>>}]);
probe(insert_element_114) -> erlang:apply(erlang,opaque(insert_element),[2,{a,b},{new,<<1:9>>}]);
probe(insert_element_115) -> erlang:apply(erlang,opaque(insert_element),[2,{a,b,c},{new,<<1:9>>}]);
probe(insert_element_116) -> erlang:apply(erlang,opaque(insert_element),[2,a,{new,<<1:9>>}]);
probe(insert_element_117) -> erlang:apply(erlang,opaque(insert_element),[2,[],{new,<<1:9>>}]);
probe(insert_element_118) -> erlang:apply(erlang,opaque(insert_element),[2,#{},{new,<<1:9>>}]);
probe(insert_element_119) -> erlang:apply(erlang,opaque(insert_element),[2,<<>>,{new,<<1:9>>}]);
probe(insert_element_120) -> erlang:apply(erlang,opaque(insert_element),[3,{},{new,<<1:9>>}]);
probe(insert_element_121) -> erlang:apply(erlang,opaque(insert_element),[3,{a},{new,<<1:9>>}]);
probe(insert_element_122) -> erlang:apply(erlang,opaque(insert_element),[3,{a,b},{new,<<1:9>>}]);
probe(insert_element_123) -> erlang:apply(erlang,opaque(insert_element),[3,{a,b,c},{new,<<1:9>>}]);
probe(insert_element_124) -> erlang:apply(erlang,opaque(insert_element),[3,a,{new,<<1:9>>}]);
probe(insert_element_125) -> erlang:apply(erlang,opaque(insert_element),[3,[],{new,<<1:9>>}]);
probe(insert_element_126) -> erlang:apply(erlang,opaque(insert_element),[3,#{},{new,<<1:9>>}]);
probe(insert_element_127) -> erlang:apply(erlang,opaque(insert_element),[3,<<>>,{new,<<1:9>>}]);
probe(insert_element_128) -> erlang:apply(erlang,opaque(insert_element),[4,{},{new,<<1:9>>}]);
probe(insert_element_129) -> erlang:apply(erlang,opaque(insert_element),[4,{a},{new,<<1:9>>}]);
probe(insert_element_130) -> erlang:apply(erlang,opaque(insert_element),[4,{a,b},{new,<<1:9>>}]);
probe(insert_element_131) -> erlang:apply(erlang,opaque(insert_element),[4,{a,b,c},{new,<<1:9>>}]);
probe(insert_element_132) -> erlang:apply(erlang,opaque(insert_element),[4,a,{new,<<1:9>>}]);
probe(insert_element_133) -> erlang:apply(erlang,opaque(insert_element),[4,[],{new,<<1:9>>}]);
probe(insert_element_134) -> erlang:apply(erlang,opaque(insert_element),[4,#{},{new,<<1:9>>}]);
probe(insert_element_135) -> erlang:apply(erlang,opaque(insert_element),[4,<<>>,{new,<<1:9>>}]);
probe(insert_element_136) -> erlang:apply(erlang,opaque(insert_element),[18446744073709551616,{},{new,<<1:9>>}]);
probe(insert_element_137) -> erlang:apply(erlang,opaque(insert_element),[18446744073709551616,{a},{new,<<1:9>>}]);
probe(insert_element_138) -> erlang:apply(erlang,opaque(insert_element),[18446744073709551616,{a,b},{new,<<1:9>>}]);
probe(insert_element_139) -> erlang:apply(erlang,opaque(insert_element),[18446744073709551616,{a,b,c},{new,<<1:9>>}]);
probe(insert_element_140) -> erlang:apply(erlang,opaque(insert_element),[18446744073709551616,a,{new,<<1:9>>}]);
probe(insert_element_141) -> erlang:apply(erlang,opaque(insert_element),[18446744073709551616,[],{new,<<1:9>>}]);
probe(insert_element_142) -> erlang:apply(erlang,opaque(insert_element),[18446744073709551616,#{},{new,<<1:9>>}]);
probe(insert_element_143) -> erlang:apply(erlang,opaque(insert_element),[18446744073709551616,<<>>,{new,<<1:9>>}]);
probe(insert_element_144) -> erlang:apply(erlang,opaque(insert_element),[a,{},{new,<<1:9>>}]);
probe(insert_element_145) -> erlang:apply(erlang,opaque(insert_element),[a,{a},{new,<<1:9>>}]);
probe(insert_element_146) -> erlang:apply(erlang,opaque(insert_element),[a,{a,b},{new,<<1:9>>}]);
probe(insert_element_147) -> erlang:apply(erlang,opaque(insert_element),[a,{a,b,c},{new,<<1:9>>}]);
probe(insert_element_148) -> erlang:apply(erlang,opaque(insert_element),[a,a,{new,<<1:9>>}]);
probe(insert_element_149) -> erlang:apply(erlang,opaque(insert_element),[a,[],{new,<<1:9>>}]);
probe(insert_element_150) -> erlang:apply(erlang,opaque(insert_element),[a,#{},{new,<<1:9>>}]);
probe(insert_element_151) -> erlang:apply(erlang,opaque(insert_element),[a,<<>>,{new,<<1:9>>}]);
probe(insert_element_152) -> erlang:apply(erlang,opaque(insert_element),[1.0,{},{new,<<1:9>>}]);
probe(insert_element_153) -> erlang:apply(erlang,opaque(insert_element),[1.0,{a},{new,<<1:9>>}]);
probe(insert_element_154) -> erlang:apply(erlang,opaque(insert_element),[1.0,{a,b},{new,<<1:9>>}]);
probe(insert_element_155) -> erlang:apply(erlang,opaque(insert_element),[1.0,{a,b,c},{new,<<1:9>>}]);
probe(insert_element_156) -> erlang:apply(erlang,opaque(insert_element),[1.0,a,{new,<<1:9>>}]);
probe(insert_element_157) -> erlang:apply(erlang,opaque(insert_element),[1.0,[],{new,<<1:9>>}]);
probe(insert_element_158) -> erlang:apply(erlang,opaque(insert_element),[1.0,#{},{new,<<1:9>>}]);
probe(insert_element_159) -> erlang:apply(erlang,opaque(insert_element),[1.0,<<>>,{new,<<1:9>>}]).
opaque(Value) -> put(tuple_mutation489_input,Value),erase(tuple_mutation489_input).
