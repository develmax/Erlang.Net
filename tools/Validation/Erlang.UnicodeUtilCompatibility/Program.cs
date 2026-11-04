using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang;
using static UnicodeUtilValidationNames;

if (args.Length != 4 || args[2] is not ("Managed" or "Beam") || args[3] is not ("core" or "long" or "tables" or "conformance" or "probe"))
{
    Console.Error.WriteLine(Usage);

    return 2;
}
string repository = Path.GetFullPath(args[0]);
string corpus = Path.GetFullPath(args[1]);
bool managed = args[2] == "Managed";
byte[] Original(string module) => File.ReadAllBytes(Path.Combine(repository, "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp", module + ".beam"));
var selections = new RuntimeLibrarySelection([
    RuntimeModuleSelection.Managed("erlang"),RuntimeModuleSelection.Managed("lists"),
    managed ? RuntimeModuleSelection.Managed(Module) : RuntimeModuleSelection.Beam(Module,Original(Module),["lists"]),
    RuntimeModuleSelection.Managed("unicode",["lists",Module])]);
await using var runtime = await ProcessRuntime.CreateAsync(selections, RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
RuntimeLibraryModuleSnapshot receipt = runtime.LibrarySelection.Modules.Single(row => row.Module == Module);
if (receipt.Backend != (managed ? RuntimeModuleBackend.Managed : RuntimeModuleBackend.Beam)
    || managed && runtime.LibrarySelection.Modules.Any(row => row.Backend != RuntimeModuleBackend.Managed)) throw new InvalidOperationException(Failure);
var failures = new List<object>();
long checkedCases = 0;
int negativeControls = 0;
await using var actor = new UnicodeUtilValidationActor(runtime);
ValueTask<Term> Call(ProcessContext context, string name, Term input) => managed
    ? UnicodeUtilModule.Invoke(context, name, [input]) : runtime.Modules.Call(
        context,
        Module,
        name,
        input
    );
if (args[3] is "core" or "long")
{
    using PreparedBeamImage observer = runtime.Modules.PrepareBeam(
        File.ReadAllBytes(Path.Combine(corpus, Observer + ".beam")),
        new BeamDependencySelection([Observer, Module, "unicode", "erlang", "lists", "binary", "file", "filename", "crypto"])
    );
    await runtime.InitializeModulesAsync(registry =>
    {
        if (registry.PublishPreparedBeam(observer).Status != BeamPublicationStatus.Published) throw new InvalidOperationException(Failure);
    });
    byte[] corpusBytes = File.ReadAllBytes(Path.Combine(corpus, args[3] == "long" ? "long.etf" : "contracts.etf"));
    // Map flat keys are ordered by the receiving runtime's atom table. Decode
    // under that table, exactly as an ETF arriving at this process would be.
    Term[] coreRows = await actor.InvokeAsync(context => Task.FromResult(Cons.Items(ExternalTermFormat.Decode(corpusBytes)).ToArray()));
    foreach (TupleTerm[] batch in coreRows.Cast<TupleTerm>().Chunk(args[3] == "long" ? 1 : Batch))
    {
        await actor.InvokeAsync(async context =>
        {
            foreach (TupleTerm row in batch)
            {
                var sent = (TupleTerm)await runtime.Modules.Call(
                    context,
                    Observer,
                    "worker",
                    row.Items[0],
                    row.Items[1],
                    context.Self
                );
                if (!sent.Items[1].Equals(row.Items[2]))
                {
                    Term actual = sent.Items[1];
                    Term expected = row.Items[2];
                    Term equality = actual is TupleTerm at && expected is TupleTerm et && at.Items[0].Equals(Term.A("ok"))
                        ? await runtime.Modules.Call(
                            context,
                            "erlang",
                            "=:=",
                            at.Items[1],
                            et.Items[1]
                        ) : Nil.Value;
                    failures.Add(new { Index = checkedCases, Function = row.Items[0].ToString(), Arguments = row.Items[1].ToString(), Expected = expected.ToString(), Actual = actual.ToString(), ValueEquality = equality.ToString(), ActualEtf = Convert.ToHexString(SHA256.HashData(ExternalTermFormat.Encode(actual))), ExpectedEtf = Convert.ToHexString(SHA256.HashData(ExternalTermFormat.Encode(expected))) });
                }
                checkedCases++;
            }

            return true;
        });
        if (args[3] == "long" || checkedCases % 1024 == 0) Console.WriteLine($"{args[2]} {args[3]}: {checkedCases}/{coreRows.Length}, differences={failures.Count}");
    }
    await actor.InvokeAsync(async context =>
    {
        var actual = (MapTerm)await runtime.Modules.Call(
            context,
            Module,
            "get_case",
            Term.I(65)
        );
        var changed = new MapTerm(actual.Entries.Select(entry => entry.Key.Equals(Term.A("fold"))
            ? new KeyValuePair<Term, Term>(entry.Key, Term.I(-1)) : entry));
        var missing = new MapTerm(actual.Entries.Where(entry => !entry.Key.Equals(Term.A("fold"))));
        foreach (MapTerm bad in new[] { changed, missing })
        {
            if ((await runtime.Modules.Call(
                context,
                "erlang",
                "=:=",
                actual,
                bad
            )).Equals(Term.A("true"))) throw new InvalidOperationException(Failure);
            negativeControls++;
        }

        return true;
    });
}
if (args[3] == "probe")
{
    byte[] input = File.ReadAllBytes(Path.Combine(corpus, "transition-details.etf"));
    Term[] rows = await actor.InvokeAsync(context => Task.FromResult(Cons.Items(ExternalTermFormat.Decode(input)).ToArray()));
    foreach (TupleTerm row in rows)
    {
        await actor.InvokeAsync(async context =>
        {
            int cp = (int)((Integer)row.Items[0]).Value;
            Term[] expected = Cons.Items(row.Items[1]).ToArray();
            int index = 0;
            foreach (Term probe in UnicodeUtilValidationCases.Probes(cp))
            {
                Term actual = await Call(context, "gc", probe);
                if (!actual.Equals(expected[index])) failures.Add(new { Codepoint = cp, Probe = index, Input = probe.ToString(), Expected = expected[index].ToString(), Actual = actual.ToString() });
                checkedCases++;
                index++;
            }

            return true;
        });
    }
}
if (args[3] == "tables")
{
    foreach (TupleTerm row in Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(corpus, "tables.etf")))))
    {
        await actor.InvokeAsync(async context =>
        {
            int first = (int)((Integer)row.Items[0]).Value;
            int last = (int)((Integer)row.Items[1]).Value;
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            for (int cp = first; cp <= last; cp++)
            {
                Term scalar = Term.I(cp);
                var lookup = (MapTerm)await Call(context, "lookup", scalar);

                Term Field(MapTerm map, string name) => map.Get(Term.A(name));
                var category = (TupleTerm)Field(lookup, "category");
                if (!category.Equals(await Call(context, "category", scalar))) throw new InvalidOperationException(Failure);
                var casing = (MapTerm)await Call(context, "get_case", scalar);
                using var bytes = new MemoryStream();
                using var writer = new BinaryWriter(bytes, Encoding.UTF8, leaveOpen: true);

                void Word(Term atom)
                {
                    byte[] word = Encoding.UTF8.GetBytes(((Atom)atom).Name);
                    writer.Write(word.Length);
                    writer.Write(word);
                }

                void Tagged(Term list)
                {
                    Term[] items = Cons.Items(list).ToArray();
                    writer.Write(items.Length);
                    foreach (TupleTerm item in items)
                    {
                        writer.Write((byte)((Integer)item.Items[0]).Value);
                        writer.Write((int)((Integer)item.Items[1]).Value);
                    }
                }
                writer.Write(cp);
                writer.Write((byte)((Integer)Field(lookup, "ccc")).Value);
                Word(category.Items[0]);
                Word(category.Items[1]);
                Tagged(Field(lookup, "canon"));
                Term compat = Field(lookup, "compat");
                writer.Write((byte)(compat is Nil ? 0 : 1));
                if (compat is TupleTerm tuple)
                {
                    Word(tuple.Items[0]);
                    Tagged(tuple.Items[1]);
                }
                foreach (string key in new[] { "upper", "lower", "title", "fold" })
                {
                    Term value = Field(casing, key);
                    writer.Write((byte)(value is Integer ? 0 : 1));
                    if (value is Integer integer) writer.Write((int)integer.Value);
                    else
                    {
                        Term[] items = Cons.Items(value).ToArray();
                        writer.Write(items.Length);
                        foreach (Integer item in items) writer.Write((int)item.Value);
                    }
                }
                foreach (string function in new[] { "is_wide", "is_whitespace", "is_other_id_start", "is_other_id_continue", "is_letter_not_pattern_syntax" })
                    writer.Write((byte)((await Call(context, function, scalar)).Equals(Term.A("true")) ? 1 : 0));

                void Shape(Term value)
                {
                    if (value is Integer integer)
                    {
                        writer.Write((byte)0);
                        writer.Write((int)integer.Value);
                    }
                    else if (value is Nil) writer.Write((byte)1);
                    else
                    {
                        writer.Write((byte)2);
                        var cell = (Cons)value;
                        Shape(cell.Head);
                        Shape(cell.Tail);
                    }
                }
                int[][] probes = [[cp,769,65],[65,cp,66],[cp,65,66],[cp,8205,128512,65],
                    [cp,2381,2325],[2325,cp,2325],[cp,4352,4449,4520,65],[4352,cp,4449,4520,65]];
                foreach (int[] probe in probes) Shape(await Call(context, "gc", Cons.From(probe.Select(value => (Term)Term.I(value)))));
                hash.AppendData(bytes.GetBuffer().AsSpan(0, (int)bytes.Length));
                checkedCases++;
            }
            byte[] actual = hash.GetHashAndReset();
            if (!actual.AsSpan().SequenceEqual(((BitString)row.Items[2]).ToArray())) failures.Add(new { First = first, Last = last, Actual = Convert.ToHexString(actual) });

            return true;
        });
        Console.WriteLine($"{args[2]} exhaustive tables: {checkedCases}/{Maximum + 1}, failed chunks={failures.Count}");
    }
}
if (args[3] == "conformance")
{
    async ValueTask<Term> Fetch(ProcessContext context, string function, Term input)
    {
        var output = new List<Term>();
        while (true)
        {
            Term next = await Call(context, function, input);
            if (next is Nil) return Cons.From(output);
            if (next is not Cons cell) throw new InvalidOperationException(Failure);
            if (cell.Head is Integer) output.Add(cell.Head); else output.AddRange(Cons.Items(cell.Head));
            input = cell.Tail;
        }
    }
    int lineNumber = 0;
    foreach (string text in File.ReadLines(Path.Combine(corpus, "NormalizationTest.txt")))
    {
        lineNumber++;
        string content = text.Split('#')[0].Trim();
        if (content.Length == 0 || content[0] == '@') continue;
        Term[] columns = content.Split(';').Take(NormalizationColumns).Select(field => Cons.From(field.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(cp => (Term)Term.I(int.Parse(cp, NumberStyles.HexNumber, CultureInfo.InvariantCulture))))).ToArray();
        await actor.InvokeAsync(async context =>
        {
            for (int form = 0; form < Forms.Length; form++) for (int column = 0; column < columns.Length; column++)
            {
                Term actual = await Fetch(context, Forms[form], columns[column]);
                Term expected = columns[NormalizationExpected[form, column]];
                if (!actual.Equals(expected)) failures.Add(new { Line = lineNumber, Form = Forms[form], Column = column, Expected = expected.ToString(), Actual = actual.ToString() });
                checkedCases++;
            }

            return true;
        });
    }
    lineNumber = 0;
    foreach (string text in File.ReadLines(Path.Combine(corpus, "GraphemeBreakTest.txt")))
    {
        lineNumber++;
        string content = text.Split('#')[0].Trim();
        if (content.Length == 0) continue;
        var clusters = new List<Term>();
        var current = new List<Term>();
        foreach (string token in content.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token == "÷")
            {
                if (current.Count != 0)
                {
                    clusters.Add(current.Count == 1 ? current[0] : Cons.From(current));
                    current.Clear();
                }
            }
            else if (token != "×") current.Add(Term.I(int.Parse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
        }
        Term flat = Cons.From(clusters.SelectMany(value => value is Integer ? new[] { value } : Cons.Items(value)));
        await actor.InvokeAsync(async context =>
        {
            Term remaining = flat;
            var actual = new List<Term>();
            while (await Call(context, "gc", remaining) is Cons cell)
            {
                actual.Add(cell.Head);
                remaining = cell.Tail;
            }
            if (!Cons.From(actual).Equals(Cons.From(clusters))) failures.Add(new { Line = lineNumber, Expected = Cons.From(clusters).ToString(), Actual = Cons.From(actual).ToString() });
            checkedCases++;

            return true;
        });
    }
}
File.WriteAllText(
    Path.Combine(corpus, args[2] + "-" + args[3] + ".json"),
    JsonSerializer.Serialize(
        new
        {
            Passed = failures.Count == 0,
            Backend = args[2],
            Mode = args[3],
            Cases = checkedCases,
            Failures = failures,
            NegativeControls = negativeControls,
            GraphemeTransitionProbes = args[3] == "tables" ? checkedCases * 8 : 0,
            receipt.InputSha256,
            receipt.ManagedAssemblies,
            ManagedAssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(UnicodeUtilModule).Assembly.Location))),
            RuntimeAssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location)))
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
Console.WriteLine($"{args[2]} {args[3]}: {checkedCases}, differences={failures.Count}");
return failures.Count == 0 ? 0 : 1;
