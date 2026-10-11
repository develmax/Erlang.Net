using Erlang;

namespace Erlang.Differential;

public static class ErlangHashCases
{
    public static string? FixtureName(string function, int arity) => (function, arity) switch
    {
        ("phash2", 1) => "erlang-hash-default-atom",
        ("phash2", 2) => "erlang-hash-vector-nil",
        ("phash", 2) => "erlang-hash-legacy-tuple",
        _ => null
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body, Term expected) => cases.Add(new("erlang-hash-" + name, "-module(hash_contract). -export([run/0]). run()->" + body + ".", Term.Tuple(Term.A("ok"), expected)));

        void Check(string name, string body) => Add(name, body, Term.A("true"));
        var vectors = new (string Name, string Source, uint Expected)[]
        {
            ("nil","[]",3468870702),("atom","abc",26499),("atom-next","abd",26500),
            ("latin1","'åäö'",62518),("unicode","'😃'",1026307),
            ("zero","0",3175731469),("one","1",539485162),("negative","-1",1117813597),
            ("small-positive","1 bsl 20",1477815345),("small-negative","-(1 bsl 20)",3076904293),
            ("word-positive","4294967296",2108323275),("word-negative","-4294967296",2586067094),
            ("large-positive","981494972710656",1622082818),("large-negative","-981494972710656",3367191372),
            ("big-positive","1 bsl 65",2545846594),("big-negative","-(1 bsl 65)",1649047068),
            ("huge-positive","1 bsl 200",2573322433),("huge-negative","-(1 bsl 200)",2288753377),
            ("empty-binary","<<>>",147926629),("zero-byte","<<0>>",2914887855),("zero-word","<<0:32>>",2014511533),
            ("binary","<<\"abc\">>",1306188027),("binary-blocks","<<\"12345678901234567890\">>",3021061640),
            ("zero-bits","<<0:7>>",1055790816),("ones-bits","<<127:7>>",911751529),
            ("bits-tail","<<\"abc\",13:4>>",670412287),
            ("float-zero","0.0",423528920),("float-negative-zero","-0.0",423528920),
            ("float","3.14",3731709215),("float-negative","-3.14",1827518724),
            ("list-float","[0.0]",167906877),("list-tuple","[{}]",4050867804),
            ("list-binary","[<<>>]",440873397),("list-nil","[[]]",499070068),("list-atom","[abc]",3112446404),
            ("list-atoms","[a,b,c]",1505666924),("improper","[a,b|c]",433753489),
            ("string","\"abc\"",519996486),("string-tail","\"abc\"++[1009]",290369864),
            ("string-suffix","\"abc\"++[1009]++\"de\"",4134369195),("string-blocks","\"1234567890123456\"",963649519),
            ("tuple-empty","{}",221703996),("tuple-nil","{[]}",1775079505),
            ("tuple-nested","{{}}",2165044361),("tuple-binary","{<<>>}",682464809),
            ("tuple-float","{0.0}",688441152),("tuple-atom","{abc}",2032039329),
            ("tuple-mixed","{a,1,{},-3.14}",1364396939),
            ("tuple-binary-a","{a,<<>>}",2724468891),("tuple-binary-b","{b,<<>>}",2702508511),
            ("rune-atom","erlang:list_to_atom(lists:seq(5792,5872))",241561024),
            ("kana-atom","erlang:list_to_atom(lists:seq(12441,12474))",246053818),
            ("large-binary","erlang:list_to_binary(lists:flatten(lists:duplicate(500,lists:seq(32,127))))",2644086993),
            ("binary-slice","begin B = erlang:list_to_binary(lists:flatten(lists:duplicate(500,lists:seq(32,127)))),<<_:128,Sub/binary>> = B,Sub end",3575839236),
            ("bits-prefix","<<5:3,\"12345678901234567890\">>",289973273),
            ("pid-wire","erlang:binary_to_term(<<131,103,100,0,11,97,112,97,64,108,101,103,111,108,97,115,0,0,0,36,0,0,0,0,1>>)",1290188489),
            ("port-wire","erlang:binary_to_term(<<131,102,100,0,11,97,112,97,64,108,101,103,111,108,97,115,0,0,0,24,3>>)",274735),
            ("reference-wire","erlang:binary_to_term(<<131,114,0,3,100,0,11,97,112,97,64,108,101,103,111,108,97,115,2,0,0,0,155,0,0,0,0,0,0,0,0>>)",3845846926),
            ("external-map-two","erlang:make_fun(lists,map,2)",840287883),
            ("external-map-three","erlang:make_fun(lists,map,3)",2318478565),
            ("external-filter-two","erlang:make_fun(lists,filter,2)",635165125),
            ("external-filter-three","erlang:make_fun(lists,filter,3)",3824649396),
            ("external-stub-two","erlang:make_fun(xxx,map,2)",2630071865),
            ("external-stub-three","erlang:make_fun(xxx,map,3)",4237970519)
        };
        foreach (var vector in vectors)
        {
            Add("vector-" + vector.Name, "erlang:phash2(" + vector.Source + ",4294967296)", new Integer(vector.Expected));
            Add("default-" + vector.Name, "erlang:phash2(" + vector.Source + ")", new Integer(vector.Expected & ((1U << 27) - 1)));
            Add("range-" + vector.Name, "erlang:phash2(" + vector.Source + ",97)", new Integer(vector.Expected % 97));
        }
        Add("legacy-tuple", "erlang:phash({a,b,c},4294967295)", Term.I(685556714));
        Add(
            "legacy-reference",
            "erlang:phash(erlang:binary_to_term(<<131,114,0,3,100,0,13,110,111,110,111,100,101,64,110,111,104,111,115,116,0,0,0,0,122,0,0,0,0,0,0,0,0>>),4294967295)",
            Term.I(1113403635)
        );
        foreach (string function in new[] { "phash", "phash2" })
            foreach (string range in new[] { "0", "-1", "1.0", "hello", "[]", "{1}", "4294967297", "1 bsl 100" })
                Add(
                    "bad-range-" + function + "-" + cases.Count,
                    "try erlang:" + function + "(#{a=>1}," + range + ") of _->unexpected catch error:Reason->Reason end",
                    Term.A("badarg")
                );
        foreach (string source in new[] { "[]", "abc", "-123456789012345678901234567890", "-0.0", "<<5:3>>", "{a,[1|tail]}", "#{1=>one,1.0=>float}" })
            Check(
                "range-laws-" + cases.Count,
                "V = " + source + ",H=erlang:phash2(V,4294967296),L=erlang:phash(V,4294967296),erlang:phash2(V) =:= (H band 134217727) andalso erlang:phash2(V,1) =:= 0 andalso erlang:phash(V,1) =:= 1 andalso erlang:phash2(V,17) =:= (H rem 17) andalso erlang:phash(V,17) =:= (((L-1) rem 17)+1) andalso H >= 0 andalso H < 4294967296 andalso L > 0 andalso L =< 4294967296"
            );
        Check(
            "map-order-independent",
            "A=maps:from_list([{1,one},{1.0,float},{a,#{b=>[1,2]}},{<<5:3>>,bits}]),B=maps:from_list(lists:reverse(maps:to_list(A))),erlang:phash2(A,4294967296) =:= erlang:phash2(B,4294967296) andalso erlang:phash(A,4294967296) =:= erlang:phash(B,4294967296)"
        );

        Add(
            "wire-local-1",
            "erlang:phash2(erlang:binary_to_term(<<131,112,0,0,0,66,0,215,206,77,69,249,50,170,17,129,47,21,98,13,196,76,242,0,0,0,1,0,0,0,0,100,0,1,116,97,1,98,2,195,126,58,103,100,0,13,110,111,110,111,100,101,64,110,111,104,111,115,116,0,0,0,112,0,0,0,0,0>>),4294967296)",
            new Integer(3826013332U)
        );
        Add(
            "wire-local-2",
            "erlang:phash2(erlang:binary_to_term(<<131,112,0,0,0,66,0,215,206,77,69,249,50,170,17,129,47,21,98,13,196,76,242,0,0,0,2,0,0,0,0,100,0,1,116,97,2,98,3,130,152,185,103,100,0,13,110,111,110,111,100,101,64,110,111,104,111,115,116,0,0,0,112,0,0,0,0,0>>),4294967296)",
            new Integer(126009152U)
        );
        Add(
            "wire-local-3",
            "erlang:phash2(erlang:binary_to_term(<<131,112,0,0,0,66,2,215,206,77,69,249,50,170,17,129,47,21,98,13,196,76,242,0,0,0,3,0,0,0,0,100,0,1,116,97,3,98,7,168,160,93,103,100,0,13,110,111,110,111,100,101,64,110,111,104,111,115,116,0,0,0,112,0,0,0,0,0>>),4294967296)",
            new Integer(3482452479U)
        );
        Add(
            "wire-local-4",
            "erlang:phash2(erlang:binary_to_term(<<131,112,0,0,0,66,0,215,206,77,69,249,50,170,17,129,47,21,98,13,196,76,242,0,0,0,4,0,0,0,0,100,0,1,116,97,4,98,2,230,21,171,103,100,0,13,110,111,110,111,100,101,64,110,111,104,111,115,116,0,0,0,112,0,0,0,0,0>>),4294967296)",
            new Integer(633704783U)
        );
        Add(
            "wire-local-5",
            "erlang:phash2(erlang:binary_to_term(<<131,112,0,0,0,92,0,215,206,77,69,249,50,170,17,129,47,21,98,13,196,76,242,0,0,0,5,0,0,0,3,100,0,1,116,97,5,98,0,99,101,130,103,100,0,13,110,111,110,111,100,101,64,110,111,104,111,115,116,0,0,0,112,0,0,0,0,0,100,0,3,97,98,99,100,0,3,97,98,100,104,2,100,0,3,97,98,99,100,0,3,97,98,100>>),4294967296)",
            new Integer(1241537408U)
        );

        Add(
            "map-native-phash2-0",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash2(#{})",
            new Integer(39679005U)
        );
        Add(
            "map-native-phash2-1",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash2(#{ a =\u003E 1, \u0022a\u0022 =\u003E 2, \u003C\u003C\u0022a\u0022\u003E\u003E =\u003E 3, {a,b} =\u003E 4 })",
            new Integer(33667975U)
        );
        Add(
            "map-native-phash2-2",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash2(#{ 1 =\u003E a, 2 =\u003E \u0022a\u0022, 3 =\u003E \u003C\u003C\u0022a\u0022\u003E\u003E, 4 =\u003E {a,b} })",
            new Integer(95332690U)
        );
        Add(
            "map-native-phash2-3",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash2(#{ 1 =\u003E a })",
            new Integer(108954384U)
        );
        Add(
            "map-native-phash2-4",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash2(#{ a =\u003E 1 })",
            new Integer(59617982U)
        );
        Add(
            "map-native-phash2-5",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash2(#{{} =\u003E \u003C\u003C\u003E\u003E})",
            new Integer(42770201U)
        );
        Add(
            "map-native-phash2-6",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash2(#{\u003C\u003C\u003E\u003E =\u003E {}})",
            new Integer(71687700U)
        );
        Add(
            "map-native-phash2-7",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash2(M0)",
            new Integer(70249457U)
        );
        Add(
            "map-native-phash2-8",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash2(M1)",
            new Integer(59617982U)
        );
        Add(
            "map-native-phash2-9",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash2(M2)",
            new Integer(70249457U)
        );
        Add(
            "map-native-phash2-10",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash2(M1000)",
            new Integer(66609305U)
        );
        Add(
            "map-native-phash2-11",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash2(Mnested1)",
            new Integer(113689339U)
        );
        Add(
            "map-native-phash2-12",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash2(Mnested2)",
            new Integer(29167443U)
        );
        Add(
            "map-native-phash-0",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash(#{},4294967296)",
            new Integer(1113425985U)
        );
        Add(
            "map-native-phash-1",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash(#{ a =\u003E 1, \u0022a\u0022 =\u003E 2, \u003C\u003C\u0022a\u0022\u003E\u003E =\u003E 3, {a,b} =\u003E 4 },4294967296)",
            new Integer(1510068139U)
        );
        Add(
            "map-native-phash-2",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash(#{ 1 =\u003E a, 2 =\u003E \u0022a\u0022, 3 =\u003E \u003C\u003C\u0022a\u0022\u003E\u003E, 4 =\u003E {a,b} },4294967296)",
            new Integer(3182345590U)
        );
        Add(
            "map-native-phash-3",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash(#{ 1 =\u003E a },4294967296)",
            new Integer(2927531828U)
        );
        Add(
            "map-native-phash-4",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash(#{ a =\u003E 1 },4294967296)",
            new Integer(1670235874U)
        );
        Add(
            "map-native-phash-5",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash(#{{} =\u003E \u003C\u003C\u003E\u003E},4294967296)",
            new Integer(3935089469U)
        );
        Add(
            "map-native-phash-6",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash(#{\u003C\u003C\u003E\u003E =\u003E {}},4294967296)",
            new Integer(71692856U)
        );
        Add(
            "map-native-phash-7",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash(M0,4294967296)",
            new Integer(2620391445U)
        );
        Add(
            "map-native-phash-8",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash(M1,4294967296)",
            new Integer(1670235874U)
        );
        Add(
            "map-native-phash-9",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash(M2,4294967296)",
            new Integer(2620391445U)
        );
        Add(
            "map-native-phash-10",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash(M1000,4294967296)",
            new Integer(1945662653U)
        );
        Add(
            "map-native-phash-11",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash(Mnested1,4294967296)",
            new Integer(113694495U)
        );
        Add(
            "map-native-phash-12",
            "M0 = #{a=\u003E1,\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1=maps:remove(\u0022key\u0022,M0),M2=M1#{\u0022key\u0022=\u003E\u003C\u003C\u0022value\u0022\u003E\u003E},M1000=maps:from_list([{K,K} || K \u003C- lists:seq(1,1000)]),Mnested1=#{flatmap=\u003EM0,M0=\u003Eflatmap,hashmap=\u003EM1000,M1000=\u003Ehashmap},Mnested2=maps:merge(Mnested1,M1000),erlang:phash(Mnested2,4294967296)",
            new Integer(431825783U)
        );

        return cases.AsReadOnly();
    }
}
