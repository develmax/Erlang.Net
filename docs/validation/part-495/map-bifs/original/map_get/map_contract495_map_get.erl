-module(map_contract495_map_get).
-export([run/0,run_stacks/0,probe/1]).
run()->ordsets_core_support:run(?MODULE,cases()).
run_stacks()->ordsets_core_support:profiles(?MODULE,cases()).
cases()->[case_0,case_1,case_2,case_3,case_4,case_5,case_6,case_7,case_8,case_9,case_10,case_11,case_12,case_13,case_14,case_15,case_16,case_17,case_18,case_19,case_20,case_21,case_22,case_23,case_24,case_25,case_26,case_27,case_28,case_29,case_30,case_31,case_32,case_33,case_34,case_35,case_36,case_37,case_38,case_39,case_40,case_41,case_42,case_43,case_44,case_45,case_46,case_47,case_48,case_49,case_50,case_51,case_52,case_53,case_54,case_55,case_56,case_57,case_58,case_59,case_60,case_61,case_62,case_63,case_64,case_65,case_66,case_67,case_68,case_69,case_70].
probe(case_0)->K=opaque(0),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_1)->K=opaque(-1),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_2)->K=opaque(1 bsl 128),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_3)->K=opaque(1.0),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_4)->K=opaque(-0.0),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_5)->K=opaque(key),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_6)->K=opaque([]),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_7)->K=opaque([1,2]),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_8)->K=opaque([1|tail]),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_9)->K=opaque({}),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_10)->K=opaque({a,1}),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_11)->K=opaque(#{1=>a}),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_12)->K=opaque(<<>>),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_13)->K=opaque(<<0,255>>),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_14)->K=opaque(<<1:1>>),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_15)->K=opaque(fun erlang:abs/1),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_16)->K=opaque(binary_to_term(<<131,88,119,14,"oracle@fixture",0,0,0,5,0,0,0,2,0,0,0,7>>)),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_17)->K=opaque(binary_to_term(<<131,89,119,14,"oracle@fixture",0,0,0,5,0,0,0,7>>)),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_18)->K=opaque(binary_to_term(<<131,114,0,3,119,14,"oracle@fixture",7,0,0,0,1,0,0,0,2,0,0,0,3>>)),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_19)->K=opaque(records:create(test,a,[{x,1}],#{is_exported=>false})),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_20)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(0)]);
probe(case_21)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(-1)]);
probe(case_22)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(1 bsl 128)]);
probe(case_23)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(-(1 bsl 128))]);
probe(case_24)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(0.0)]);
probe(case_25)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(-0.0)]);
probe(case_26)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(1.5)]);
probe(case_27)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(ok)]);
probe(case_28)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque([])]);
probe(case_29)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque([1,2])]);
probe(case_30)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque([1|tail])]);
probe(case_31)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque({})]);
probe(case_32)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque({a})]);
probe(case_33)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque({#{1=>a}})]);
probe(case_34)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(<<>>)]);
probe(case_35)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(<<0,255>>)]);
probe(case_36)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(<<1:1>>)]);
probe(case_37)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(fun erlang:abs/1)]);
probe(case_38)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(fun(X)->X end)]);
probe(case_39)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(list_to_pid("<0.88.0>"))]);
probe(case_40)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(binary_to_term(<<131,88,119,14,"oracle@fixture",0,0,0,5,0,0,0,2,0,0,0,7>>))]);
probe(case_41)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(binary_to_term(<<131,89,119,14,"oracle@fixture",0,0,0,5,0,0,0,7>>))]);
probe(case_42)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(binary_to_term(<<131,114,0,3,119,14,"oracle@fixture",7,0,0,0,1,0,0,0,2,0,0,0,3>>))]);
probe(case_43)->erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(records:create(test,a,[{x,1}],#{is_exported=>false}))]);
probe(case_44)->erlang:apply(erlang,opaque(map_get),[opaque(-999),opaque(#{})]);
probe(case_45)->erlang:apply(erlang,opaque(map_get),[opaque(-999),opaque(#{0=>0})]);
probe(case_46)->erlang:apply(erlang,opaque(map_get),[opaque(-999),opaque(#{0=>0,1=>2,2=>4,3=>6,4=>8,5=>10,6=>12,7=>14,8=>16,9=>18,10=>20,11=>22,12=>24,13=>26,14=>28,15=>30,16=>32,17=>34,18=>36,19=>38,20=>40,21=>42,22=>44,23=>46,24=>48,25=>50,26=>52,27=>54,28=>56,29=>58,30=>60})]);
probe(case_47)->erlang:apply(erlang,opaque(map_get),[opaque(-999),opaque(#{0=>0,1=>2,2=>4,3=>6,4=>8,5=>10,6=>12,7=>14,8=>16,9=>18,10=>20,11=>22,12=>24,13=>26,14=>28,15=>30,16=>32,17=>34,18=>36,19=>38,20=>40,21=>42,22=>44,23=>46,24=>48,25=>50,26=>52,27=>54,28=>56,29=>58,30=>60,31=>62})]);
probe(case_48)->erlang:apply(erlang,opaque(map_get),[opaque(-999),opaque(#{0=>0,1=>2,2=>4,3=>6,4=>8,5=>10,6=>12,7=>14,8=>16,9=>18,10=>20,11=>22,12=>24,13=>26,14=>28,15=>30,16=>32,17=>34,18=>36,19=>38,20=>40,21=>42,22=>44,23=>46,24=>48,25=>50,26=>52,27=>54,28=>56,29=>58,30=>60,31=>62,32=>64})]);
probe(case_49)->erlang:apply(erlang,opaque(map_get),[opaque(-999),opaque(#{0=>0,1=>2,2=>4,3=>6,4=>8,5=>10,6=>12,7=>14,8=>16,9=>18,10=>20,11=>22,12=>24,13=>26,14=>28,15=>30,16=>32,17=>34,18=>36,19=>38,20=>40,21=>42,22=>44,23=>46,24=>48,25=>50,26=>52,27=>54,28=>56,29=>58,30=>60,31=>62,32=>64,33=>66,34=>68,35=>70,36=>72,37=>74,38=>76,39=>78,40=>80,41=>82,42=>84,43=>86,44=>88,45=>90,46=>92,47=>94,48=>96,49=>98,50=>100,51=>102,52=>104,53=>106,54=>108,55=>110,56=>112,57=>114,58=>116,59=>118,60=>120,61=>122,62=>124,63=>126})]);
probe(case_50)->erlang:apply(erlang,opaque(map_get),[opaque(-999),opaque(#{0=>0,1=>2,2=>4,3=>6,4=>8,5=>10,6=>12,7=>14,8=>16,9=>18,10=>20,11=>22,12=>24,13=>26,14=>28,15=>30,16=>32,17=>34,18=>36,19=>38,20=>40,21=>42,22=>44,23=>46,24=>48,25=>50,26=>52,27=>54,28=>56,29=>58,30=>60,31=>62,32=>64,33=>66,34=>68,35=>70,36=>72,37=>74,38=>76,39=>78,40=>80,41=>82,42=>84,43=>86,44=>88,45=>90,46=>92,47=>94,48=>96,49=>98,50=>100,51=>102,52=>104,53=>106,54=>108,55=>110,56=>112,57=>114,58=>116,59=>118,60=>120,61=>122,62=>124,63=>126,64=>128,65=>130,66=>132,67=>134,68=>136,69=>138,70=>140,71=>142,72=>144,73=>146,74=>148,75=>150,76=>152,77=>154,78=>156,79=>158,80=>160,81=>162,82=>164,83=>166,84=>168,85=>170,86=>172,87=>174,88=>176,89=>178,90=>180,91=>182,92=>184,93=>186,94=>188,95=>190,96=>192,97=>194,98=>196,99=>198,100=>200,101=>202,102=>204,103=>206,104=>208,105=>210,106=>212,107=>214,108=>216,109=>218,110=>220,111=>222,112=>224,113=>226,114=>228,115=>230,116=>232,117=>234,118=>236,119=>238,120=>240,121=>242,122=>244,123=>246,124=>248,125=>250,126=>252,127=>254,128=>256,129=>258,130=>260,131=>262,132=>264,133=>266,134=>268,135=>270,136=>272,137=>274,138=>276,139=>278,140=>280,141=>282,142=>284,143=>286,144=>288,145=>290,146=>292,147=>294,148=>296,149=>298,150=>300,151=>302,152=>304,153=>306,154=>308,155=>310,156=>312,157=>314,158=>316,159=>318,160=>320,161=>322,162=>324,163=>326,164=>328,165=>330,166=>332,167=>334,168=>336,169=>338,170=>340,171=>342,172=>344,173=>346,174=>348,175=>350,176=>352,177=>354,178=>356,179=>358,180=>360,181=>362,182=>364,183=>366,184=>368,185=>370,186=>372,187=>374,188=>376,189=>378,190=>380,191=>382,192=>384,193=>386,194=>388,195=>390,196=>392,197=>394,198=>396,199=>398,200=>400,201=>402,202=>404,203=>406,204=>408,205=>410,206=>412,207=>414,208=>416,209=>418,210=>420,211=>422,212=>424,213=>426,214=>428,215=>430,216=>432,217=>434,218=>436,219=>438,220=>440,221=>442,222=>444,223=>446,224=>448,225=>450,226=>452,227=>454,228=>456,229=>458,230=>460,231=>462,232=>464,233=>466,234=>468,235=>470,236=>472,237=>474,238=>476,239=>478,240=>480,241=>482,242=>484,243=>486,244=>488,245=>490,246=>492,247=>494,248=>496,249=>498,250=>500,251=>502,252=>504,253=>506,254=>508,255=>510,256=>512})]);
probe(case_51)->erlang:apply(erlang,opaque(map_get),[opaque(1),opaque(#{1=>integer,1.0=>floating})]);
probe(case_52)->erlang:apply(erlang,opaque(map_get),[opaque(1),opaque(#{-0.0=>zero,0.0=>same_zero})]);
probe(case_53)->erlang:apply(erlang,opaque(map_get),[opaque(1),opaque(#{a=>1,a=>2})]);
probe(case_54)->K=lists:foldl(fun(_,A)->{A} end,base,lists:seq(1,1)),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_55)->K=lists:foldl(fun(_,A)->{A} end,base,lists:seq(1,64)),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_56)->K=lists:foldl(fun(_,A)->{A} end,base,lists:seq(1,128)),M=opaque(#{K=>found}),erlang:apply(erlang,opaque(map_get),[opaque(K),opaque(M)]);
probe(case_57)->A=records:create(test,a,[{x,1},{y,2}],#{is_exported=>false}),B=records:create(test,a,[{y,2},{x,1}],#{is_exported=>false}),M=opaque(#{A=>record_value}),erlang:apply(erlang,opaque(map_get),[opaque(B),opaque(M)]);
probe(case_58)->A=opaque(1),B=opaque(1.0),M=opaque(#{A=>found}),erlang:apply(erlang,opaque(map_get),[opaque(B),opaque(M)]);
probe(case_59)->A=opaque(0.0),B=opaque(-0.0),M=opaque(#{A=>found}),erlang:apply(erlang,opaque(map_get),[opaque(B),opaque(M)]);
probe(case_60)->A=opaque(-0.0),B=opaque(0.0),M=opaque(#{A=>found}),erlang:apply(erlang,opaque(map_get),[opaque(B),opaque(M)]);
probe(case_61)->A=opaque(<<1:1>>),B=opaque(<<1:2>>),M=opaque(#{A=>found}),erlang:apply(erlang,opaque(map_get),[opaque(B),opaque(M)]);
probe(case_62)->A=opaque(#{a=>1,b=>2}),B=opaque(#{b=>2,a=>1}),M=opaque(#{A=>found}),erlang:apply(erlang,opaque(map_get),[opaque(B),opaque(M)]);
probe(case_63)->A=opaque([1,{a,<<1:9>>}]),B=opaque([1,{a,<<1:9>>}]),M=opaque(#{A=>found}),erlang:apply(erlang,opaque(map_get),[opaque(B),opaque(M)]);
probe(case_64)->V=opaque({<<1:9>>,fun erlang:abs/1}),M=opaque(#{key=>V}),R=erlang:apply(erlang,opaque(map_get),[opaque(key),opaque(M)]),erts_debug:same(V,R);
probe(case_65)->guard_probe(opaque(key),opaque(#{key=>found}));
probe(case_66)->guard_probe(opaque(missing),opaque(#{key=>found}));
probe(case_67)->guard_probe(opaque(key),opaque(not_map));
probe(case_68)->guard_probe(opaque(key),opaque([]));
probe(case_69)->guard_probe(opaque(key),opaque(records:create(test,a,[],#{is_exported=>false})));
probe(case_70)->erlang:map_get(opaque(missing),opaque(#{1=>one})).
guard_probe(K,M) when erlang:map_get(K,M)=:=found->true;guard_probe(_,_)->false.
opaque(V)->put(map_contract495_input,V),erase(map_contract495_input).
