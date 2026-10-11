namespace Erlang.Differential;

public static class ErlangClockCases
{
    public static string? FixtureName(string function, int arity)
    {
        string name = "erlang-clock-export-" + function + "-" + arity;

        return Modules.Any(fixture => fixture.Name == name) ? name : null;
    }

    private static CompiledModuleCase Case(string name, string body) => new(
        "erlang-clock-" + name,
        "-module(oracle_erlang_clock). -export([run/0]). run()->" + body + ",done.",
        Term.Tuple(Term.A("ok"), Term.A("done"))
    );

    private static string Error(string call) => "try erlang:" + call + " of _->unexpected catch error:badarg->ok end";

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        cases.Add(Case(
            "export-send_after-4",
            "R=erlang:send_after(0,self(),sent,[{abs,false}]),receive sent->ok after 1000->error(missing) end,false=erlang:read_timer(R)"
        ));
        cases.Add(Case(
            "export-start_timer-4",
            "R=erlang:start_timer(0,self(),sent,[{abs,false}]),receive {timeout,R,sent}->ok after 1000->error(missing) end,false=erlang:read_timer(R)"
        ));
        cases.Add(Case(
            "export-read_timer-2",
            "R=erlang:send_after(10000,self(),sent),ok=erlang:read_timer(R,[{async,true}]),receive {read_timer,R,N}->true=is_integer(N),true=N>0 after 1000->error(missing) end,true=is_integer(erlang:cancel_timer(R))"
        ));
        cases.Add(Case(
            "export-cancel_timer-2",
            "R=erlang:send_after(10000,self(),sent),ok=erlang:cancel_timer(R,[{async,true}]),receive {cancel_timer,R,N}->true=is_integer(N),true=N>0 after 1000->error(missing) end,false=erlang:read_timer(R)"
        ));
        cases.Add(Case(
            "timer-absolute-future",
            "N=erlang:monotonic_time(millisecond),R=erlang:send_after(N+10000,self(),sent,[{abs,true}]),T=erlang:read_timer(R),true=is_integer(T),true=T>0,true=T=<10000,true=is_integer(erlang:cancel_timer(R))"
        ));
        cases.Add(Case(
            "timer-absolute-past",
            "N=erlang:monotonic_time(millisecond),receive after 30->ok end,R=erlang:start_timer(N,self(),sent,[{abs,true}]),receive {timeout,R,sent}->ok after 1000->error(missing) end"
        ));
        cases.Add(Case(
            "timer-info-false",
            "R=erlang:send_after(10000,self(),sent),ok=erlang:cancel_timer(R,[{info,false}]),ok=erlang:cancel_timer(R,[{info,false}]),false=erlang:read_timer(R)"
        ));
        cases.Add(Case(
            "timer-async-silent",
            "R=erlang:send_after(10000,self(),sent),ok=erlang:cancel_timer(R,[{async,true},{info,false}]),receive {cancel_timer,R,_}->error(unexpected) after 30->ok end,false=erlang:read_timer(R)"
        ));
        cases.Add(Case(
            "timer-async-missing",
            "R=erlang:make_ref(),ok=erlang:read_timer(R,[{async,true}]),receive {read_timer,R,false}->ok after 1000->error(missing) end,ok=erlang:cancel_timer(R,[{async,true}]),receive {cancel_timer,R,false}->ok after 1000->error(missing) end"
        ));
        cases.Add(Case(
            "timer-last-options",
            "R=erlang:send_after(10000,self(),sent,[{abs,true},{abs,false}]),ok=erlang:cancel_timer(R,[{async,true},{async,false},{info,true},{info,false}]),receive {cancel_timer,R,_}->error(unexpected) after 30->ok end"
        ));
        foreach (string duration in new[] { "-1", "9223372036854775807", "9223372036001", "1.0", "bad" })
            cases.Add(Case(
                "timer-relative-error-" + cases.Count,
                Error("send_after(" + duration + ",self(),sent)") + "," + Error("start_timer(" + duration + ",self(),sent,[])")
            ));
        foreach (string options in new[] { "bad", "[{abs,bad}]", "[{abs,true}|bad]", "[{async,true}]", "[abs]", "[{abs}]" })
            cases.Add(Case("timer-start-options-error-" + cases.Count, Error("send_after(1,self(),sent," + options + ")")));
        foreach (string options in new[] { "bad", "[{async,bad}]", "[{async,true}|bad]", "[{info,false}]", "[{abs,true}]" })
            cases.Add(Case("timer-read-options-error-" + cases.Count, Error("read_timer(erlang:make_ref()," + options + ")")));
        foreach (string options in new[] { "bad", "[{info,bad}]", "[{info,true}|bad]", "[{abs,true}]" })
            cases.Add(Case("timer-cancel-options-error-" + cases.Count, Error("cancel_timer(erlang:make_ref()," + options + ")")));
        cases.Add(Case(
            "export-convert_time_unit-3",
            "-1=erlang:convert_time_unit(-1,millisecond,second),0=erlang:convert_time_unit(1,millisecond,second),-2=erlang:convert_time_unit(-1001,millisecond,second),1500=erlang:convert_time_unit(3,2,1000)"
        ));
        foreach (string clock in new[] { "monotonic_time", "system_time", "time_offset" })
        {
            cases.Add(Case("export-" + clock + "-0", "true=is_integer(erlang:" + clock + "())"));
            cases.Add(Case("export-" + clock + "-1", "true=is_integer(erlang:" + clock + "(millisecond))"));
        }
        cases.Add(Case(
            "export-timestamp-0",
            "{M,S,U}=erlang:timestamp(),true=is_integer(M),true=M>0,true=S>=0,true=S<1000000,true=U>=0,true=U<1000000"
        ));
        cases.Add(Case("export-now-0", "A=erlang:now(),B=erlang:now(),true=A<B"));
        cases.Add(Case("export-unique_integer-0", "A=erlang:unique_integer(),B=erlang:unique_integer(),true=is_integer(A),true=A=/=B"));
        cases.Add(Case(
            "export-unique_integer-1",
            "A=erlang:unique_integer([positive,monotonic]),B=erlang:unique_integer([monotonic,positive]),true=A>0,true=A<B"
        ));
        foreach (string unit in new[] { "second", "seconds", "millisecond", "milli_seconds", "microsecond", "micro_seconds", "nanosecond", "nano_seconds", "native", "perf_counter", "1", "1000", "100000000000000000000000000000000000000000000000000000000000" })
        {
            cases.Add(Case(
                "units-" + cases.Count,
                "-42=erlang:convert_time_unit(-42," + unit + "," + unit + "),42=erlang:convert_time_unit(42," + unit + "," + unit + "),true=is_integer(erlang:monotonic_time(" + unit + ")),true=is_integer(erlang:system_time(" + unit + ")),true=is_integer(erlang:time_offset(" + unit + "))"
            ));
        }
        cases.Add(Case(
            "monotonic-bracket",
            "A=erlang:monotonic_time(),B=erlang:monotonic_time(millisecond),C=erlang:monotonic_time(),true=A=<C,true=erlang:convert_time_unit(A,native,millisecond)=<B,true=B=<erlang:convert_time_unit(C,native,millisecond)"
        ));
        cases.Add(Case(
            "system-offset-bracket",
            "A=erlang:monotonic_time(),O=erlang:time_offset(),S=erlang:system_time(),B=erlang:monotonic_time(),true=A+O=<S,true=S=<B+O"
        ));
        cases.Add(Case(
            "offset-unit-conversion",
            "O=erlang:time_offset(),M=erlang:time_offset(millisecond),M=erlang:convert_time_unit(O,native,millisecond)"
        ));
        cases.Add(Case(
            "timestamp-bracket",
            "A=erlang:system_time(microsecond),{M,S,U}=erlang:timestamp(),B=erlang:system_time(microsecond),T=M*1000000000000+S*1000000+U,true=A=<T,true=T=<B"
        ));
        foreach (string input in new[] { "bad", "1.0", "[]" })
            cases.Add(Case("conversion-type-" + cases.Count, Error("convert_time_unit(" + input + ",second,millisecond)")));
        foreach (string unit in new[] { "bad", "0", "-1", "1.0", "[]", "{}" })
            cases.Add(Case(
                "unit-error-" + cases.Count,
                Error("convert_time_unit(1," + unit + ",second)") + "," + Error("convert_time_unit(1,second," + unit + ")") + "," + Error("monotonic_time(" + unit + ")") + "," + Error("system_time(" + unit + ")") + "," + Error("time_offset(" + unit + ")")
            ));
        foreach (string options in new[] { "[]", "[positive]", "[monotonic]", "[positive,monotonic]", "[positive,positive,monotonic,monotonic]" })
            cases.Add(Case(
                "unique-modifiers-" + cases.Count,
                "L=lists:map(fun(_)->erlang:unique_integer(" + options + ") end,lists:seq(1,1000)),1000=length(lists:usort(L)),true=lists:all(fun(X)->is_integer(X) end,L)"
            ));
        foreach (string options in new[] { "bad", "[bad]", "[positive|bad]", "[1]", "[{}]" })
            cases.Add(Case("unique-bad-" + cases.Count, Error("unique_integer(" + options + ")")));
        cases.Add(Case(
            "big-unit-floor",
            "N=100000000000000000000000000000000000000000000000000000000000,-1=erlang:convert_time_unit(-1,N,1),N=erlang:convert_time_unit(1,1,N),-2=erlang:convert_time_unit(-N-1,N,1)"
        ));
        foreach (string operation in new[] { "date", "time" })
            cases.Add(Case("export-" + operation + "-0", "{A,B,C}=erlang:" + operation + "(),true=is_integer(A),true=is_integer(B),true=is_integer(C)"));
        foreach (string operation in new[] { "localtime", "universaltime" })
            cases.Add(Case(
                "export-" + operation + "-0",
                "{{Y,M,D},{H,I,S}}=erlang:" + operation + "(),true=Y>=1902,true=M>=1,true=M=<12,true=D>=1,true=D=<31,true=H>=0,true=H=<23,true=I>=0,true=I=<59,true=S>=0,true=S=<59"
            ));
        cases.Add(Case(
            "export-universaltime_to_posixtime-1",
            "0=erlang:universaltime_to_posixtime({{1970,1,1},{0,0,0}}),-1=erlang:universaltime_to_posixtime({{1969,12,31},{23,59,59}})"
        ));
        cases.Add(Case(
            "export-posixtime_to_universaltime-1",
            "{{1970,1,1},{0,0,0}}=erlang:posixtime_to_universaltime(0),{{1969,12,31},{23,59,59}}=erlang:posixtime_to_universaltime(-1)"
        ));
        cases.Add(Case(
            "export-universaltime_to_localtime-1",
            "U={{2020,1,15},{12,0,0}},L=erlang:universaltime_to_localtime(U),U=erlang:localtime_to_universaltime(L)"
        ));
        cases.Add(Case(
            "export-localtime_to_universaltime-1",
            "L={{2020,1,15},{12,0,0}},U=erlang:localtime_to_universaltime(L),L=erlang:universaltime_to_localtime(U)"
        ));
        cases.Add(Case(
            "export-localtime_to_universaltime-2",
            "L={{2020,1,15},{12,0,0}},U=erlang:localtime_to_universaltime(L),U=erlang:localtime_to_universaltime(L,undefined)"
        ));
        foreach (string date in new[] { "{{1600,1,1},{0,0,0}}", "{{1900,3,1},{0,0,0}}", "{{2000,2,29},{23,59,59}}", "{{2400,2,29},{12,30,59}}", "{{10000,1,1},{0,0,0}}", "{{2147483646,12,31},{23,59,59}}" })
            cases.Add(Case("civil-roundtrip-" + cases.Count, "D=" + date + ",D=erlang:posixtime_to_universaltime(erlang:universaltime_to_posixtime(D))"));
        foreach (string date in new[] { "{{1599,12,31},{23,59,59}}", "{{2147483647,1,1},{0,0,0}}", "{{2000,0,1},{0,0,0}}", "{{2000,13,1},{0,0,0}}", "{{2000,2,30},{0,0,0}}", "{{1900,2,29},{0,0,0}}", "{{2000,1,0},{0,0,0}}", "{{2000,1,1},{24,0,0}}", "{{2000,1,1},{0,60,0}}", "{{2000,1,1},{0,0,60}}", "{{2000,1,1},{0,-1,0}}", "{{2000.0,1,1},{0,0,0}}", "bad", "{{2000,1,1},bad}" })
            cases.Add(Case("civil-bad-" + cases.Count, Error("universaltime_to_posixtime(" + date + ")")));
        foreach (string date in new[] { "{{1901,12,31},{23,59,59}}", "{{2020,2,30},{0,0,0}}", "bad" })
            cases.Add(Case(
                "timezone-bad-" + cases.Count,
                Error("universaltime_to_localtime(" + date + ")") + "," + Error("localtime_to_universaltime(" + date + ")")
            ));
        foreach (string seconds in new[] { "bad", "1.0", "9223372036854775808", "-9223372036854775809" })
            cases.Add(Case("epoch-bad-" + cases.Count, Error("posixtime_to_universaltime(" + seconds + ")")));
        cases.Add(Case("dst-option-bad", Error("localtime_to_universaltime({{2020,1,1},{0,0,0}},bad)")));

        return cases;
    }
}
