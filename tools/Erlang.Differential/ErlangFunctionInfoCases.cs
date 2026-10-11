using Erlang;
namespace Erlang.Differential;

public static class ErlangFunctionInfoCases
{
    public static string? FixtureName(string function, int arity) => (function, arity) switch
    {
        ("fun_info", 1) => "erlang-fun-info-external-info-order",
        ("fun_info", 2) => "erlang-fun-info-local-environment",
        ("fun_info_mfa", 1) => "erlang-fun-info-external-mfa",
        ("fun_to_list", 1) => "erlang-fun-info-external-text-0",
        _ => null
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(
            string name,
            string body,
            Term expected,
            string helpers = ""
        ) => cases.Add(new(
            "erlang-fun-info-" + name,
            "-module(fun_info_probe). -export([run/0,maker/2]). run()->" + body + ". maker(Value,_Unused)->fun()->Value end. " + helpers,
            Term.Tuple(Term.A("ok"), expected)
        ));
        var external = new (string Key, Term Expected)[]
        {
            ("type", Term.A("external")), ("env", Nil.Value), ("arity", Term.I(1)),
            ("name", Term.A("length")), ("module", Term.A("erlang")),
            ("pid", Term.A("undefined")), ("refc", Term.A("undefined")),
            ("uniq", Term.A("undefined")), ("index", Term.A("undefined")),
            ("new_index", Term.A("undefined")), ("new_uniq", Term.A("undefined"))
        };
        foreach (var field in external)
            Add(
                "external-" + field.Key,
                "erlang:fun_info(erlang:make_fun(erlang,length,1)," + field.Key + ")",
                Term.Tuple(Term.A(field.Key), field.Expected)
            );
        Add(
            "external-info-order",
            "[{module,erlang},{name,length},{arity,1},{env,[]},{type,external}]=erlang:fun_info(erlang:make_fun(erlang,length,1)),true",
            Term.A("true")
        );
        Add(
            "external-mfa",
            "erlang:fun_info_mfa(erlang:make_fun(erlang,length,1))",
            Term.Tuple(Term.A("erlang"), Term.A("length"), Term.I(1))
        );
        Add(
            "unavailable-mfa",
            "erlang:fun_info_mfa(erlang:make_fun(unavailable_mod,private_fun,255))",
            Term.Tuple(Term.A("unavailable_mod"), Term.A("private_fun"), Term.I(255))
        );
        Add("local-type", "erlang:fun_info(maker(42,unused),type)", Term.Tuple(Term.A("type"), Term.A("local")));
        Add("local-environment", "erlang:fun_info(maker(42,unused),env)", Term.Tuple(Term.A("env"), Term.List(Term.I(42))));
        Add("local-refc", "erlang:fun_info(maker(42,unused),refc)", Term.Tuple(Term.A("refc"), Term.I(1)));
        Add("local-arity", "erlang:fun_info(maker(42,unused),arity)", Term.Tuple(Term.A("arity"), Term.I(0)));
        Add("local-module", "erlang:fun_info(maker(42,unused),module)", Term.Tuple(Term.A("module"), Term.A("fun_info_probe")));
        Add(
            "local-owner",
            "{pid,P}=erlang:fun_info(maker(42,unused),pid),{is_pid(P),P=:=whereis(init),P=/=self(),erlang:is_process_alive(P),lists:member(P,erlang:processes())}",
            Term.Tuple(
                Term.A("true"),
                Term.A("true"),
                Term.A("true"),
                Term.A("true"),
                Term.A("true")
            )
        );
        Add(
            "local-name-and-mfa",
            "F=maker(42,unused),{module,M}=erlang:fun_info(F,module),{name,N}=erlang:fun_info(F,name),{arity,A}=erlang:fun_info(F,arity),{is_atom(N),erlang:fun_info_mfa(F)=:={M,N,A}}",
            Term.Tuple(Term.A("true"), Term.A("true"))
        );
        Add(
            "local-code-fields",
            "F=maker(42,unused),{new_uniq,U}=erlang:fun_info(F,new_uniq),{uniq,O}=erlang:fun_info(F,uniq),{index,I}=erlang:fun_info(F,index),{new_index,N}=erlang:fun_info(F,new_index),{is_binary(U),byte_size(U),is_integer(O),O>=0,O<134217728,is_integer(I),I>=0,is_integer(N),N>=0}",
            Term.Tuple(
                Term.A("true"),
                Term.I(16),
                Term.A("true"),
                Term.A("true"),
                Term.A("true"),
                Term.A("true"),
                Term.A("true"),
                Term.A("true"),
                Term.A("true")
            )
        );
        Add(
            "local-info-order",
            "F=maker(42,unused),[pid,module,new_index,new_uniq,index,uniq,name,arity,env,type]=[K||{K,_}<-erlang:fun_info(F)],true",
            Term.A("true")
        );
        Add(
            "local-stable-identity",
            "A=maker(42,left),B=maker(42,right),{A=:=B,A==B,A(),B(),#{A=>old,B=>new}=:=#{A=>new}}",
            Term.Tuple(
                Term.A("true"),
                Term.A("true"),
                Term.I(42),
                Term.I(42),
                Term.A("true")
            )
        );
        Add(
            "local-environment-inequality",
            "{maker(42,left)=/=maker(43,right),maker(42,left)=/=maker(42.0,right),maker(42,left)<maker(43,right)}",
            Term.Tuple(Term.A("true"), Term.A("true"), Term.A("true"))
        );
        Add(
            "local-numeric-order",
            "{maker(42,left)==maker(42.0,right),maker(42,left)<maker(42.0,right),maker(42,left)>maker(42.0,right)}",
            Term.Tuple(Term.A("true"), Term.A("false"), Term.A("false"))
        );
        Add(
            "local-distinct-code",
            "A=fun()->same end,B=fun()->same end,{A=/=B,A(),B()}",
            Term.Tuple(Term.A("true"), Term.A("same"), Term.A("same"))
        );
        Add(
            "local-parameter-shadow",
            "F=shadow(outer),{erlang:fun_info(F,env),F(inner)}",
            Term.Tuple(Term.Tuple(Term.A("env"), Nil.Value), Term.A("inner")),
            "shadow(X)->fun(X)->X end."
        );
        Add(
            "local-nested-env",
            "F=nested(42,unused),G=F(),{erlang:fun_info(F,env),erlang:fun_info(G,env),G()}",
            Term.Tuple(Term.Tuple(Term.A("env"), Term.List(Term.I(42))), Term.Tuple(Term.A("env"), Term.List(Term.I(42))), Term.I(42)),
            "nested(X,_Y)->fun()->fun()->X end end."
        );
        Add(
            "local-generator-shadow",
            "F=generator(outer),{erlang:fun_info(F,env),F()}",
            Term.Tuple(Term.Tuple(Term.A("env"), Nil.Value), Term.List(Term.I(1), Term.I(2))),
            "generator(X)->fun()->[X||X<-[1,2]] end."
        );
        Add(
            "local-map-key-env",
            "F=key_fun(key),{erlang:fun_info(F,env),F(#{key=>value})}",
            Term.Tuple(Term.Tuple(Term.A("env"), Term.List(Term.A("key"))), Term.A("value")),
            "key_fun(Key)->fun(#{Key:=Value})->Value end."
        );
        Add(
            "local-binary-size-env",
            "F=bit_fun(4),{erlang:fun_info(F,env),F(<<9:4>>)}",
            Term.Tuple(Term.Tuple(Term.A("env"), Term.List(Term.I(4))), Term.I(9)),
            "bit_fun(Size)->fun(<<Value:Size>>)->Value end."
        );
        Add(
            "local-owner-cross-process",
            "Parent=self(),F=maker(42,unused),spawn(fun()->{pid,P}=erlang:fun_info(F,pid),Parent!{owner,P=:=whereis(init),F()} end),receive {owner,B,V}->{B,V} after 1000->timeout end",
            Term.Tuple(Term.A("true"), Term.I(42))
        );
        Add(
            "local-init-status",
            "P=whereis(init),P!{self(),get_status},receive {init,S}->S after 1000->timeout end",
            Term.Tuple(Term.A("started"), Term.A("started"))
        );
        Add(
            "local-text-coherence",
            "F=maker(42,unused),{module,M}=erlang:fun_info(F,module),{index,I}=erlang:fun_info(F,index),{uniq,U}=erlang:fun_info(F,uniq),erlang:fun_to_list(F)=:=\"#Fun<\"++atom_to_list(M)++\".\"++integer_to_list(I)++\".\"++integer_to_list(U)++\">\"",
            Term.A("true")
        );
        Add("local-text-excludes-env", "erlang:fun_to_list(maker(42,left))=:=erlang:fun_to_list(maker(43,right))", Term.A("true"));
        Add(
            "local-guard-env",
            "F=guard_fun(5,unused),{erlang:fun_info(F,env),F(6),F(4)}",
            Term.Tuple(Term.Tuple(Term.A("env"), Term.List(Term.I(5))), Term.A("large"), Term.A("small")),
            "guard_fun(Limit,_Unused)->fun(X) when X>Limit->large;(_)->small end."
        );
        Add(
            "local-outer-pattern-env",
            "F=match_fun(value,unused),{erlang:fun_info(F,env),F(value),F(other)}",
            Term.Tuple(Term.Tuple(Term.A("env"), Term.List(Term.A("value"))), Term.A("matched"), Term.A("other")),
            "match_fun(X,_Unused)->fun(V)->case V of X->matched;_->other end end."
        );
        Add(
            "local-size-shadow-env",
            "F=size_shadow(4),{erlang:fun_info(F,env),F(<<9:4>>)}",
            Term.Tuple(Term.Tuple(Term.A("env"), Term.List(Term.I(4))), Term.I(9)),
            "size_shadow(X)->fun(<<X:X>>)->X end."
        );
        Add(
            "local-multiple-env",
            "F=multi(z,a,unused),{env,E}=erlang:fun_info(F,env),{lists:sort(E),F()}",
            Term.Tuple(Term.List(Term.A("a"), Term.A("z")), Term.Tuple(Term.A("z"), Term.A("a"))),
            "multi(Z,A,_Unused)->fun()->{Z,A} end."
        );
        Add(
            "local-dead-rhs-env",
            "F=dead_rhs(unused),{erlang:fun_info(F,env),F()}",
            Term.Tuple(Term.Tuple(Term.A("env"), Nil.Value), Term.A("false")),
            "dead_rhs(X)->fun()->false andalso X end."
        );
        Add(
            "local-complex-env",
            "V=#{key=>[1,2|tail]},F=maker(V,unused),{erlang:fun_info(F,env),F()}",
            Term.Tuple(
                Term.Tuple(
                    Term.A("env"),
                    Term.List(new MapTerm([new KeyValuePair<Term, Term>(Term.A("key"), Cons.From([Term.I(1), Term.I(2)], Term.A("tail")))]))
                ),
                new MapTerm([new KeyValuePair<Term, Term>(Term.A("key"), Cons.From([Term.I(1), Term.I(2)], Term.A("tail")))])
            )
        );
        Add(
            "local-function-env",
            "V=erlang:make_fun(erlang,length,1),F=maker(V,unused),{env,[E]}=erlang:fun_info(F,env),{E=:=V,F()=:=V,E([a,b])}",
            Term.Tuple(Term.A("true"), Term.A("true"), Term.I(2))
        );
        var texts = new (string Module, string Function, string Text)[]
        {
            ("erlang", "length", "fun erlang:length/1"),
            ("'Mod Name'", "'quoted name'", "fun 'Mod Name':'quoted name'/1"),
            ("''", "''", "fun '':''/1"),
            ("'àé'", "'aØ_1'", "fun àé:aØ_1/1"),
            ("'λ'", "'😀'", "fun 'λ':'😀'/1"),
            ("list_to_atom([109,10,9])", "list_to_atom([102,39,92])", "fun 'm\\n\\t':'f\\'\\\\'/1"),
            ("list_to_atom([0,1,27,128,159])", "f", "fun '\\000\\001\\033\\200\\237':f/1")
        };
        for (int index = 0; index < texts.Length; index++)
        {
            var text = texts[index];
            Add(
                "external-text-" + index,
                "erlang:fun_to_list(erlang:make_fun(" + text.Module + "," + text.Function + ",1))",
                Term.String(text.Text)
            );
        }
        foreach (string call in new[] { "erlang:fun_info(not_fun)", "erlang:fun_info(not_fun,type)", "erlang:fun_info_mfa(not_fun)", "erlang:fun_to_list(not_fun)", "erlang:fun_info(maker(1,2),invalid)", "erlang:fun_info(maker(1,2),[])", "erlang:fun_info(erlang:make_fun(erlang,length,1),invalid)" })
            Add("invalid-" + cases.Count, "try " + call + " catch error:badarg->badarg end", Term.A("badarg"));

        return cases.AsReadOnly();
    }

}
