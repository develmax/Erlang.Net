namespace Erlang.Differential;

public static class ProcessAliasCases
{
    public static string? FixtureName(string function, int arity) => (function, arity) switch
    {
        ("alias", 0) => "erlang-alias-default",
        ("alias", 1) => "erlang-alias-explicit",
        ("unalias", 1) => "erlang-alias-deactivate",
        ("send", 3) => "erlang-alias-send-options",
        ("send_nosuspend", 2) => "erlang-alias-nosuspend",
        ("send_nosuspend", 3) => "erlang-alias-nosuspend-options",
        _ => null
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-alias-" + name,
            "-module(alias_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A("ok"), Term.A("done"))
        ));
        Add(
            "default",
            "A=erlang:alias(),true=is_reference(A),ping=A!ping,receive ping->ok after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "explicit",
            "A=erlang:alias([explicit_unalias]),A!one,A!two,receive one->ok after 1000->error(timeout) end,receive two->ok after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "deactivate",
            "A=erlang:alias(),true=erlang:unalias(A),false=erlang:unalias(A),dropped=A!dropped,receive dropped->error(unexpected) after 0->ok end"
        );
        Add(
            "admitted-survives-deactivate",
            "A=erlang:alias(),A!kept,true=erlang:unalias(A),receive kept->ok after 1000->error(timeout) end"
        );
        Add(
            "reply",
            "A=erlang:alias([reply]),A!first,A!second,receive first->ok after 1000->error(timeout) end,receive second->error(unexpected) after 0->ok end,false=erlang:unalias(A)"
        );
        Add(
            "reply-before-selection",
            "A=erlang:alias([reply]),A!ignored,self()!barrier,receive barrier->ok after 1000->error(timeout) end,false=erlang:unalias(A),receive ignored->ok after 0->error(lost) end"
        );
        Add(
            "last-explicit",
            "A=erlang:alias([reply,explicit_unalias]),A!one,A!two,receive one->ok after 1000->error(timeout) end,receive two->ok after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "last-reply",
            "A=erlang:alias([explicit_unalias,reply]),A!one,A!two,receive one->ok after 1000->error(timeout) end,receive two->error(unexpected) after 0->ok end,false=erlang:unalias(A)"
        );
        Add(
            "duplicate-options",
            "A=erlang:alias([priority,priority,reply,reply,explicit_unalias]),ok=erlang:send(A,p,[priority]),receive p->ok after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "ordinary-reference",
            "R=make_ref(),false=erlang:unalias(R),lost=R!lost,ok=erlang:send(R,also_lost,[priority]),receive _->error(unexpected) after 0->ok end"
        );
        Add(
            "different-owner",
            "P=self(),A=erlang:alias(),spawn(fun()->P!{result,erlang:unalias(A)},A!reply end),receive {result,false}->ok after 1000->error(timeout) end,receive reply->ok after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "owner-exit",
            "P=self(),{C,R}=spawn_monitor(fun()->A=erlang:alias(),P!A end),A=receive X when is_reference(X)->X after 1000->error(timeout) end,receive {'DOWN',R,process,C,normal}->ok after 1000->error(timeout) end,false=erlang:unalias(A),gone=A!gone"
        );
        Add(
            "wire-roundtrip",
            "A=erlang:alias(),B=erlang:binary_to_term(erlang:term_to_binary(A)),true=(A=:=B),B!roundtrip,receive roundtrip->ok after 1000->error(timeout) end,true=erlang:unalias(B),false=erlang:unalias(A)"
        );
        Add(
            "compressed-wire",
            "A=erlang:alias([reply]),B=erlang:binary_to_term(erlang:term_to_binary(A,[compressed])),B!one,A!two,receive one->ok after 1000->error(timeout) end,receive two->error(unexpected) after 0->ok end,false=erlang:unalias(B)"
        );
        Add(
            "send-options",
            "A=erlang:alias(),ok=erlang:send(A,p,[nosuspend,noconnect]),receive p->ok after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "nosuspend",
            "A=erlang:alias(),true=erlang:send_nosuspend(A,p),receive p->ok after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "nosuspend-options",
            "A=erlang:alias([priority]),true=erlang:send_nosuspend(A,p,[priority,noconnect]),receive p->ok after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "priority-order",
            "A=erlang:alias([priority]),self()!ordinary,ok=erlang:send(A,first,[priority]),ok=erlang:send(A,second,[priority]),first=receive M1->M1 after 1000->error(timeout) end,second=receive M2->M2 after 1000->error(timeout) end,ordinary=receive M3->M3 after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "permission-without-request",
            "A=erlang:alias([priority]),self()!ordinary,A!plain,ordinary=receive M1->M1 after 1000->error(timeout) end,plain=receive M2->M2 after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "request-without-permission",
            "A=erlang:alias(),self()!ordinary,ok=erlang:send(A,plain,[priority]),ordinary=receive M1->M1 after 1000->error(timeout) end,plain=receive M2->M2 after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "pid-priority-is-ordinary",
            "self()!ordinary,ok=erlang:send(self(),plain,[priority]),ordinary=receive M1->M1 after 1000->error(timeout) end,plain=receive M2->M2 after 1000->error(timeout) end"
        );
        Add(
            "selective-priority",
            "A=erlang:alias([priority]),self()!wanted,ok=erlang:send(A,unmatched,[priority]),receive wanted->ok after 1000->error(timeout) end,ok=erlang:send(A,new,[priority]),unmatched=receive M1->M1 after 1000->error(timeout) end,new=receive M2->M2 after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "remove-priority-tail",
            "A=erlang:alias([priority]),ok=erlang:send(A,first,[priority]),ok=erlang:send(A,last,[priority]),receive last->ok after 1000->error(timeout) end,self()!normal,ok=erlang:send(A,new,[priority]),first=receive M1->M1 after 1000->error(timeout) end,new=receive M2->M2 after 1000->error(timeout) end,normal=receive M3->M3 after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "reply-priority-kept",
            "A=erlang:alias([reply,priority]),self()!normal,ok=erlang:send(A,urgent,[priority]),urgent=receive M1->M1 after 1000->error(timeout) end,normal=receive M2->M2 after 1000->error(timeout) end,false=erlang:unalias(A)"
        );
        Add(
            "local-named-tuple",
            "true=register(alias_receiver,self()),ok=erlang:send({alias_receiver,node()},p,[priority,noconnect]),receive p->ok after 1000->error(timeout) end,true=unregister(alias_receiver)"
        );
        Add(
            "local-absent-tuple",
            "ok=erlang:send({alias_absent,node()},lost,[]),lost={alias_absent,node()}!lost,true=erlang:send_nosuspend({alias_absent,node()},lost)"
        );
        Add(
            "remote-no-connect",
            "noconnect=erlang:send({absent,'alias_missing@nohost'},lost,[noconnect]),false=erlang:send_nosuspend({absent,'alias_missing@nohost'},lost,[noconnect])"
        );
        int index = 0;
        foreach (string option in new[] { "bad", "42", "[bad]", "[reply|bad]", "[priority,42]", "[{reply,true}]", "[priority|42]" })
            Add("invalid-alias-" + index++, "ok=try erlang:alias(" + option + ") of _->unexpected catch error:badarg->ok end");
        index = 0;
        foreach (string value in new[] { "bad", "42", "[]", "self()", "{}" })
            Add("invalid-unalias-" + index++, "ok=try erlang:unalias(" + value + ") of _->unexpected catch error:badarg->ok end");
        index = 0;
        foreach (string options in new[] { "bad", "[bad]", "[priority|bad]", "[{nosuspend,true}]" })
            Add(
                "invalid-send-" + index++,
                "A=erlang:alias([reply]),ok=try erlang:send(A,lost," + options + ") of _->unexpected catch error:badarg->ok end,true=erlang:unalias(A),receive lost->error(unexpected) after 0->ok end"
            );

        int corrected = cases.FindIndex(fixture => fixture.Name == "erlang-alias-admitted-survives-deactivate");
        cases[corrected] = cases[corrected] with { Expected = Term.Tuple(Term.A("error"), Term.A("error"), Term.A("timeout")) };
        Add(
            "pending-explicit-deactivation",
            "A=erlang:alias(),A!lost,true=erlang:unalias(A),receive lost->error(unexpected) after 0->ok end"
        );
        Add(
            "pending-reply-deactivation",
            "A=erlang:alias([reply]),A!lost,true=erlang:unalias(A),receive lost->error(unexpected) after 0->ok end"
        );
        Add(
            "barrier-admitted-survives",
            "A=erlang:alias(),A!kept,self()!barrier,receive barrier->ok after 1000->error(timeout) end,true=erlang:unalias(A),receive kept->ok after 0->error(lost) end"
        );
        Add(
            "ordinary-pending-fifo",
            "A=erlang:alias(),A!first,self()!second,A!third,first=receive M1->M1 after 1000->error(timeout) end,second=receive M2->M2 after 1000->error(timeout) end,third=receive M3->M3 after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "priority-pending-deactivation",
            "A=erlang:alias([priority]),self()!normal,ok=erlang:send(A,lost,[priority]),true=erlang:unalias(A),normal=receive M1->M1 after 1000->error(timeout) end,receive lost->error(unexpected) after 0->ok end"
        );
        Add(
            "reply-pending-ordinary-first",
            "A=erlang:alias([reply]),self()!normal,A!first,A!second,normal=receive M1->M1 after 1000->error(timeout) end,first=receive M2->M2 after 1000->error(timeout) end,false=erlang:unalias(A),receive second->error(unexpected) after 0->ok end"
        );
        Add(
            "pending-distinct-aliases",
            "A=erlang:alias(),B=erlang:alias(),A!lost,B!kept,true=erlang:unalias(A),receive kept->ok after 1000->error(timeout) end,true=erlang:unalias(B),receive lost->error(unexpected) after 0->ok end"
        );
        Add(
            "pending-selective-drop",
            "A=erlang:alias(),A!lost,self()!kept,true=erlang:unalias(A),receive kept->ok after 1000->error(timeout) end,receive lost->error(unexpected) after 0->ok end"
        );
        Add(
            "explicit-yield-admission",
            "A=erlang:alias(),A!kept,true=erlang:yield(),true=erlang:unalias(A),receive kept->ok after 0->error(lost) end"
        );
        Add(
            "reply-waiter-wakeup",
            "A=erlang:alias([reply]),spawn(fun()->A!first,A!second end),receive first->ok after 1000->error(timeout) end,false=erlang:unalias(A),receive second->error(unexpected) after 0->ok end"
        );

        return cases;
    }
}
