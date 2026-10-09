using System.Diagnostics;
using System.Text.Json;
using Erlang;
using Erlang.Compiler;
using Erlang.Differential;

if (args.Length != 2) { Console.Error.WriteLine("Usage: Erlang.Differential <erl-executable> <report.json>; oracle must be OTP 29.1.1"); return 2; }
string oracle = args[0];
var results = new List<object>(); int failed = 0, planned = 0;
string? activeSource = null; bool versionVerified = false;
async Task SaveReport(bool complete, string? infrastructureError = null)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
    await File.WriteAllTextAsync(args[1], JsonSerializer.Serialize(new
    {
        Baseline = "OTP-29.1.1", VersionVerified = versionVerified, Complete = complete,
        Planned = planned, Executed = results.Count, Passed = results.Count - failed, Failed = failed,
        AbortedSource = infrastructureError is null ? null : activeSource, InfrastructureError = infrastructureError, Results = results
    }, new JsonSerializerOptions { WriteIndented = true }));
}
async Task<string> RunOracle(string expression)
{
    var info = new ProcessStartInfo(oracle) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
    foreach (string argument in new[] { "-noshell", "-noinput", "-eval", expression }) info.ArgumentList.Add(argument);
    using var process = Process.Start(info)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
    try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15)); } catch (TimeoutException) { process.Kill(true); throw; }
    string stdout = await output, stderr = await error; if (process.ExitCode != 0) throw new InvalidOperationException($"Oracle exited {process.ExitCode}: {stderr} {stdout}"); return stdout.Trim();
}
try
{
    string version = await RunOracle("{ok,V}=file:read_file(filename:join([code:root_dir(),\"releases\",erlang:system_info(otp_release),\"OTP_VERSION\"])), io:format(\"~s\",[V]), halt().");
    if (version != "29.1.1") throw new InvalidOperationException("Oracle version mismatch: " + version);
    versionVerified = true;
    string[] cases = ["1 + 2 * 3", "{1 == 1.0, 1 =:= 1.0}", "{0.0 == -0.0, 0.0 =:= -0.0}", "9007199254740993 > 9007199254740992.0", "{a, [1,2|tail]}", "lists:member(1, [1.0])", "lists:reverse([1,2,3])", "lists:sum([1,2,3])", "[1,2] ++ [3]", "[1,2,1] -- [1]", "-7 div 3", "-7 rem 3", "case {1,1} of {X,X} -> X; _ -> no end", "case a of X when hd(X) == 1; is_atom(X) -> ok end", "receive {hello, X} -> X after 0 -> timeout end", "lists:map(fun(X) -> X * 2 end, [1,2,3])", "'𐀀' > '\ufffd'", "{length([1,2]), hd([ok]), tl([1,2])}", "maps:get(a, #{a => 1, a => 42})", "case #{1 => integer, 1.0 => float} of #{1 := A, 1.0 := B} -> {A,B} end", "maps:get(a, #{}#{a => 1, a := 2})", "case #{a => {7,7}} of #{a := {X,X}} -> X end", "case 1 of K -> case #{2 => 42} of #{K + 1 := V} -> V end end", "case #{a => 1} of #{hd(atom) := X} -> wrong; _ -> ok end", "case a of K -> F = fun(#{K := K}) -> K end, F(#{a => 42}) end", "case #{} of M when M#{a := 1} =:= #{} -> wrong; _ -> ok end", "{is_map(#{}),is_map([]),map_size(#{a => 1}),is_map_key(a,#{a => 42}),map_get(a,#{a => 42})}", "map_get(1.0,#{1 => value})", "map_get(key,not_map)", "map_size([])", "is_map_key(key,42)", "case #{} of M when map_get(a,M) =:= 42; map_size(M) =:= 0 -> ok; _ -> no end", "#{}#{missing := 1}", "atom#{a := error(blurf)}", "error(boom)", "throw(reason)", "exit(reason)", "<<>>", "<<1,2,255>>", "<<511,-1,16:4,31:4>>", "<<5:3,17:5,3:2>>", "<<4660:16/big>>", "<<4660:16/little>>", "<<291:12/little>>", "<<4660:2/unit:8>>", "<<-1:16/signed-little>>", "case 4 of S -> <<(2+3):(S+1)>> end", "<<(<<1,2,3>>):2/binary>>", "<<1:1,(<<2:2>>)/bitstring,3:2>>", "<<(<<1:1>>)/binary-unit:1>>", "<<-123:0,42:8>>", "<<atom>>", "<<1:-1>>", "<<(<<1>>):2/binary>>", "<<(<<1:1>>)/binary>>", "<<(<<1,2>>):all/binary>>", "<<1:8/integer-integer-big-big-unit:1-unit:1>>", "{bit_size(<<>>),byte_size(<<>>),bit_size(<<1:1>>),byte_size(<<1:1>>),byte_size(<<1:9>>)}", "{is_bitstring(<<>>),is_binary(<<>>),is_bitstring(<<1:1>>),is_binary(<<1:1>>),is_bitstring([])}", "bit_size(atom)", "byte_size([1,2])", "case atom of X when bit_size(X) =:= 0; byte_size(X) =:= 0; is_atom(X) -> ok end"];
    cases = [..cases,
        "case <<42>> of <<X>> -> X end",
        "case <<255>> of <<X:8/signed>> -> X end",
        "case <<255>> of <<X:8/unsigned>> -> X end",
        "case <<-257:16/little>> of <<X:16/signed-little>> -> X end",
        "case <<291:12/little>> of <<X:12/little>> -> X end",
        "case <<4660:16/native>> of <<X:16/native>> -> X end",
        "case <<3,5:3,2:2>> of <<N,X:N,Rest/bitstring>> -> {X,Rest} end",
        "case 3 of N -> case <<17:5>> of <<X:(N+2)>> -> X end end",
        "case 8 of L -> F = fun(<<L:L,B:L>>) -> B end, F(<<16:8,7:16>>) end",
        "case 8 of L -> F = fun(<<L:L,B:L,L:L>>) -> B; (_) -> no end, F(<<16:8,7:16,16:16>>) end",
        "case <<1,2>> of <<X,X>> -> wrong; _ -> ok end",
        "case 42 of X -> case <<42>> of <<X>> -> ok; _ -> no end end",
        "case <<1:7>> of <<X:8>> -> wrong; _ -> ok end",
        "case <<1,2>> of <<X:8>> -> wrong; _ -> ok end",
        "case atom of <<X:8>> -> wrong; _ -> ok end",
        "case <<>> of <<X:0/signed>> -> X end",
        "case <<1,2,3>> of <<B:2/binary,T/binary>> -> {B,T} end",
        "case <<1:1,5:3>> of <<_:1,B:3/bitstring>> -> B end",
        "case <<1,2>> of <<X:2/unit:8>> -> X end",
        "case <<1:1>> of <<B/binary>> -> wrong; _ -> ok end",
        "case -1 of S -> case <<1>> of <<X:S>> -> wrong; _ -> ok end end",
        "case atom of S -> case <<1>> of <<X:S>> -> wrong; _ -> ok end end",
        "case <<\"OK\",42>> of <<\"OK\",X>> -> X end",
        "case <<0>> of <<256>> -> wrong; _ -> ok end",
        "case <<255>> of <<-1:8/signed>> -> ok; _ -> no end",
        "case <<>> of <<>> -> ok; _ -> no end",
        "case #{a => <<3,5:3>>} of #{a := <<N,X:N>>} -> X end",
        "<<X:16>> = <<1>>"];
    planned = cases.Length;
    foreach (string source in cases)
    {
        activeSource = source;
        string wrapped = "try (" + source + ") of OracleValue -> {ok,OracleValue} catch OracleClass:OracleReason -> {error,OracleClass,OracleReason} end";
        Term expected = ExternalTermFormat.Decode(Convert.FromBase64String(await RunOracle(OracleProtocol.EvaluationCommand(wrapped))));
        await using var runtime = new ProcessRuntime(); Term? actual = null; var expression = new Parser(source).ParseExpression(); Semantics.Validate(expression);
        var process = runtime.Spawn(async ctx =>
        {
            try { actual = Term.Tuple(Term.A("ok"), await Execution.EvaluateAsync(expression, ctx)); }
            catch (ErlangException ex) { actual = Term.Tuple(Term.A("error"), Term.A(ex.ExceptionClass), ex.Reason); }
            return Term.A("ok");
        }); Term reason = await process.Completion;
        bool passed = reason.Equals(Term.A("normal")) && actual is not null && actual.Equals(expected); if (!passed) failed++;
        Console.WriteLine((passed ? "PASS " : "FAIL ") + source); results.Add(new { Source = source, Expected = expected.ToString(), Actual = actual?.ToString(), ExitReason = reason.ToString(), Passed = passed });
        await SaveReport(false);
    }
    await SaveReport(true); return failed == 0 ? 0 : 1;
}
catch (Exception ex) { await SaveReport(false, ex.Message); Console.Error.WriteLine("Differential oracle unavailable or failed: " + ex.Message); return 2; }
