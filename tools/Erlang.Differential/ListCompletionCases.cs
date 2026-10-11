namespace Erlang.Differential;

public static class ListCompletionCases
{
    private static readonly IReadOnlyList<(string Function, int Arity, string Body)> exports =
    [
        ("concat",1,"\"a12b\"=lists:concat([a,12,\"b\"] )"),
        ("delete",2,"[1.0,2,1]=lists:delete(1,[1.0,1,2,1])"),
        ("droplast",1,"[a,b]=lists:droplast([a,b,c])"),
        ("enumerate",1,"[{1,a},{2,b}]=lists:enumerate([a,b])"),
        ("enumerate",2,"[{-2,a},{-1,b}]=lists:enumerate(-2,[a,b])"),
        ("enumerate",3,"[{5,a},{3,b},{1,c}]=lists:enumerate(5,-2,[a,b,c])"),
        ("filter",2,"[1,3]=lists:filter(fun(X)->X rem 2=:=1 end,[1,2,3])"),
        ("flatlength",1,"4=lists:flatlength([[a,[]],b,[c,[d]]])"),
        ("flatmap",2,"[1,1,2,2]=lists:flatmap(fun(X)->[X,X] end,[1,2])"),
        ("join",2,"[a,sep,b,sep,c]=lists:join(sep,[a,b,c])"),
        ("keydelete",3,"[wrong,{1.0,b},{2,c}]=lists:keydelete(1,1,[wrong,{1,a},{1.0,b},{2,c}])"),
        ("keymap",3,"[{a,3},{b,4}]=lists:keymap(fun(X)->X+1 end,2,[{a,2},{b,3}])"),
        ("keymerge",3,"[{1,a},{1.0,b},{2,c},{3,d}]=lists:keymerge(1,[{1,a},{3,d}],[{1.0,b},{2,c}])"),
        ("keyreplace",4,"[wrong,{changed},tail]=lists:keyreplace(1.0,1,[wrong,{1,a},tail],{changed})"),
        ("keysort",2,"[{1.0,b},{1,a},{2,c}]=lists:keysort(1,[{2,c},{1.0,b},{1,a}])"),
        ("keystore",4,"[{a,1},{b,2}]=lists:keystore(b,1,[{a,1}],{b,2})"),
        ("keytake",3,"{value,{1,a},[wrong,{2,b}]}=lists:keytake(1.0,1,[wrong,{1,a},{2,b}])"),
        ("max",1,"[a]=lists:max([1,a,{},[a]])"),
        ("min",1,"1.0=lists:min([1.0,1,2])"),
        ("merge",1,"[1,2,3,4,5]=lists:merge([[1,4],[],[2,5],[3]])"),
        ("merge",2,"[1,1.0,2,3]=lists:merge([1,3],[1.0,2])"),
        ("merge",3,"[1,2,3,4]=lists:merge(fun(A,B)->A=<B end,[1,3],[2,4])"),
        ("merge3",3,"[1,2,3,4,5,6]=lists:merge3([1,4],[2,5],[3,6])"),
        ("prefix",2,"true=lists:prefix([a,b],[a,b,c|tail]),false=lists:prefix([1],[1.0])"),
        ("rkeymerge",3,"[{3,a},{2,c},{1.0,b},{1,d}]=lists:rkeymerge(1,[{3,a},{1,d}],[{2,c},{1.0,b}])"),
        ("rmerge",2,"[3,2,1.0,1]=lists:rmerge([3,1],[2,1.0])"),
        ("rmerge",3,"[4,3,2,1]=lists:rmerge(fun(A,B)->A=<B end,[3,1],[4,2])"),
        ("rmerge3",3,"[6,5,4,3,2,1]=lists:rmerge3([6,3],[5,2],[4,1])"),
        ("rukeymerge",3,"[{3,a},{2,c},{1,d}]=lists:rukeymerge(1,[{3,a},{1,d}],[{2,c},{1.0,b}])"),
        ("rumerge",2,"[3,2,1]=lists:rumerge([3,1],[3.0,2,1.0])"),
        ("rumerge",3,"[3,2,1]=lists:rumerge(fun(A,B)->A=<B end,[3,1],[3.0,2,1.0])"),
        ("rumerge3",3,"[3,2,1]=lists:rumerge3([3,1],[3.0,2],[2.0,1.0])"),
        ("search",2,"{value,3}=lists:search(fun(X)->X>2 end,[1,2,3,4])"),
        ("sort",1,"[1.0,1,2,2,3]=lists:sort([3,1.0,2,1,2])"),
        ("splitwith",2,"{[1,2],[3,1]}=lists:splitwith(fun(X)->X<3 end,[1,2,3,1])"),
        ("sublist",2,"[a,b]=lists:sublist([a,b,c],2)"),
        ("sublist",3,"[b,c]=lists:sublist([a,b,c],2,5)"),
        ("subtract",2,"[1.0,2]=lists:subtract([1,1.0,1,2],[1,1])"),
        ("suffix",2,"true=lists:suffix([b,c],[a,b,c]),false=lists:suffix([1],[1.0])"),
        ("ukeymerge",3,"[{1,a},{2,c},{3,d}]=lists:ukeymerge(1,[{1,a},{3,d}],[{1.0,b},{2,c}])"),
        ("ukeysort",2,"[{1.0,b},{2,c}]=lists:ukeysort(1,[{2,c},{1.0,b},{1,a}])"),
        ("umerge",1,"[1,2,3,4]=lists:umerge([[1,3],[1.0,2],[3.0,4]])"),
        ("umerge",2,"[1,2,3]=lists:umerge([1,3],[1.0,2,3.0])"),
        ("umerge",3,"[1,2,3]=lists:umerge(fun(A,B)->A=<B end,[1,3],[1.0,2,3.0])"),
        ("umerge3",3,"[1,2,3]=lists:umerge3([1,3],[1.0,2],[2.0,3.0])"),
        ("uniq",1,"[1,1.0,2]=lists:uniq([1,1.0,1,2,2])"),
        ("uniq",2,"[{a,1},{b,3}]=lists:uniq(fun({K,_})->K end,[{a,1},{a,2},{b,3}])"),
        ("unzip",1,"{[a,b],[1,2]}=lists:unzip([{a,1},{b,2}])"),
        ("unzip3",1,"{[a,b],[1,2],[x,y]}=lists:unzip3([{a,1,x},{b,2,y}])"),
        ("usort",1,"[1.0,2,3]=lists:usort([3,1.0,2,1,2])"),
        ("usort",2,"[1,2,3]=lists:usort(fun(A,B)->A=<B end,[3,1,2,1])"),
        ("zf",2,"[1,20]=lists:zf(fun(1)->true;(2)->{true,20};(_)->false end,[1,2,3])"),
        ("zip",2,"[{a,1},{b,2}]=lists:zip([a,b],[1,2])"),
        ("zip",3,"[{a,1}]=lists:zip([a,b],[1],trim)"),
        ("zip3",3,"[{a,1,x},{b,2,y}]=lists:zip3([a,b],[1,2],[x,y])"),
        ("zip3",4,"[{a,1,x},{none,2,none}]=lists:zip3([a],[1,2],[x],{pad,{none,none,none}})"),
        ("zipwith",3,"[3,7]=lists:zipwith(fun(A,B)->A+B end,[1,3],[2,4])"),
        ("zipwith",4,"[3,3]=lists:zipwith(fun(A,B)->A+B end,[1,3],[2],{pad,{0,0}})"),
        ("zipwith3",4,"[6]=lists:zipwith3(fun(A,B,C)->A+B+C end,[1],[2],[3])"),
        ("zipwith3",5,"[6]=lists:zipwith3(fun(A,B,C)->A+B+C end,[1,9],[2],[3],trim)")
    ];

    public static string? FixtureName(string function, int arity) => exports.Any(export => export.Function == function && export.Arity == arity) ? "lists-completion-export-" + function + "-" + arity : null;

    private static CompiledModuleCase Case(string name, string body) => new(
        "lists-completion-" + name,
        "-module(oracle_lists_completion). -export([run/0]). run()->" + body + ",done.",
        Term.Tuple(Term.A("ok"), Term.A("done"))
    );

    private static string Error(string call, string reason) => "try " + call + " of _->unexpected catch error:" + reason + "->ok end";

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = exports.Select(export => Case("export-" + export.Function + "-" + export.Arity, export.Body)).ToList();
        cases.AddRange(new[]
        {
            Case(
                "concat-float-exact",
                "\"1.10000000000000008882e+00\"=lists:concat([1.1]),\"-0.00000000000000000000e+00\"=lists:concat([-0.0])"
            ),
            Case("concat-list-identity", "[a,98]=lists:concat([[a],b]),[]=lists:concat([])"),
            Case("delete-tail-short-circuit", "tail=lists:delete(a,[a|tail]),[b|tail]=lists:delete(a,[b,a|tail])"),
            Case("prefix-short-circuit", "false=lists:prefix([a|wrong],[b|wrong]),true=lists:prefix([],[a|wrong])"),
            Case("sublist-short-circuit", "[]=lists:sublist([a|wrong],0),[a]=lists:sublist([a|wrong],1),[]=lists:sublist([],9,wrong)"),
            Case(
                "enumerate-big-step",
                "[{100000000000000000000,a},{0,b},{-100000000000000000000,c}]=lists:enumerate(100000000000000000000,-100000000000000000000,[a,b,c]),[{3,a},{3,b}]=lists:enumerate(3,0,[a,b])"
            ),
            Case(
                "uniq-exact-composite",
                "true=([1,1.0,#{a=>1},#{a=>1.0},[1],[1.0]]=:=lists:uniq([1,1.0,1,#{a=>1},#{a=>1.0},#{a=>1},[1],[1.0],[1]]))"
            ),
            Case(
                "key-invalid-elements-skipped",
                "[a,{},replacement]=lists:keydelete(k,2,[a,{},replacement,{x,k}]),false=lists:keytake(k,99,[a,{}]),[a,{}]=lists:keyreplace(k,99,[a,{}],{})"
            ),
            Case(
                "key-short-circuit-tail",
                "tail=lists:keydelete(k,1,[{k}|tail]),[{new}|tail]=lists:keyreplace(k,1,[{k}|tail],{new}),{value,{k},tail}=lists:keytake(k,1,[{k}|tail])"
            ),
            Case("keysort-singleton-no-element", "[wrong]=lists:keysort(99,[wrong]),[{}]=lists:ukeysort(99,[{}])"),
            Case(
                "merge-empty-tail-identity",
                "[a|tail]=lists:merge([a|tail],[]),[a|tail]=lists:rumerge([],[a|tail]),[a|tail]=lists:keymerge(999,[a|tail],[])"
            ),
            Case(
                "merge-callback-trace",
                "put(trace,[]),[1,2,3,4]=lists:merge(fun(A,B)->put(trace,[{A,B}|get(trace)]),A=<B end,[1,3],[2,4]),[{1,2},{3,2},{3,4}]=lists:reverse(get(trace))"
            ),
            Case(
                "umerge-callback-trace",
                "put(trace,[]),[1,2,3]=lists:umerge(fun(A,B)->put(trace,[{A,B}|get(trace)]),A=<B end,[1,3],[1,2,3]),[{1,1},{3,1},{1,1},{3,2},{3,3},{3,3}]=lists:reverse(get(trace))"
            ),
            Case(
                "rumerge-callback-trace",
                "put(trace,[]),[3,2,1]=lists:rumerge(fun(A,B)->put(trace,[{A,B}|get(trace)]),A=<B end,[3,1],[3,2,1]),[{3,3},{3,2},{3,3},{1,2},{1,1},{1,1}]=lists:reverse(get(trace))"
            ),
            Case(
                "usort-callback-trace",
                "put(trace,[]),[1,2,3]=lists:usort(fun(A,B)->put(trace,[{A,B}|get(trace)]),A=<B end,[3,1,2,1]),[{3,1},{1,2},{2,1},{2,1},{1,1},{1,1},{3,2}]=lists:reverse(get(trace))"
            ),
            Case("sort-mixed-order", "[1,1.0,a,{},#{},[],[a],<<1>>]=lists:sort([<<1>>,[a],[],#{},{},a,1,1.0])"),
            Case("sort-stability-many-equals", "[1.0,1,1.0,1]=lists:sort([1.0,1,1.0,1]),[1.0]=lists:usort([1.0,1,1.0,1])"),
            Case(
                "usort-comparator-stable-equals",
                "[{1,first},{2,third}]=lists:usort(fun({A,_},{B,_})->A=<B end,[{2,third},{1,first},{1,second},{2,fourth}])"
            ),
            Case(
                "keysort-stable-groups",
                "[{1,first},{1.0,second},{2,third},{2,fourth}]=lists:keysort(1,[{2,third},{1,first},{2,fourth},{1.0,second}])"
            ),
            Case(
                "zip-pad-two",
                "[{a,1},{b,0},{c,0}]=lists:zip([a,b,c],[1],{pad,{none,0}}),[{none,1},{none,2}]=lists:zip([],[1,2],{pad,{none,0}})"
            ),
            Case("zip3-trim-improper-remainder", "[]=lists:zip3([],[a|tail],[b],trim)"),
            Case(
                "zip3-pad-each-side",
                "[{a,x,z},{b,x,z}]=lists:zip3([a,b],[],[],{pad,{none,x,z}}),[{x,a,z},{x,b,z}]=lists:zip3([],[a,b],[],{pad,{x,none,z}}),[{x,y,a},{x,y,b}]=lists:zip3([],[],[a,b],{pad,{x,y,none}})"
            ),
            Case(
                "zip-with-invalid-mode-late",
                "put(called,0)," + Error("lists:zipwith(fun(_,_) ->put(called,get(called)+1) end,[a],[b],wrong)","function_clause") + ",1=get(called)"
            ),
            Case(
                "zip-with-trim-no-callback",
                "put(called,false),[]=lists:zipwith(fun(_,_) ->put(called,true) end,[],[a|tail],trim),false=get(called)"
            ),
            Case(
                "keymap-callback-order",
                "put(trace,[]),[{1},{2}]=lists:keymap(fun(X)->put(trace,[X|get(trace)]),X end,1,[{1},{2}]),[2,1]=get(trace)"
            ),
            Case(
                "flatmap-late-append-error",
                "put(trace,[])," + Error("lists:flatmap(fun(X)->put(trace,[X|get(trace)]),wrong end,[1,2])","badarg") + ",[2,1]=get(trace)"
            ),
            Case(
                "flatmap-invalid-tail-before-append",
                "put(trace,[])," + Error("lists:flatmap(fun(X)->put(trace,[X|get(trace)]),wrong end,[1|tail])","function_clause") + ",[1]=get(trace)"
            ),
            Case(
                "search-tail-short-circuit",
                "{value,1}=lists:search(fun(_)->true end,[1|tail]),false=lists:search(fun(_)->false end,[])"
            ),
            Case("splitwith-tail-short-circuit", "{[],[1|tail]}=lists:splitwith(fun(_)->false end,[1|tail])"),
            Case(
                "uniq-every-key-callback",
                "put(trace,[]),[1]=lists:uniq(fun(X)->put(trace,[X|get(trace)]),same end,[1,2,3]),[3,2,1]=get(trace)"
            ),
            Case("map-initial-invalid", Error("lists:map(fun(X)->X end,wrong)","{case_clause,wrong}")),
            Case("map-improper-tail", Error("lists:map(fun(X)->X end,[1|wrong])","function_clause")),
            Case("map-invalid-function", Error("lists:map(wrong,[])","function_clause")),
            Case("sum-improper-tail", Error("lists:sum([1|wrong])","function_clause")),
            Case("sum-invalid-element-first", Error("lists:sum([wrong|tail])","badarith")),
            Case("filter-invalid-result", Error("lists:filter(fun(_)->wrong end,[a])","{bad_filter,wrong}")),
            Case("filter-invalid-generator", Error("lists:filter(fun(_)->true end,wrong)","{bad_generator,wrong}")),
            Case("filter-improper-generator", Error("lists:filter(fun(_)->true end,[a|wrong])","{bad_generator,wrong}")),
            Case("keymap-element-before-badfun", Error("lists:keymap(wrong,99,[{}])","badarg")),
            Case("keymap-badfun", Error("lists:keymap(wrong,1,[{a}])","{badfun,wrong}")),
            Case("keymap-empty-badfun", Error("lists:keymap(wrong,1,[])","function_clause")),
            Case("keymap-empty-bad-index", Error("lists:keymap(fun(X)->X end,0,[])","function_clause")),
            Case("keysort-invalid-list", Error("lists:keysort(1,wrong)","{case_clause,wrong}")),
            Case("keysort-invalid-element", Error("lists:keysort(1,[wrong,{}])","badarg")),
            Case("usort-invalid-function", Error("lists:usort(wrong,[1,2])","function_clause")),
            Case("usort-invalid-result", Error("lists:usort(fun(_,_) ->wrong end,[1,2])","{case_clause,wrong}")),
            Case("zip-fail-length", Error("lists:zip([a],[])","function_clause")),
            Case("zip-pad-improper-tail", Error("lists:zip([],[a|wrong],{pad,{x,y}})","{bad_generator,wrong}")),
            Case("zip-with-empty-badfun", Error("lists:zipwith(wrong,[],[])","function_clause")),
            Case("zip-with-nonempty-badfun", Error("lists:zipwith(wrong,[a],[b])","{badfun,wrong}")),
            Case("zip3-trim-invalid-spine", Error("lists:zip3([],wrong,[a],trim)","function_clause"))
        });
        foreach (var (name, call) in new[]
        {
            ("concat","lists:concat(wrong)"),("concat-element","lists:concat([{}])"),("delete","lists:delete(a,wrong)"),
            ("droplast","lists:droplast([])"),("enumerate","lists:enumerate(wrong,[])"),("enumerate-list","lists:enumerate(wrong)"),
            ("flatlength","lists:flatlength([[]|wrong])"),("join","lists:join(sep,wrong)"),("min","lists:min([])"),("max","lists:max(wrong)"),
            ("prefix","lists:prefix([],wrong)"),("sublist","lists:sublist([], -1)"),("sublist-start","lists:sublist([a],0,1)"),
            ("keydelete","lists:keydelete(a,0,[])"),("keyreplace","lists:keyreplace(a,1,[],wrong)"),("keystore","lists:keystore(a,1,[],wrong)"),
            ("keytake","lists:keytake(a,1,wrong)"),("uniq","lists:uniq([a|wrong])"),("unzip","lists:unzip([{a,b,c}])"),("unzip3","lists:unzip3([{a,b}])"),
            ("filter-fun","lists:filter(wrong,[])"),("flatmap-fun","lists:flatmap(wrong,[])"),("search-fun","lists:search(wrong,[])"),
            ("splitwith-fun","lists:splitwith(wrong,[])"),("uniq-fun","lists:uniq(wrong,[])"),("zf-fun","lists:zf(wrong,[])"),
            ("merge","lists:merge(wrong,[])"),("rmerge","lists:rmerge([],wrong)"),("umerge-fun","lists:umerge(wrong,[],[])"),
            ("keymerge-index","lists:keymerge(0,[],[])"),("ukeysort-index","lists:ukeysort(0,[])"),("merge3","lists:merge3([],[],wrong)")
        })
            cases.Add(Case("error-" + name, Error(call, "function_clause")));
        foreach (var (name, call) in new[]
        {
            ("subtract-left","lists:subtract(wrong,[])"),("subtract-right","lists:subtract([],wrong)"),
            ("suffix","lists:suffix([],wrong)"),("suffix-improper","lists:suffix([a|wrong],[a])")
        })
            cases.Add(Case("error-" + name, Error(call, "badarg")));
        foreach (string operation in new[] { "filter", "flatmap", "search", "splitwith", "uniq" })
        {
            cases.Add(Case(operation + "-callback-error", Error("lists:" + operation + "(fun(_)->erlang:error(callback) end,[a])", "callback")));
            cases.Add(Case(
                operation + "-callback-throw",
                "try lists:" + operation + "(fun(_)->throw(callback) end,[a]) of _->unexpected catch throw:callback->ok end"
            ));
            cases.Add(Case(
                operation + "-callback-exit",
                "try lists:" + operation + "(fun(_)->exit(callback) end,[a]) of _->unexpected catch exit:callback->ok end"
            ));
        }
        foreach (string operation in new[] { "merge", "rmerge", "umerge", "rumerge", "usort" })
        {
            string arguments = operation == "usort" ? ",[1,2]" : ",[1],[2]";
            cases.Add(Case(
                operation + "-comparator-error",
                Error("lists:" + operation + "(fun(_,_) ->erlang:error(callback) end" + arguments + ")", "callback")
            ));
            cases.Add(Case(
                operation + "-comparator-throw",
                "try lists:" + operation + "(fun(_,_) ->throw(callback) end" + arguments + ") of _->unexpected catch throw:callback->ok end"
            ));
        }

        var random = new Random(109);
        for (int sample = 0; sample < 40; sample++)
        {
            int[] input = Enumerable.Range(0, sample % 2 == 0 ? 24 : 64).Select(_ => random.Next(-4, 5)).ToArray();
            int[] counts = new int[9];
            foreach (int value in input)
                counts[value + 4]++;
            string vector = "[" + string.Join(',', input) + "]";
            string sorted = "[" + string.Join(',', Enumerable.Range(-4, 9).SelectMany(value => Enumerable.Repeat(value, counts[value + 4]))) + "]";
            string distinct = "[" + string.Join(',', Enumerable.Range(-4, 9).Where(value => counts[value + 4] != 0)) + "]";
            string keys = "[" + string.Join(
                ',',
                Enumerable.Range(-4, 9).SelectMany(value => input.Select((item, index) => (item, index)).Where(pair => pair.item == value).Select(pair => "{" + value + "," + (pair.index + 1) + "}"))
            ) + "]";
            string uniqueKeys = "[" + string.Join(
                ',',
                Enumerable.Range(-4, 9).Where(value => counts[value + 4] != 0).Select(value => "{" + value + "," + (Array.IndexOf(input, value) + 1) + "}")
            ) + "]";
            cases.Add(Case(
                "seeded-sorts-" + sample,
                "L=" + vector + ",S=" + sorted + ",U=" + distinct + ",S=lists:sort(L),U=lists:usort(L),S=lists:sort(fun(A,B)->A=<B end,L),U=lists:usort(fun(A,B)->A=<B end,L),K=[{X,I}||{I,X}<-lists:enumerate(L)]," + keys + "=lists:keysort(1,K)," + uniqueKeys + "=lists:ukeysort(1,K)"
            ));
        }
        foreach (var (name, input, trace) in new[]
        {
            ("ascending", "[1,2,3]", "[{1,2},{2,1},{2,3},{3,2}]"),
            ("descending", "[3,2,1]", "[{3,2},{2,1}]"),
            ("split", "[2,1,3]", "[{2,1},{1,3},{3,1},{2,3},{2,1},{3,2}]")
        })
            cases.Add(Case(
                "usort-trace-" + name,
                "put(trace,[]),[1,2,3]=lists:usort(fun(A,B)->put(trace,[{A,B}|get(trace)]),A=<B end," + input + ")," + trace + "=lists:reverse(get(trace))"
            ));
        foreach (var (name, call) in new[]
        {
            ("filter-arity", "lists:filter(fun(_,_) ->true end,[])"),
            ("flatmap-arity", "lists:flatmap(fun(_,_) ->[] end,[])"),
            ("search-arity", "lists:search(fun(_,_) ->true end,[])"),
            ("uniq-arity", "lists:uniq(fun(_,_) ->same end,[])"),
            ("merge-arity", "lists:merge(fun(_)->true end,[],[])"),
            ("rumerge-arity", "lists:rumerge(fun(_)->true end,[],[])"),
            ("usort-arity", "lists:usort(fun(_)->true end,[1,2])")
        })
            cases.Add(Case("error-" + name, Error(call, "function_clause")));
        cases.Add(Case(
            "callback-badarity-selected",
            "try lists:zipwith(fun(_)->same end,[a],[b]) of _->unexpected catch error:{badarity,_}->ok end"
        ));
        cases.Add(Case(
            "empty-contracts",
            "[]=lists:concat([]),[]=lists:enumerate([]),0=lists:flatlength([]),[]=lists:join(sep,[]),[]=lists:uniq([]),{[],[]}=lists:unzip([]),{[],[],[]}=lists:unzip3([]),[]=lists:merge([]),[]=lists:umerge([]),[]=lists:merge3([],[],[]),[]=lists:rumerge3([],[],[])"
        ));

        for (int sample = 0; sample < 24; sample++)
        {
            int[] left = Enumerable.Range(0, 12).Where(_ => random.Next(2) == 0).ToArray();
            int[] right = Enumerable.Range(0, 12).Where(_ => random.Next(2) == 0).ToArray();
            int[] third = Enumerable.Range(0, 12).Where(_ => random.Next(2) == 0).ToArray();

            string Values(IEnumerable<int> values) => "[" + string.Join(',', values) + "]";
            string all = Values(Enumerable.Range(0, 12).SelectMany(value => Enumerable.Repeat(value, (left.Contains(value) ? 1 : 0) + (right.Contains(value) ? 1 : 0) + (third.Contains(value) ? 1 : 0))));
            string unique = Values(Enumerable.Range(0, 12).Where(value => left.Contains(value) || right.Contains(value) || third.Contains(value)));
            string reversedAll = Values(Enumerable.Range(0, 12).Reverse().SelectMany(value => Enumerable.Repeat(value, (left.Contains(value) ? 1 : 0) + (right.Contains(value) ? 1 : 0) + (third.Contains(value) ? 1 : 0))));
            string reversedUnique = Values(Enumerable.Range(0, 12).Reverse().Where(value => left.Contains(value) || right.Contains(value) || third.Contains(value)));
            cases.Add(Case(
                "seeded-merges-" + sample,
                "A=" + Values(left) + ",B=" + Values(right) + ",C=" + Values(third) + ",S=" + all + ",U=" + unique + ",S=lists:merge([A,[],B,C]),S=lists:merge3(A,B,C),U=lists:umerge([A,[],B,C]),U=lists:umerge3(A,B,C),AR=lists:reverse(A),BR=lists:reverse(B),CR=lists:reverse(C)," + reversedAll + "=lists:rmerge3(AR,BR,CR)," + reversedUnique + "=lists:rumerge3(AR,BR,CR)"
            ));
        }

        foreach (string operation in new[] { "keysort", "ukeysort" })
            cases.Add(Case(
                operation + "-error-order",
                Error("lists:" + operation + "(1,[wrong,{}|tail])", "badarg") + "," + Error("lists:" + operation + "(1,[{a},wrong|tail])", "badarg") + "," + Error("lists:" + operation + "(1,[wrong|tail])", "{case_clause,[wrong|tail]}")
            ));

        foreach (string operation in new[] { "merge", "umerge" })
            cases.Add(Case(operation + "-many-error-order", Error("lists:" + operation + "([[0],[1],[2|tail]|wrong])", "badarg")));

        return cases;
    }
}

