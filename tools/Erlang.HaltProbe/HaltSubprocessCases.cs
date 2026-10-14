using System.Security.Cryptography;
using System.Text;

namespace Erlang.HaltProbe;

public static class HaltSubprocessCases
{
    public static IReadOnlyList<HaltSubprocessContract> NumericAndErrors { get; } = Array.AsReadOnly(new[]
    {
        Halt(
            "halt-zero",
            "erlang:halt()",
            0,
            0
        ),
        Halt(
            "halt-one-status",
            "erlang:halt(41)",
            41,
            41
        ),
        Halt(
            "halt-two-flush-false",
            "erlang:halt(43,[{flush,false}])",
            43,
            43
        ),
        Halt(
            "halt-low-word-big",
            "erlang:halt(18446744073709551663)",
            47,
            47
        ),
        Halt(
            "halt-low31-wrap",
            "erlang:halt(2147483648)",
            0,
            0
        ),
        Halt(
            "halt-os-width",
            "erlang:halt(257)",
            257,
            1
        ),
        Halt(
            "halt-last-options-uint32",
            "erlang:halt(41,[{flush,false},{flush,true},{flush_timeout,0},{flush_timeout,4294967295}])",
            41,
            41
        ),
        Halt(
            "halt-catch-after",
            "try erlang:halt(41) catch _:_ -> io:put_chars(\"CATCH\\n\") after io:put_chars(\"AFTER\\n\") end",
            41,
            41
        ),
        Halt(
            "halt-generator",
            "[begin erlang:halt(43), io:put_chars(\"CONTINUE\\n\") end || _ <- [ok]]",
            43,
            43
        ),
        Halt(
            "halt-both-streams",
            "io:put_chars(standard_error,\"ERROR-BEFORE\\n\"), io:put_chars(\"BEFORE\\n\"), erlang:halt(41)",
            41,
            41,
            "BEFORE\n",
            "ERROR-BEFORE\n"
        ),
        Error(
            "halt-options-first",
            "try erlang:halt(-1,broken),error(unexpected_halt_return) catch error:badarg:S -> [{erlang,halt,[-1,broken],I}|_]=S,{error_info,#{module:=erl_erts_errors,cause:=badopt}}=lists:keyfind(error_info,1,I) end"
        ),
        Error(
            "halt-improper-options",
            "try erlang:halt(0,[{flush,true}|broken]),error(unexpected_halt_return) catch error:badarg:S -> [{erlang,halt,[0,[{flush,true}|broken]],I}|_]=S,{error_info,#{cause:=badopt}}=lists:keyfind(error_info,1,I) end"
        ),
        Error(
            "halt-infinity-invalid",
            "try erlang:halt(0,[{flush_timeout,infinity}]),error(unexpected_halt_return) catch error:badarg:S -> [{erlang,halt,_,I}|_]=S,{error_info,#{cause:=badopt}}=lists:keyfind(error_info,1,I) end"
        ),
        Halt(
            "halt-uint32-overflow",
            "erlang:halt(0,[{flush_timeout,4294967296}])",
            255,
            255
        ),
        Error(
            "halt-one-error-wrapper",
            "try erlang:halt(-1),error(unexpected_halt_return) catch error:badarg:S -> [{erlang,halt,[-1],I}|_]=S,{error_info,#{module:=erl_erts_errors}=M}=lists:keyfind(error_info,1,I),false=maps:is_key(cause,M) end"
        ),
        Error(
            "halt-two-plain-status",
            "try erlang:halt(-1,[]),error(unexpected_halt_return) catch error:badarg:S -> [{erlang,halt,[-1,[]],I}|_]=S,{error_info,#{module:=erl_erts_errors}=M}=lists:keyfind(error_info,1,I),false=maps:is_key(cause,M) end"
        ),
        Error(
            "halt-negative-big-status",
            "try erlang:halt(-18446744073709551663,[]),error(unexpected_halt_return) catch error:badarg:S -> [{erlang,halt,[-18446744073709551663,[]],I}|_]=S,{error_info,#{module:=erl_erts_errors}=M}=lists:keyfind(error_info,1,I),false=maps:is_key(cause,M) end"
        ),
        Error(
            "halt-valid-abort-invalid-options-first",
            "try erlang:halt(abort,[{flush,wrong}]),error(unexpected_halt_return) catch error:badarg:S -> [{erlang,halt,_,I}|_]=S,{error_info,#{cause:=badopt}}=lists:keyfind(error_info,1,I) end"
        ),
        Error(
            "halt-invalid-slogan-prefix",
            "try erlang:halt([16#D800],[]),error(unexpected_halt_return) catch error:badarg:S -> [{erlang,halt,_,I}|_]=S,{error_info,#{module:=erl_erts_errors}=M}=lists:keyfind(error_info,1,I),false=maps:is_key(cause,M) end"
        ),
        Halt(
            "halt-direct-zero-watchdog",
            "erlang:halt(41,[{flush_timeout,0}])",
            255,
            255
        ),
        Halt(
            "halt-maximum-native-small-timeout",
            "erlang:halt(41,[{flush_timeout,576460752303423487}])",
            41,
            41
        ),
        Error(
            "halt-native-small-boundary-rejected",
            "try erlang:halt(0,[{flush_timeout,576460752303423488}]),error(unexpected_halt_return) catch error:badarg:S -> [{erlang,halt,[0,[{flush_timeout,576460752303423488}]],I}|_]=S,{error_info,#{module:=erl_erts_errors,cause:=badopt}}=lists:keyfind(error_info,1,I) end"
        ),
        Halt(
            "halt-wrapped-one-flush-false",
            "erlang:halt(43,[{flush_timeout,4294967297},{flush,false}])",
            43,
            43
        )
    });

    public static HaltSubprocessContract Abort { get; } = Create(
        "halt-actual-abort",
        "erlang:halt(abort)",
        HaltProcessObservation.HaltRequest,
        RuntimeHaltKind.Abort,
        null,
        134,
        3,
        "",
        null,
        false,
        "Raw abort stderr/reporting differs by CRT; Windows report-fault UI disabled explicitly"
    );

    public static HaltSubprocessContract CrashDump { get; } = Create(
        "halt-real-clr-dump",
        "erlang:halt(\"CLR-DUMP\",[{flush,false}])",
        HaltProcessObservation.HaltRequest,
        RuntimeHaltKind.CrashDump,
        1,
        1,
        1,
        "",
        null,
        false,
        HaltProbeNames.DumpFormatGap
    );

    public static HaltSubprocessContract Direct(string module, string function, int arity)
    {
        if (module != ErlangModuleNames.Module || function != "halt" || arity is < 0 or > 2)
            throw new ArgumentException(module + ":" + function + "/" + arity);

        return NumericAndErrors[arity];
    }

    public static IReadOnlyList<HaltSubprocessContract> Adapters { get; } = Array.AsReadOnly(new[]
    {
        WithUnit(
            Halt(
                "halt-module-chain",
                "halt_probe_callee:stop()",
                47,
                47
            ),
            "halt_probe_callee",
            "-module(halt_probe_callee).\n-export([stop/0]).\nstop()->erlang:halt(47).\n"
        ),
        WithUnit(
            Halt(
                "halt-server-init",
                "gen_server:start_link(halt_probe_server,[],[])",
                41,
                41
            ),
            "halt_probe_server",
            "-module(halt_probe_server).\n-behaviour(gen_server).\n-export([init/1,handle_call/3,handle_cast/2,handle_info/2,terminate/2,code_change/3]).\ninit(_)->erlang:halt(41).\nhandle_call(_,_,S)->{reply,ok,S}.\nhandle_cast(_,S)->{noreply,S}.\nhandle_info(_,S)->{noreply,S}.\nterminate(_,_) -> io:put_chars(\"TERMINATE\\n\").\ncode_change(_,S,_)->{ok,S}.\n"
        ),
        WithUnit(
            Halt(
                "halt-supervisor-init",
                "supervisor:start_link(halt_probe_supervisor,[])",
                43,
                43
            ),
            "halt_probe_supervisor",
            "-module(halt_probe_supervisor).\n-behaviour(supervisor).\n-export([init/1]).\ninit(_)->erlang:halt(43).\n"
        )
    });

    private static HaltSubprocessContract WithUnit(HaltSubprocessContract contract, string module, string source)
    {
        var unit = new HaltSourceUnit(module, source, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))));

        return contract with { Sources = Array.AsReadOnly(new[] { unit }.Concat(contract.Sources).ToArray()), ForbiddenMarkers = Array.AsReadOnly(contract.ForbiddenMarkers.Concat(new[] { "TERMINATE" }).ToArray()) };
    }

    private static HaltSubprocessContract Halt(
        string name,
        string body,
        int logical,
        int posix,
        string output = "",
        string error = ""
    ) => Create(
        name,
        body,
        HaltProcessObservation.HaltRequest,
        RuntimeHaltKind.Numeric,
        logical,
        posix,
        logical,
        output,
        error,
        true
    );

    private static HaltSubprocessContract Error(string name, string body) => Create(
        name,
        body + ",io:put_chars(\"DONE\\n\")",
        HaltProcessObservation.EntryExit,
        null,
        null,
        0,
        0,
        "DONE\n",
        "",
        true,
        continuation: false
    );

    private static HaltSubprocessContract Create(
        string name,
        string body,
        HaltProcessObservation observation,
        RuntimeHaltKind? kind,
        int? logical,
        int posix,
        int windows,
        string output,
        string? error,
        bool comparable,
        string? gap = null,
        bool continuation = true
    )
    {
        string module = name.Replace('-', '_');
        string source = "-module(" + module + ").\n-export([run/0]).\nrun()->" + body + (continuation ? ",io:put_chars(\"CONTINUE\\n\").\n" : ".\n");
        var unit = new HaltSourceUnit(module, source, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))));

        return new(
            name,
            Array.AsReadOnly(new[] { unit }),
            module,
            HaltProbeNames.DefaultEntry,
            observation,
            kind,
            logical,
            posix,
            windows,
            Array.AsReadOnly(Encoding.UTF8.GetBytes(output)),
            error is null ? null : Array.AsReadOnly(Encoding.UTF8.GetBytes(error)),
            error is null ? null : Array.AsReadOnly(Encoding.UTF8.GetBytes(error)),
            Array.AsReadOnly(new[] { "CONTINUE", "CATCH", "AFTER" }),
            comparable,
            gap
        );
    }
}
