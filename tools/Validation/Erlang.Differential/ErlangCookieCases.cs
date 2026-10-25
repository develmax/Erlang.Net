namespace Erlang.Differential;

public static class ErlangCookieCases
{
    public static string? FixtureName(string function, int arity) => function switch
    {
        ErlangCookieNames.Get when arity == 0 => "erlang-cookie-default",
        ErlangCookieNames.Get when arity == 1 => "erlang-cookie-own",
        ErlangCookieNames.Set when arity == 1 => "erlang-cookie-set-disabled",
        ErlangCookieNames.Set when arity == 2 => "erlang-cookie-set-remote-disabled",
        _ => null
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-cookie-" + name,
            "-module(cookie_contract). -export([run/0,init/1,handle_call/3,handle_cast/2]). run()->" + body + ",done. " +
            "init(C)->{ok,{C,#{}}}. " +
            "handle_call(get_our_cookie,_,{C,M}=S)->{reply,C,S}; " +
            "handle_call({get_cookie,N},_,{C,M}=S)->{reply,maps:get(N,M,C),S}; " +
            "handle_call({set_our_cookie,C},_,{_,M})->{reply,true,{C,M}}; " +
            "handle_call({set_cookie,N,C},_,{Own,M})->{reply,true,{Own,M#{N=>C}}}; " +
            "handle_call(stop,_,S)->{stop,normal,stopped,S}. handle_cast(_,S)->{noreply,S}.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add("default", "nocookie=erlang:get_cookie()");
        Add("own", "nocookie=erlang:get_cookie(node())");
        Add("remote-disabled", "nocookie=erlang:get_cookie('peer@invalid')");
        Add("arbitrary-atom", "nocookie=erlang:get_cookie(arbitrary)");
        Add("set-disabled", "try erlang:set_cookie(test_cookie) of _->error(accepted) catch error:distribution_not_started->ok end");
        Add(
            "set-remote-disabled",
            "try erlang:set_cookie('peer@invalid',test_cookie) of _->error(accepted) catch error:distribution_not_started->ok end"
        );
        Add(
            "set-arbitrary-disabled",
            "try erlang:set_cookie(arbitrary,test_cookie) of _->error(accepted) catch error:distribution_not_started->ok end"
        );
        Add("nonode-rejected", "try erlang:set_cookie(node(),test_cookie) of _->error(accepted) catch error:badarg->ok end");
        foreach (string value in new[] { "42", "[]", "[cookie]", "{cookie}", "self()", "make_ref()", "<<1>>", "fun()->ok end" })
        {
            Add("bad-get-" + cases.Count, "try erlang:get_cookie(" + value + ") of _->error(accepted) catch error:badarg->ok end");
            Add("bad-set-" + cases.Count, "try erlang:set_cookie(" + value + ") of _->error(accepted) catch error:badarg->ok end");
            Add("bad-node-" + cases.Count, "try erlang:set_cookie(" + value + ",cookie) of _->error(accepted) catch error:badarg->ok end");
            Add(
                "bad-remote-cookie-" + cases.Count,
                "try erlang:set_cookie('peer@invalid'," + value + ") of _->error(accepted) catch error:badarg->ok end"
            );
        }
        Add(
            "badarg-info",
            "try erlang:get_cookie(42) catch error:badarg:S->[{erlang,get_cookie,[42],L}|_]=S,{error_info,#{module:=erl_erts_errors}}=lists:keyfind(error_info,1,L) end"
        );
        const string start = "undefined=whereis(auth),{ok,P}=gen_server:start_link({local,auth},cookie_contract,initial_cookie,[]),Ref=monitor(process,P),";
        const string stop = ",stopped=gen_server:call(P,stop),receive {'DOWN',Ref,process,P,normal}->ok after 1000->error(auth_not_stopped) end,nocookie=erlang:get_cookie()";
        Add("service-get-own", start + "initial_cookie=erlang:get_cookie(),initial_cookie=erlang:get_cookie(node())" + stop);
        Add("service-default-remote", start + "initial_cookie=erlang:get_cookie('peer@invalid')" + stop);
        Add(
            "service-set-own",
            start + "true=erlang:set_cookie(next_cookie),next_cookie=erlang:get_cookie(),next_cookie=erlang:get_cookie('peer@invalid')" + stop
        );
        Add(
            "service-remote-override",
            start + "true=erlang:set_cookie('peer@invalid',peer_cookie),peer_cookie=erlang:get_cookie('peer@invalid'),initial_cookie=erlang:get_cookie(),true=erlang:set_cookie(next_cookie),peer_cookie=erlang:get_cookie('peer@invalid'),next_cookie=erlang:get_cookie(other)" + stop
        );
        Add(
            "service-nonode-still-rejected",
            start + "try erlang:set_cookie(node(),cookie) of _->error(accepted) catch error:badarg->ok end,initial_cookie=erlang:get_cookie()" + stop
        );

        return cases;
    }
}
