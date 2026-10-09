using System.Diagnostics;
using System.Text.Json;
using Erlang;
using Erlang.Compiler;

if (args.Length != 2) { Console.Error.WriteLine("Usage: Erlang.Differential <erl-executable> <report.json>; oracle must be OTP 29.1.1"); return 2; }
string oracle = args[0];
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
    string[] cases = ["1 + 2 * 3", "{1 == 1.0, 1 =:= 1.0}", "{0.0 == -0.0, 0.0 =:= -0.0}", "9007199254740993 > 9007199254740992.0", "{a, [1,2|tail]}", "lists:member(1, [1.0])", "lists:reverse([1,2,3])", "lists:sum([1,2,3])", "[1,2] ++ [3]", "[1,2,1] -- [1]", "-7 div 3", "-7 rem 3", "case {1,1} of {X,X} -> X; _ -> no end", "case a of X when hd(X) == 1; is_atom(X) -> ok end", "receive {hello, X} -> X after 0 -> timeout end", "lists:map(fun(X) -> X * 2 end, [1,2,3])", "'𐀀' > '\uffff'", "{length([1,2]), hd([ok]), tl([1,2])}", "maps:get(a, #{a => 1, a => 42})", "case #{1 => integer, 1.0 => float} of #{1 := A, 1.0 := B} -> {A,B} end", "maps:get(a, #{}#{a => 1, a := 2})", "case #{a => {7,7}} of #{a := {X,X}} -> X end", "case 1 of K -> case #{2 => 42} of #{K + 1 := V} -> V end end", "case #{a => 1} of #{hd(atom) := X} -> wrong; _ -> ok end", "case a of K -> F = fun(#{K := K}) -> K end, F(#{a => 42}) end", "case #{} of M when M#{a := 1} =:= #{} -> wrong; _ -> ok end", "{is_map(#{}),is_map([]),map_size(#{a => 1}),is_map_key(a,#{a => 42}),map_get(a,#{a => 42})}", "map_get(1.0,#{1 => value})", "map_get(key,not_map)", "map_size([])", "is_map_key(key,42)", "case #{} of M when map_get(a,M) =:= 42; map_size(M) =:= 0 -> ok; _ -> no end", "#{}#{missing := 1}", "atom#{a := error(blurf)}", "error(boom)", "throw(reason)", "exit(reason)"];
    var results = new List<object>(); int failed = 0;
    foreach (string source in cases)
    {
        string wrapped = "try (" + source + ") of OracleValue -> {ok,OracleValue} catch OracleClass:OracleReason -> {error,OracleClass,OracleReason} end";
        Term expected = ExternalTermFormat.Decode(Convert.FromBase64String(await RunOracle("io:format(\"~s\",[base64:encode(term_to_binary(" + wrapped + "))]),halt().")));
        await using var runtime = new ProcessRuntime(); Term? actual = null; var expression = new Parser(source).ParseExpression(); Semantics.Validate(expression);
        var process = runtime.Spawn(async ctx =>
        {
            try { actual = Term.Tuple(Term.A("ok"), await Execution.EvaluateAsync(expression, ctx)); }
            catch (ErlangException ex) { actual = Term.Tuple(Term.A("error"), Term.A(ex.ExceptionClass), ex.Reason); }
            return Term.A("ok");
        }); Term reason = await process.Completion;
        bool passed = reason.Equals(Term.A("normal")) && actual is not null && actual.Equals(expected); if (!passed) failed++;
        Console.WriteLine((passed ? "PASS " : "FAIL ") + source); results.Add(new { Source = source, Expected = expected.ToString(), Actual = actual?.ToString(), ExitReason = reason.ToString(), Passed = passed });
    }
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!); await File.WriteAllTextAsync(args[1], JsonSerializer.Serialize(new { Baseline = "OTP-29.1.1", Passed = cases.Length - failed, Failed = failed, Results = results }, new JsonSerializerOptions { WriteIndented = true })); return failed == 0 ? 0 : 1;
}
catch (Exception ex) { Console.Error.WriteLine("Differential oracle unavailable or failed: " + ex.Message); return 2; }
