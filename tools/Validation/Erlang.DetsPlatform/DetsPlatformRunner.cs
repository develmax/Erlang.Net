using Erlang;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal static class DetsPlatformRunner
{
    internal static async Task Run(string repository, string output, bool managed)
    {
        await using var runtime = new ProcessRuntime();
        var prepared = new List<PreparedBeamImage>();
        var assets = Directory.EnumerateFiles(Path.Combine(repository, "src/Otp/Original/Foundation"), "*.beam", SearchOption.AllDirectories)
            .GroupBy(Path.GetFileNameWithoutExtension).ToDictionary(group => group.Key!, group => group.First(), StringComparer.Ordinal);
        var dependencies = new BeamDependencySelection(assets.Keys.Concat(new[] { "erlang", "erts_internal" }).Distinct());
        string[] modules = { "dets", "dets_server", "dets_sup", "dets_utils", "dets_v9", "ets", "file", "file_server", "file_io_server", "timer", "os", "orddict", "dict", "file_sorter", "calendar", "prim_file", "prim_buffer", "raw_file_io", "raw_file_io_list", "raw_file_io_delayed", "raw_file_io_compressed", "raw_file_io_inflate", "filename", "lists", "gen", "gen_server", "supervisor", "proc_lib", "sys", "logger", "logger_config", "logger_filters", "logger_backend", "logger_handler_watcher", "logger_formatter", "io", "io_lib", "io_lib_format", "erl_stdlib_errors", "erl_kernel_errors", "erl_posix_msg", "init", "binary", "unicode", "unicode_util", "qlc", "maps", "ordsets", "gb_sets", "gb_trees", "sets", "queue", "proplists", "erl_anno", "erl_parse", "erl_scan" };
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapEtsLibrary.Register(runtime, registry);
            RuntimeBootstrapFileSystem.Register(runtime, registry);
            RuntimeBootstrapScheduler.Register(runtime, registry);
            RuntimeBootstrapInternalPorts416.Register(runtime, registry);
            RuntimeNativePrimitiveProviders.Register(runtime, registry, RuntimeNativePrimitives.PersistentTermCore | RuntimeNativePrimitives.InternalScalars413);
            if (managed) DetsModule.Register(registry);
        });
        foreach (string name in modules.Distinct())
        {
            if (name == "dets" && managed) continue;
            if (!assets.TryGetValue(name, out string? path)) continue;
            prepared.Add(runtime.Modules.PrepareBeam(File.ReadAllBytes(path), dependencies));
        }
        await runtime.InitializeModulesAsync(registry =>
        {
            var published = registry.PublishPreparedBeam(prepared.ToArray());
            if (published.Status != BeamPublicationStatus.Published) throw new InvalidOperationException("DETS platform source publication: " + published.Status);
        });
        var completion = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle worker = runtime.Spawn(async context =>
        {
            async ValueTask<Term> Call(string m, string f, params Term[] a)
            {
                Console.WriteLine((managed ? "Managed" : "Original") + " => " + m + ":" + f);

                return await runtime.Modules.Call(
                    context,
                    m,
                    f,
                    a
                );
            }
            int checks = 0;

            void Check(bool condition, string operation)
            {
                checks++;
                if (!condition) throw new InvalidOperationException("DETS platform contract: " + operation);
            }

            void Ok(Term value, string operation) => Check(value.Equals(Term.A("ok")), operation + ": " + value);
            try
            {
                Ok(await Call("prim_file", "on_load"), "prim_file on_load");
                Ok(await Call("prim_buffer", "on_load"), "prim_buffer on_load");
                foreach (string service in new[] { "file_server", "dets_sup", "dets_server" })
                {
                    Term started = await Call(service, "start_link");
                    Check(started is TupleTerm { Items.Count: 2 } pair && pair.Items[0].Equals(Term.A("ok")), "service " + service + ": " + started);
                }

                string probe = Path.Combine(output, (managed ? "managed-" : "original-") + "file-probe");
                string folder = probe + "-dir";
                Ok(await Call("file", "make_dir", Term.String(folder)), "mkdir");
                Ok(await Call(
                    "file",
                    "write_file",
                    Term.String(probe),
                    new BitString(new byte[] { 0, 7, 255 })
                ), "write_file");
                Check(
                    (await Call("file", "read_file", Term.String(probe))).Equals(Term.Tuple(Term.A("ok"), new BitString(new byte[] { 0, 7, 255 }))),
                    "read_file"
                );
                Term meta = await Call("file", "read_file_info", Term.String(probe));
                Check(
                    meta is TupleTerm { Items.Count: 2 } metadata && metadata.Items[0].Equals(Term.A("ok"))
                    && metadata.Items[1] is TupleTerm { Items.Count: 14 } fi && fi.Items[1].Equals(Term.I(3))
                    && fi.Items[2].Equals(Term.A("regular")),
                    "file metadata"
                );
                Check(await Call("file", "get_cwd") is TupleTerm { Items.Count: 2 } cwd && cwd.Items[0].Equals(Term.A("ok")), "getcwd");
                Check(
                    await Call("file", "list_dir", Term.String(output)) is TupleTerm { Items.Count: 2 } listing && listing.Items[0].Equals(Term.A("ok")),
                    "listdir"
                );
                Ok(await Call(
                    "file",
                    "rename",
                    Term.String(folder),
                    Term.String(folder + "-moved")
                ), "directory rename");
                Ok(await Call("file", "del_dir", Term.String(folder + "-moved")), "rmdir");
                Ok(await Call("file", "delete", Term.String(probe)), "unlink");
                foreach (string kind in new[] { "set", "bag", "duplicate_bag" })
                {
                    Term table = Term.A("portable_" + kind);
                    string path = Path.Combine(output, (managed ? "managed-" : "original-") + kind + ".dets");
                    Term options = Term.List(
                        Term.Tuple(Term.A("file"), Term.String(path)),
                        Term.Tuple(Term.A("type"), Term.A(kind)),
                        Term.Tuple(Term.A("auto_save"), Term.A("infinity")),
                        Term.Tuple(Term.A("delayed_write"), Term.Tuple(Term.I(0), Term.I(0)))
                    );
                    Check((await Call(
                        "dets",
                        "open_file",
                        table,
                        options
                    )).Equals(Term.Tuple(Term.A("ok"), table)), "open " + kind);
                    Term[] rows = Enumerable.Range(1, 1200).Select(i => (Term)Term.Tuple(Term.I(i), Term.I(i * i))).ToArray();
                    Ok(await Call(
                        "dets",
                        "insert",
                        table,
                        Cons.From(rows)
                    ), "insert growth");
                    Check((await Call(
                        "dets",
                        "lookup",
                        table,
                        Term.I(1199)
                    )).Equals(Term.List(rows[1198])), "lookup after growth");
                    Check(
                        (await Call(
                            "dets",
                            "insert_new",
                            table,
                            Term.List(Term.Tuple(Term.A("atomic"), Term.I(1)), rows[0])
                        )).Equals(Term.A("false")),
                        "atomic insert_new"
                    );
                    Check((await Call(
                        "dets",
                        "lookup",
                        table,
                        Term.A("atomic")
                    )).Equals(Nil.Value), "no partial insertion");
                    Ok(await Call("dets", "sync", table), "sync");
                    Ok(await Call("dets", "close", table), "close");
                    Check((await Call(
                        "dets",
                        "open_file",
                        table,
                        options
                    )).Equals(Term.Tuple(Term.A("ok"), table)), "reopen");
                    Check((await Call(
                        "dets",
                        "info",
                        table,
                        Term.A("size")
                    )).Equals(Term.I(rows.Length)), "persisted size");
                    Ok(await Call(
                        "dets",
                        "safe_fixtable",
                        table,
                        Term.A("true")
                    ), "fix");
                    Ok(await Call(
                        "dets",
                        "insert",
                        table,
                        Term.Tuple(Term.I(500), Term.I(99))
                    ), "fixed mutation");
                    Ok(await Call(
                        "dets",
                        "safe_fixtable",
                        table,
                        Term.A("false")
                    ), "unfix");
                    Ok(await Call(
                        "dets",
                        "delete",
                        table,
                        Term.I(1)
                    ), "delete");
                    Ok(await Call("dets", "close", table), "final close");

                }
                Check((runtime.Modules.SourceOwner("dets", "open_file", 2) is PreparedBeamImage) != managed, "selected backend identity");
                completion.TrySetResult(Term.I(checks));
            }
            catch (Exception error)
            {
                completion.TrySetException(error);
            }

            return Nil.Value;
        });
        Term result = await completion.Task.WaitAsync(TimeSpan.FromMinutes(5));
        await worker.Completion.WaitAsync(TimeSpan.FromMinutes(5));
        File.WriteAllText(
            Path.Combine(output, managed ? "managed.json" : "original.json"),
            JsonSerializer.Serialize(
                new
                {
                    Backend = managed ? "Managed" : "OriginalBeam",
                    Cases = result.ToString(),
                    Passed = true,
                    OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    RuntimeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location)))
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        foreach (var image in prepared) image.Dispose();
        Console.WriteLine((managed ? "Managed" : "Original") + " DETS: " + result + " checks passed");
    }
}
