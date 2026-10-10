using System.Diagnostics;
using System.Text.Json;
using Erlang;
using Erlang.Compiler;
using Erlang.Differential;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: Erlang.Differential <erl-executable> <report.json>; oracle must be OTP 29.1.1");

    return 2;
}
string oracle = args[0];
var results = new List<object>();
var moduleResults = new List<object>();
var diagnosticResults = new List<object>();
int moduleFailed = 0;
int diagnosticFailed = 0;
int failed = 0, planned = 0;
string? activeSource = null;
bool versionVerified = false;
async Task SaveReport(bool complete, string? infrastructureError = null)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
    await File.WriteAllTextAsync(
        args[1],
        JsonSerializer.Serialize(
            new
            {
                Baseline = "OTP-29.1.1",
                VersionVerified = versionVerified,
                Complete = complete,
                Planned = planned + CompiledModuleCases.All.Count + CompiledDiagnosticCases.All.Count,
                Executed = results.Count + moduleResults.Count + diagnosticResults.Count,
                Passed = results.Count + moduleResults.Count + diagnosticResults.Count - failed,
                Failed = failed,
                AbortedSource = infrastructureError is null ? null : activeSource,
                InfrastructureError = infrastructureError,
                ExpressionTrack = new
                {
                    Mode = OracleProtocol.EvaluationMode,
                    Planned = planned,
                    Executed = results.Count,
                    Passed = results.Count - (failed - moduleFailed - diagnosticFailed),
                    Failed = failed - moduleFailed - diagnosticFailed
                },
                CompiledModuleTrack = new
                {
                    ReferenceMode = GeneratedModuleMetadata.ReferenceMode,
                    ImplementationMode = GeneratedModuleMetadata.ImplementationMode,
                    Planned = CompiledModuleCases.All.Count,
                    Executed = moduleResults.Count,
                    Passed = moduleResults.Count - moduleFailed,
                    Failed = moduleFailed
                },
                CompilerDiagnosticTrack = new
                {
                    ReferenceMode = CompiledDiagnosticExpectations.ReferenceMode,
                    Planned = CompiledDiagnosticCases.All.Count,
                    Executed = diagnosticResults.Count,
                    Passed = diagnosticResults.Count - diagnosticFailed,
                    Failed = diagnosticFailed
                },
                DiagnosticResults = diagnosticResults,
                ModuleResults = moduleResults,
                Results = results
            },
            new JsonSerializerOptions { WriteIndented = true }
        )
    );
}
async Task<string> RunOracle(string expression)
{
    var info = new ProcessStartInfo(oracle) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
    foreach (string argument in new[] { "-noshell", "-noinput", "-eval", expression })
        info.ArgumentList.Add(argument);
    using var process = Process.Start(info)!;
    var output = process.StandardOutput.ReadToEndAsync();
    var error = process.StandardError.ReadToEndAsync();
    try
    {
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
    }
    catch (TimeoutException)
    {
        process.Kill(true);
        throw;
    }
    string stdout = await output, stderr = await error;
    if (process.ExitCode != 0)
        throw new InvalidOperationException(OracleDiagnostics.ProcessExit(process.ExitCode, stderr, stdout));

    return stdout.Trim();
}
try
{
    string version = await RunOracle("{ok,V}=file:read_file(filename:join([code:root_dir(),\"releases\",erlang:system_info(otp_release),\"OTP_VERSION\"])), io:format(\"~s\",[V]), halt().");
    if (version != "29.1.1")
        throw new InvalidOperationException(OracleDiagnostics.VersionMismatch(version));
    versionVerified = true;
    string[] cases = ["1 + 2 * 3", "{1 == 1.0, 1 =:= 1.0}", "{0.0 == -0.0, 0.0 =:= -0.0}", "9007199254740993 > 9007199254740992.0", "{a, [1,2|tail]}", "lists:member(1, [1.0])", "lists:reverse([1,2,3])", "lists:sum([1,2,3])", "[1,2] ++ [3]", "[1,2,1] -- [1]", "-7 div 3", "-7 rem 3", "case {1,1} of {X,X} -> X; _ -> no end", "case a of X when hd(X) == 1; is_atom(X) -> ok end", "receive {hello, X} -> X after 0 -> timeout end", "lists:map(fun(X) -> X * 2 end, [1,2,3])", "'𐀀' > '\ufffd'", "{length([1,2]), hd([ok]), tl([1,2])}", "maps:get(a, #{a => 1, a => 42})", "case #{1 => integer, 1.0 => float} of #{1 := A, 1.0 := B} -> {A,B} end", "maps:get(a, #{}#{a => 1, a := 2})", "case #{a => {7,7}} of #{a := {X,X}} -> X end", "case 1 of K -> case #{2 => 42} of #{K + 1 := V} -> V end end", "case #{a => 1} of #{hd(atom) := X} -> wrong; _ -> ok end", "case a of K -> F = fun(#{K := K}) -> K end, F(#{a => 42}) end", "case #{} of M when M#{a := 1} =:= #{} -> wrong; _ -> ok end", "{is_map(#{}),is_map([]),map_size(#{a => 1}),is_map_key(a,#{a => 42}),map_get(a,#{a => 42})}", "map_get(1.0,#{1 => value})", "map_get(key,not_map)", "map_size([])", "is_map_key(key,42)", "case #{} of M when map_get(a,M) =:= 42; map_size(M) =:= 0 -> ok; _ -> no end", "#{}#{missing := 1}", "atom#{a := error(blurf)}", "error(boom)", "throw(reason)", "exit(reason)", "<<>>", "<<1,2,255>>", "<<511,-1,16:4,31:4>>", "<<5:3,17:5,3:2>>", "<<4660:16/big>>", "<<4660:16/little>>", "<<291:12/little>>", "<<4660:2/unit:8>>", "<<-1:16/signed-little>>", "case 4 of S -> <<(2+3):(S+1)>> end", "<<(<<1,2,3>>):2/binary>>", "<<1:1,(<<2:2>>)/bitstring,3:2>>", "<<(<<1:1>>)/binary-unit:1>>", "<<-123:0,42:8>>", "<<atom>>", "<<1:(-1)>>", "<<(<<1>>):2/binary>>", "<<(<<1:1>>)/binary>>", "<<(<<1,2>>):all/binary>>", "<<1:8/integer-integer-big-big-unit:1-unit:1>>", "{bit_size(<<>>),byte_size(<<>>),bit_size(<<1:1>>),byte_size(<<1:1>>),byte_size(<<1:9>>)}", "{is_bitstring(<<>>),is_binary(<<>>),is_bitstring(<<1:1>>),is_binary(<<1:1>>),is_bitstring([])}", "bit_size(atom)", "byte_size([1,2])", "case atom of X when bit_size(X) =:= 0; byte_size(X) =:= 0; is_atom(X) -> ok end"];
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
        "<<X:16>> = <<1>>",
        "case 42 of X -> F = fun({X}) -> X; (_) -> X end, F(atom) end",
        "case 42 of X -> F = fun(X) when is_integer(X) -> X; (_) -> X end, F(atom) end",
        "case {8,42} of {L,B} -> F = fun(<<L:L,B:L>>) -> {L,B}; (_) -> {L,B} end, F(atom) end"];
    cases = [..cases,
        "<<1.5:16/float>>",
        "<<1.5:32/float>>",
        "<<1.5/float>>",
        "<<1.5:16/float-little,1.5:32/float-native>>",
        "<<1:16/float,2:32/float,9007199254740993:64/float>>",
        "case <<9007199254740995/float,-9007199254740995/float,9007199254740993/float>> of <<A/float,B/float,C/float>> -> {A,B,C} end",
        "case <<9007199254740995/float>> of <<9007199254740995/float>> -> ok; _ -> no end",
        "<<1.5:2/float-unit:8>>",
        "<<1.0:0/float>>",
        "<<1.0:8/float>>",
        "<<atom/float>>",
        "<<1.00048840045928955078125:16/float,1.00048828125:16/float,1.000488282181322574615478515625:16/float>>",
        "<<0.000000059604644775390625:16/float,0.000030517578125:16/float,0.000021159648895263671875:16/float>>",
        "<<1000000000:16/float,1.0e100:32/float>>",
        "case <<0.1:16/float>> of <<F:16/float>> -> F end",
        "case <<1.5:32/float>> of <<F:32/float>> -> F end",
        "case <<1.5/float>> of <<F/float>> -> F end",
        "case <<1:1,1.5:16/float-little,5:3>> of <<_:1,F:16/float-little,T:3>> -> {F,T} end",
        "case <<>> of <<F:0/float>> -> F; _ -> no end",
        "case <<31744:16>> of <<F:16/float>> -> wrong; _ -> ok end",
        "case <<32256:16>> of <<F:16/float>> -> wrong; _ -> ok end",
        "case <<2139095040:32>> of <<F:32/float>> -> wrong; _ -> ok end",
        "case <<9221120237041090560:64>> of <<F:64/float>> -> wrong; _ -> ok end",
        "case <<1:8>> of <<F:16/float>> -> wrong; _ -> ok end",
        "case <<0:8>> of <<F:8/float>> -> wrong; _ -> ok end",
        "case <<1:32/float>> of <<1:32/float>> -> ok; _ -> no end",
        "case 1 of X -> case <<1:32/float>> of <<X:32/float>> -> wrong; _ -> ok end end",
        "case <<-0.0:32/float>> of <<0.0:32/float>> -> wrong; <<-0.0:32/float>> -> ok end",
        "case <<16,1.5:16/float>> of <<N,F:N/float>> -> F end",
        "case #{<<1.5:16/float>> => ok} of #{<<1.5:16/float>> := V} when <<1.5:16/float>> =:= <<62,0>> -> V end"];
    cases = [..cases,
        "<<0/utf8,127/utf8,128/utf8,2047/utf8,2048/utf8,55295/utf8,57344/utf8,65535/utf8,65536/utf8,1114111/utf8>>",
        "<<128512/utf16,128512/utf16-little>>",
        "<<128512/utf32,128512/utf32-little>>",
        "<<128512/utf16-native,128512/utf32-native>>",
        "<<128512/utf8-little>>",
        "<<-1/utf8>>",
        "<<55296/utf16>>",
        "<<1114112/utf32>>",
        "<<1.0/utf8>>",
        "<<65/utf8,65/utf16>>",
        "case <<65534/utf8,65535/utf16>> of <<A/utf8,B/utf16>> -> {A,B} end",
        "case <<240,159,152,128,42>> of <<X/utf8,Rest/binary>> -> {X,Rest} end",
        "case <<216,61,222,0>> of <<X/utf16>> -> X end",
        "case <<0,246,1,0>> of <<X/utf32-little>> -> X end",
        "case <<192,175>> of <<X/utf8,Rest/binary>> -> wrong; _ -> ok end",
        "case <<237,160,128>> of <<X/utf8,Rest/binary>> -> wrong; _ -> ok end",
        "case <<244,144,128,128>> of <<X/utf8,Rest/binary>> -> wrong; _ -> ok end",
        "case <<226,130>> of <<X/utf8,Rest/binary>> -> wrong; _ -> ok end",
        "case <<128>> of <<X/utf8>> -> wrong; _ -> ok end",
        "case <<216,0>> of <<X/utf16>> -> wrong; _ -> ok end",
        "case <<216,0,0,65>> of <<X/utf16>> -> wrong; _ -> ok end",
        "case <<55296:32>> of <<X/utf32>> -> wrong; _ -> ok end",
        "case <<1114112:32>> of <<X/utf32>> -> wrong; _ -> ok end",
        "case <<1:3,128512/utf16-little,5:3>> of <<_:3,X/utf16-little,T:3>> -> {X,T} end",
        "case <<240,159,152,64:7>> of <<X/utf8>> -> wrong; _ -> ok end",
        "case <<65,128>> of <<X/utf8,Y/utf8>> -> wrong; <<X:16>> -> X end",
        "case <<3/utf8,5:3>> of <<N/utf8,X:N>> -> X end",
        "case 128512 of X -> case <<128512/utf16>> of <<X/utf16>> -> X end end",
        "<<\"A😀\"/utf8,\"A😀\"/utf16-little,\"\"/utf32>>",
        "case <<\"A😀\"/utf8,42>> of <<\"A😀\"/utf8,X>> -> X end",
        "case #{<<128512/utf8>> => 42} of #{<<128512/utf8>> := X} when <<65/utf8>> =:= <<65>> -> X end"];
    string hugeInteger = (System.Numerics.BigInteger.One << 2000).ToString(System.Globalization.CultureInfo.InvariantCulture);
    cases = [..cases,
        "lists:duplicate(0,anything)",
        "lists:duplicate(1,a)",
        "lists:duplicate(3,{a,1})",
        "lists:duplicate(2,[a|tail])",
        "lists:duplicate(-1,a)",
        "lists:duplicate(-999999999999999999999999,a)",
        "lists:duplicate(0.0,a)",
        "lists:duplicate(atom,a)",
        "lists:duplicate([],a)",
        "lists:flatten([])",
        "lists:flatten([a,b,c])",
        "lists:flatten([[],[[],[]],[]])",
        "lists:flatten([a,[[b],[]],{[c]},<<1>>])",
        "lists:flatten([\"A😀\",[66]])",
        "lists:flatten([],[[]])",
        "lists:flatten([[a],b],[[c]])",
        "lists:flatten([[a],b],[c|tail])",
        "lists:flatten(atom)",
        "lists:flatten({a})",
        "lists:flatten([a|tail])",
        "lists:flatten([[a|tail]])",
        "lists:flatten([],atom)",
        "lists:flatten(atom,[])",
        "lists:flatten([a|tail],[])",
        "lists:flatten([[a|tail]],[b|tail])"];
    cases = [..cases,
        "lists:append([])",
        "lists:append([tail])",
        "lists:append([42])",
        "lists:append([[a|tail]])",
        "lists:append([[a,b],[],[c,d]])",
        "lists:append([[a],[b],tail])",
        "lists:append([[],42])",
        "lists:append([[a],[b|tail]])",
        "lists:append(atom)",
        "lists:append([[a]|tail])",
        "lists:append([atom,[]])",
        "lists:append([[a|tail],[]])",
        "lists:append([atom|tail])",
        "lists:append([atom,[a]|tail])",
        "lists:member(a,[])",
        "lists:member(a,[a|tail])",
        "lists:member(b,[a,b|tail])",
        "lists:member({1},[{1.0}])",
        "lists:member(#{1=>a},[#{1.0=>a}])",
        "lists:member(#{a=>1,b=>2},[#{b=>2,a=>1}])",
        "lists:member(<<1:1>>,[<<1:2>>])",
        "lists:member([a|tail],[[a|tail]])",
        "lists:member(0.0,[-0.0])",
        "lists:member(a,atom)",
        "lists:member(b,[a|tail])",
        "lists:member(1,[1.0|tail])"];
    cases = [..cases,
        "lists:last([a])",
        "lists:last([a,[],{b,2}])",
        "lists:last([])",
        "lists:last(atom)",
        "lists:last([a,b|tail])",
        "lists:split(0,[])",
        "lists:split(0,[a,b])",
        "lists:split(1,[a,b,c])",
        "lists:split(2,[a,b])",
        "lists:split(0,[a|tail])",
        "lists:split(1,[a,b|tail])",
        "lists:split(2,[a,b|tail])",
        "lists:split(1,[a|42])",
        "lists:split(-1,[a])",
        "lists:split(1.0,[a])",
        "lists:split(atom,[a])",
        "lists:split(0,atom)",
        "lists:split(1,[])",
        "lists:split(3,[a,b])",
        "lists:split(3,[a,b|tail])",
        "lists:split(999999999999999999999999,[a])",
        "lists:split(999999999999999999999999,[a|tail])"];
    cases = [..cases,
        "{9007199254740995+0.0,0.0+9007199254740995,9007199254740995*1.0,9007199254740995-9007199254740996.0}",
        "{9007199254740995/1,1/9007199254740995}",
        "1/"+hugeInteger,
        hugeInteger+"*0.0",
        "0.0/"+hugeInteger,
        "case "+hugeInteger+" of X when 0.0/X =:= 0.0 -> wrong; _ -> ok end"];
    cases = [..cases,
        "<<\"ab\":16/little>>",
        "<<\"AB\":2/unit:8>>",
        "<<1:1,\"AB\":4,3:2>>",
        "<<\"Ā😀\"/integer>>",
        "<<\"AB\":16/float>>",
        "<<\"A\":32/float-little>>",
        "<<\"A\"/float>>",
        "case ok of ok -> put(counter,0),B = <<\"ab\":(put(counter,get(counter)+1))>>, {B,get(counter)} end",
        "case ok of ok -> B = <<\"ab\":(S = 8)>>,{B,S} end",
        "<<\"\":16/little,\"\":32/float,42>>",
        "<<\"\":(error(boom))>>",
        "<<\"abc\":0,42>>",
        "<<\"ab\":(-1)>>",
        "<<\"\":(-1)>>",
        "case all of S -> <<\"\":S>> end",
        "<<\"ab\":1.0>>",
        "<<\"\":1/float>>",
        "<<\"ab\":0/float>>",
        "<<\"a\"/binary>>",
        "<<\"\"/binary>>",
        "<<[65,66]:8>>",
        "<<[]:8>>",
        "case #{<<\"ab\":16/little>> => 42} of #{<<\"ab\":16/little>> := X} when <<\"A\":16/float>> =:= <<84,16>> -> X end"];
    cases = [..cases,
        "lists:nth(2,[a,b,c])",
        "lists:nth(1,[a|tail])",
        "lists:nth(2,[a,b|tail])",
        "lists:nth(0,[a])",
        "lists:nth(-1,[a])",
        "lists:nth(1.0,[a])",
        "lists:nth(2,[a|tail])",
        "lists:nth(1,[])",
        "lists:nth(1,atom)",
        "lists:nth(999999999999999999999999,[a])",
        "lists:nthtail(0,[])",
        "lists:nthtail(0,[a|tail])",
        "lists:nthtail(2,[a,b|tail])",
        "lists:nthtail(0,atom)",
        "lists:nthtail(-1,[a])",
        "lists:nthtail(1.0,[a])",
        "lists:nthtail(3,[a,b|tail])",
        "lists:nthtail(1,[])",
        "lists:seq(1,3)",
        "lists:seq(2,1)",
        "lists:seq(-2,1)",
        "lists:seq(3,1)",
        "lists:seq(1.0,3)",
        "lists:seq(1,atom)",
        "lists:seq(1,6,2)",
        "lists:seq(6,1,-2)",
        "lists:seq(3,1,2)",
        "lists:seq(3,5,-2)",
        "lists:seq(7,7,0)",
        "lists:seq(1,2,0)",
        "lists:seq(4,1,2)",
        "lists:seq(3,6,-2)",
        "lists:seq(1,3,1.0)",
        "lists:seq(1.0,3,1)",
        "lists:seq(1,atom,1)",
        "lists:seq(9223372036854775808,9223372036854775812,2)"];
    cases = [..cases,
        "lists:reverse(atom)",
        "lists:reverse([a|tail])",
        "lists:reverse([a,b|tail])",
        "lists:reverse([a,b,c|tail])",
        "lists:reverse([],tail)",
        "lists:reverse([a,b],tail)",
        "lists:reverse(atom,tail)",
        "lists:reverse([a|tail],[])",
        "lists:keyfind(a,2,[atom,{}, {a},{x,a,first},{y,a,second}])",
        "lists:keyfind(a,1,[{a,found}|tail])",
        "lists:keymember(a,1,[{a}|tail])",
        "lists:keysearch(a,1,[{a}|tail])",
        "lists:keyfind(a,2,[{},atom,{a}])",
        "lists:keymember(a,1,[])",
        "lists:keysearch(a,1,[{b}])",
        "lists:keyfind(a,0,[])",
        "lists:keymember(a,-1,[{a}])",
        "lists:keysearch(a,1.0,[])",
        "lists:keyfind(a,576460752303423488,[])",
        "lists:keyfind(a,1,[{b}|tail])",
        "lists:keymember(a,1,atom)",
        "lists:keysearch(a,1,[atom|tail])",
        "lists:keyfind(a,576460752303423487,[{a}])",
        "lists:keyfind(9007199254740993,1,[{9007199254740992.0}])",
        "lists:keyfind(9007199254740992.0,1,[{9007199254740993}])",
        "lists:keysearch({[1],#{a=>2}},1,[{{[1.0],#{a=>2.0}},found}])",
        "lists:keyfind(0,1,[{-0.0}])",
        "lists:keyfind(-576460752303423488,1,[{-576460752303423488.0}])",
        "lists:keyfind(576460752303423489,1,[{576460752303423488.0}])",
        "lists:keyfind(#{1=>a},1,[{#{1.0=>a}}])"];
    cases = [.. cases, .. IfExpressionCases.All.Select(fixture => fixture.Source)];
    cases = [.. cases, .. BeginOperatorCases.All.Select(fixture => fixture.Source)];
    cases = [.. cases, .. ExpressionListCases.All.Select(fixture => fixture.Source)];
    cases = [.. cases, .. MapBindingCases.All.Select(fixture => fixture.Source)];
    cases = [.. cases, .. BitEvaluationCases.All.Select(fixture => fixture.Source)];
    cases = [.. cases, .. BitEmptyStringCases.All.Select(fixture => fixture.Source)];
    cases = [.. cases, .. MatchTimingCases.All.Select(fixture => fixture.Source)];
    cases = [.. cases, .. TryExpressionCases.All.Select(fixture => fixture.Source)];
    cases = [.. cases, .. CatchPatternCases.All.Select(fixture => fixture.Source)];
    cases = [.. cases, .. StackGuardScopeCases.All.Select(fixture => fixture.Source)];
    cases = [.. cases, .. MaybeExpressionCases.All.Select(fixture => fixture.Source)];
    cases = [.. cases, .. AliasPatternCases.All.Select(fixture => fixture.Source)];
    cases = [.. cases, .. ListComprehensionCases.All.Select(fixture => fixture.Source), .. BinaryComprehensionCases.All.Select(fixture => fixture.Source), .. MapComprehensionCases.All.Select(fixture => fixture.Source), .. MapTemplateOrderCases.All.Select(fixture => fixture.Source), .. ComparatorSortCases.All.Select(fixture => fixture.Source), .. ZipGeneratorCases.All.Select(fixture => fixture.Source)];
    planned = cases.Length;
    foreach (string source in cases)
    {
        activeSource = source;
        string wrapped = "try (" + source + ") of OracleValue -> {ok,OracleValue} catch OracleClass:OracleReason -> {error,OracleClass,OracleReason} end";
        Term expected = OracleResultProtocol.Decode(await RunOracle(OracleProtocol.EvaluationCommand(wrapped)));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        var expression = new Parser(source).ParseExpression();
        Semantics.Validate(expression);
        var process = runtime.Spawn(async ctx =>
        {
            try
            {
                actual = Term.Tuple(Term.A(OracleOutcomeTags.Success), await Execution.EvaluateAsync(expression, ctx));
            }
            catch (ErlangException ex)
            {
                actual = Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.A(ex.ExceptionClass), ex.Reason);
            }

            return Term.A(OracleProcessResults.Completed);
        });
        Term reason = await process.Completion;
        bool passed = reason.Equals(Term.A(ProcessExitReasons.Normal)) && actual is not null && actual.Equals(expected);
        if (!passed)
            failed++;
        Console.WriteLine((passed ? "PASS " : "FAIL ") + source);
        results.Add(new
        {
            Source = source,
            Expected = expected.ToString(),
            Actual = actual?.ToString(),
            ExitReason = reason.ToString(),
            Passed = passed
        });
        await SaveReport(false);
    }
    foreach (var fixture in CompiledModuleCases.All)
    {
        activeSource = fixture.Source;
        Term expected = OracleResultProtocol.Decode(await RunOracle(CompiledModuleProtocol.Command(fixture.Source)));
        using var generated = GeneratedModuleCompiler.Compile(fixture.Source);
        Term actual = await CompiledModuleExecution.Run(generated);
        bool passed = actual.Equals(expected) && expected.Equals(fixture.Expected);
        if (!passed)
        {
            failed++;
            moduleFailed++;
        }
        moduleResults.Add(new
        {
            fixture.Name,
            fixture.Source,
            SourceSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(fixture.Source))),
            generated.CSharpSha256,
            Expected = expected.ToString(),
            FixtureExpected = fixture.Expected.ToString(),
            Actual = actual.ToString(),
            Passed = passed
        });
        Console.WriteLine((passed ? "PASS module " : "FAIL module ") + fixture.Name);
        await SaveReport(false);
    }
    foreach (var fixture in CompiledDiagnosticCases.All)
    {
        activeSource = fixture.Source;
        Term reference = OracleResultProtocol.Decode(await RunOracle(CompiledModuleProtocol.DiagnosticCommand(fixture.Source)));
        string? code = null;
        string? message = null;
        bool accepted = true;
        try
        {
            using var generated = GeneratedModuleCompiler.Compile(fixture.Source);
        }
        catch (CompileException exception)
        {
            accepted = false;
            code = exception.Code;
            message = exception.Message;
        }
        Term expected = CompiledDiagnosticExpectations.ReferenceOutcome(fixture);
        bool passed = reference.Equals(expected)
            && !accepted
            && code == CompiledDiagnosticExpectations.Code(fixture)
            && message == CompiledDiagnosticExpectations.Message(fixture);
        if (!passed)
        {
            failed++;
            diagnosticFailed++;
        }
        diagnosticResults.Add(new
        {
            fixture.Name,
            fixture.Source,
            SourceSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(fixture.Source))),
            Expected = expected.ToString(),
            Reference = reference.ToString(),
            ImplementationAccepted = accepted,
            ImplementationCode = code,
            ImplementationMessage = message,
            Passed = passed
        });
        Console.WriteLine((passed ? "PASS diagnostic " : "FAIL diagnostic ") + fixture.Name);
        await SaveReport(false);
    }
    await SaveReport(true);

    return failed == 0 ? 0 : 1;
}
catch (Exception ex)
{
    await SaveReport(false, ex.Message);
    Console.Error.WriteLine("Differential oracle unavailable or failed: " + ex.Message);

    return 2;
}
