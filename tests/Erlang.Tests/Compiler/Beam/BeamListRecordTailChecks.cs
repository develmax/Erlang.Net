using System.Text;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamListRecordTailChecks
{
    internal static async Task Run()
    {
        const string file = "beam_list_record_tail_probe.erl";
        string source = Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(file));
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, file));
        await using var runtime = new ProcessRuntime();
        Term payload = Term.A("payload");
        Term missing = Term.A("undefined");
        Term original = Term.Tuple(
            Term.A("state"),
            payload,
            missing,
            missing,
            Term.A("false")
        );
        Term updated = Term.Tuple(
            Term.A("state"),
            payload,
            missing,
            missing,
            Term.A("true")
        );
        Term reversed = Term.List(Term.A("b"), Term.A("a"));
        Term marker = Term.A("marker");
        Term invalid = Term.A("bad");
        bool verified = false;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term mixed = await image.Invoke(
                context,
                "run",
                Term.List(Term.A("a"), marker, Term.A("b")),
                payload
            );
            Term ordinary = await image.Invoke(
                context,
                "run",
                Term.List(Term.A("a"), Term.A("b")),
                payload
            );
            Term empty = await image.Invoke(
                context,
                "run",
                Nil.Value,
                payload
            );
            Term uncheckedEmpty = await image.Invoke(
                context,
                "unchecked",
                Nil.Value,
                invalid
            );
            try
            {
                await image.Invoke(
                    context,
                    "unchecked",
                    Term.List(marker),
                    invalid
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badrecord"), invalid)))
            {
                verified = mixed.Equals(Term.Tuple(original, Term.Tuple(reversed, updated)))
                    && ordinary.Equals(Term.Tuple(original, Term.Tuple(reversed, original)))
                    && empty.Equals(Term.Tuple(original, Term.Tuple(Nil.Value, original)))
                    && uncheckedEmpty.Equals(Term.Tuple(Nil.Value, invalid));
            }

            return Nil.Value;
        });
        Term exit = await process.Completion;
        if (!verified)
        {
            throw new InvalidOperationException($"The list record continuation did not preserve its result or record check. Exit {exit}.");
        }
    }
}
