using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace BeamFixtureExport;

internal static class MetadataMutationExporter
{
    internal static async Task Append(string installedRoot, string output)
    {
        string root = Path.GetFullPath(installedRoot);
        string directory = Path.Combine(Path.GetFullPath(output), MetadataMutationNames.Directory);
        Directory.CreateDirectory(directory);
        string oracle = Path.Combine(
            root,
            FixtureExportNames.BinaryDirectory,
            FixtureExportNames.OracleExecutable + (OperatingSystem.IsWindows() ? FixtureExportNames.ExecutableExtension : "")
        );
        FixtureDefinition constant = FixtureCases.All.Single(row => row.Module == MetadataMutationNames.ConstantModule);
        string input = Path.Combine(Path.GetFullPath(output), constant.Module + MetadataMutationNames.BeamSuffix);
        var declarations = new List<object>();
        foreach (string chunk in new[] { MetadataMutationNames.AttributesChunk, MetadataMutationNames.CompilationChunk })
        {
            foreach (MetadataMutationCase row in MetadataMutationCases.All)
                declarations.Add(new { Name = chunk + "-" + row.Name, Chunk = chunk, row.PayloadKind, row.TermExpression, row.IndependentExpectedExpression, row.ExpectedLoadAccepted, BodyExpected = constant.IndependentExpectedExpression });
        }
        await File.WriteAllTextAsync(
            Path.Combine(directory, MetadataMutationNames.LedgerFilename),
            JsonSerializer.Serialize(
                new
                {
                    SourcePin = OraclePin.Commit,
                    SourceTag = OraclePin.Tag,
                    BaselineFixtures = FixtureCases.All.Concat(FixtureClosureCases.All).Select(row => new { row.Module, SourceSha256 = Hash(System.Text.Encoding.UTF8.GetBytes(row.Source.Replace("\r\n", "\n") + "\n")), row.IndependentExpectedExpression }),
                    InputBeamSha256 = Hash(await File.ReadAllBytesAsync(input)),
                    Declarations = declarations,
                    MutationCasesOnLoadExecuted = false,
                    SeparateObserverTrack = MetadataMutationNames.ObserverTrackScope,
                    OnLoadExecuted = false
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        var evidence = new List<MetadataMutationEvidence>();
        foreach (string chunk in new[] { MetadataMutationNames.AttributesChunk, MetadataMutationNames.CompilationChunk })
        {
            foreach (MetadataMutationCase row in MetadataMutationCases.All)
            {
                string name = chunk + "-" + row.Name;
                string prefix = Path.Combine(directory, name);
                string beam = prefix + MetadataMutationNames.BeamSuffix;
                string payload = prefix + MetadataMutationNames.PayloadSuffix;
                string actual = prefix + MetadataMutationNames.ActualSuffix;
                string expected = prefix + MetadataMutationNames.ExpectedSuffix;
                string text = prefix + MetadataMutationNames.TextSuffix;
                string evaluation = prefix + MetadataMutationNames.EvaluationSuffix;
                string expression = Evaluation(
                    input,
                    chunk,
                    row,
                    constant,
                    beam,
                    payload,
                    actual,
                    expected,
                    text
                );
                await File.WriteAllTextAsync(evaluation, expression + "\n");
                await OracleCommand.Run(oracle, [OracleCommandNames.NoShell, OracleCommandNames.Evaluate, expression], prefix + MetadataMutationNames.LogSuffix);
                byte[] actualBytes = await File.ReadAllBytesAsync(actual);
                byte[] expectedBytes = await File.ReadAllBytesAsync(expected);
                if (!actualBytes.AsSpan().SequenceEqual(expectedBytes))
                    throw new InvalidDataException(name);
                evidence.Add(new(
                    name,
                    chunk,
                    row.ExpectedLoadAccepted,
                    row.IndependentExpectedExpression,
                    Hash(await File.ReadAllBytesAsync(beam)),
                    Hash(await File.ReadAllBytesAsync(payload)),
                    Hash(actualBytes),
                    Hash(expectedBytes),
                    Hash(await File.ReadAllBytesAsync(evaluation)),
                    int.Parse(
                        await File.ReadAllTextAsync(prefix + MetadataMutationNames.LogSuffix + OracleCommandNames.ExitCodeSuffix),
                        CultureInfo.InvariantCulture
                    )
                ));
                await File.WriteAllTextAsync(
                    Path.Combine(directory, MetadataMutationNames.EvidenceFilename),
                    JsonSerializer.Serialize(
                        new
                        {
                            Complete = evidence.Count == declarations.Count,
                            OriginalObserved = true,
                            MutationCasesOnLoadExecuted = false,
                            SeparateObserverTrack = MetadataMutationNames.ObserverTrackScope,
                            OnLoadExecuted = false,
                            Evidence = evidence
                        },
                        new JsonSerializerOptions { WriteIndented = true }
                    )
                );
            }
        }
    }

    private static string Evaluation(
        string input,
        string chunk,
        MetadataMutationCase row,
        FixtureDefinition constant,
        string beam,
        string payload,
        string actual,
        string expected,
        string text
    )
    {
        string payloadExpression = row.PayloadKind switch
        {
            MetadataMutationNames.MissingChunk or MetadataMutationNames.EmptyChunk => "<<>>",
            MetadataMutationNames.TrailingChunk => "<<(term_to_binary(" + row.TermExpression + "))/binary," + MetadataMutationWire.TrailingFirstByte.ToString(CultureInfo.InvariantCulture) + "," + MetadataMutationWire.TrailingSecondByte.ToString(CultureInfo.InvariantCulture) + ">>",
            MetadataMutationNames.CompressedChunk => "term_to_binary(" + row.TermExpression + ",[compressed])",
            MetadataMutationNames.TruncatedChunk => "<<" + MetadataMutationWire.ExternalTermVersion.ToString(CultureInfo.InvariantCulture) + ">>",
            _ => "term_to_binary(" + row.TermExpression + ")"
        };
        string wanted = row.ExpectedLoadAccepted
            ? "{accepted," + row.IndependentExpectedExpression + "," + constant.IndependentExpectedExpression + ",true}"
            : "{error,badfile}";
        string key = chunk == MetadataMutationNames.AttributesChunk ? MetadataMutationNames.AttributesKey : MetadataMutationNames.CompilationKey;
        string expression = """
            try
                {ok,Input}=file:read_file(@INPUT@),
                <<"FOR1",_:32/big,"BEAM",Body/binary>>=Input,
                Parse=fun Parse(<<>>,Acc)->lists:reverse(Acc);
                    Parse(<<Id:4/binary,Size:32/big,Data:Size/binary,Rest/binary>>,Acc)->
                        Pad=(4-(Size rem 4)) rem 4,
                        <<_:Pad/binary,Tail/binary>>=Rest,
                        Parse(Tail,[{Id,Data}|Acc])
                end,
                Chunks=Parse(Body,[]),
                Chunk = @CHUNK@,
                Payload = @PAYLOAD@,
                Fresh=case @REMOVE@ of
                    true->[C || C={Id,_}<-Chunks,Id=/=Chunk];
                    false->[case Id=:=Chunk of true->{Id,Payload};false->{Id,Data} end || {Id,Data}<-Chunks]
                end,
                true=lists:keymember(Chunk,1,Chunks),
                Encoded=iolist_to_binary([begin N=byte_size(Data),Pad=(4-(N rem 4)) rem 4,<<Id/binary,N:32/big,Data/binary,0:(Pad*8)>> end || {Id,Data}<-Fresh]),
                Size=byte_size(Encoded)+4,
                Image = <<"FOR1",Size:32/big,"BEAM",Encoded/binary>>,
                ok=file:write_file(@BEAM@,Image),
                ok=file:write_file(@PAYLOADFILE@,Payload),
                {ok,{@MODULE@,OriginalMd5}}=beam_lib:md5(Input),
                Load=code:load_binary(@MODULE@,@BEAM@,Image),
                {Result,Details}=case Load of
                    {module,@MODULE@}->
                        Value=erlang:get_module_info(@MODULE@,@KEY@),
                        BodyResult=apply(@MODULE@,run,[]),
                        Md5=erlang:get_module_info(@MODULE@,md5),
                        {{accepted,Value,BodyResult,Md5=:=OriginalMd5},{Load,erlang:get_module_info(@MODULE@,attributes),erlang:get_module_info(@MODULE@,compile),erlang:get_module_info(@MODULE@,exports),erlang:get_module_info(@MODULE@,functions)}};
                    Error->{Error,{Load}}
                end,
                Wanted=@EXPECTED@,
                ok=file:write_file(@ACTUAL@,term_to_binary(Result)),
                ok=file:write_file(@EXPECTEDFILE@,term_to_binary(Wanted)),
                ok=file:write_file(@TEXT@,unicode:characters_to_binary(io_lib:format("~tp.~n",[{Result,Details}]))),
                case Result=:=Wanted of true->halt(0);false->halt(6) end
            catch Class:Reason:Stack->io:format(standard_error,"~tp~n",[{Class,Reason,Stack}]),halt(4) end.
            """;

        return expression.Replace("@INPUT@", String(input)).Replace("@CHUNK@", "<<" + String(chunk) + ">>")
            .Replace("@PAYLOAD@", payloadExpression).Replace("@REMOVE@", row.PayloadKind == MetadataMutationNames.MissingChunk ? "true" : "false")
            .Replace("@BEAM@", String(beam)).Replace("@PAYLOADFILE@", String(payload)).Replace("@MODULE@", constant.Module)
            .Replace("@KEY@", key).Replace("@EXPECTED@", wanted).Replace("@ACTUAL@", String(actual))
            .Replace("@EXPECTEDFILE@", String(expected)).Replace("@TEXT@", String(text));
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static string String(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
}
