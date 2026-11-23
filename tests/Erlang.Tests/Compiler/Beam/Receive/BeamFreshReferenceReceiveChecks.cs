using System.Collections;
using System.Reflection;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamFreshReferenceReceiveChecks
{
    internal const string TestName = "compiler/beam/fresh-reference-receive-marker";
    private const string BadMarkerSequence = "Expected a marker around the fresh reference's successful receive block.";
    private const string UnsafeMarker = "Unexpected marker for a disabled or unproven receive.";
    private const string ChangedOptions = "Recording the receive opt-out changed its source directive or recorded build options.";
    private const string ChangedMailbox = "Fresh-reference receive changed mailbox order or selected the wrong message.";
    private const string Module = "beam_fresh_reference_receive";
    private const string Source = """
        -module(beam_fresh_reference_receive).
        %% options
        -export([run/1]).
        run(Value) ->
            Ref = make_ref(),
            Self = self(),
            Message = {reply, Ref, Value},
            Self ! Message,
            receive
                {reply, Ref, Result} -> Result
            end.
        """;

    private const string MaterializedSource = """
        -module(beam_fresh_reference_receive).
        -export([run/1]).
        run(Value) ->
            Ref = make_ref(),
            Self = self(),
            Message = {reply, Self, Ref, Value},
            Recipient = recipient(),
            erlang:send(Recipient, Message),
            receive {reply, Recipient, Ref, Result} -> Result end.

        recipient() ->
            erlang:garbage_collect(),
            self().
        """;

    internal static async Task Run()
    {
        byte[] enabled = Compile(Source);
        byte[] nestedSend = Compile(Source.Replace(
            "    Self = self(),\n    Message = {reply, Ref, Value},\n    Self ! Message,",
            "    self() ! {reply, Ref, Value},",
            StringComparison.Ordinal
        ));
        RequireMarkerSequence(enabled);
        RequireMarkerSequence(nestedSend);
        RequireDirectRegisterConstruction(nestedSend);

        string optOutSource = Source.Replace("%% options", "-compile(no_recv_opt).", StringComparison.Ordinal);
        byte[] disabled = Compile(optOutSource);
        byte[] moduleDisabled = Compile(Source.Replace("%% options", "-compile(no_module_opt).", StringComparison.Ordinal));
        byte[] shadowed = Compile(Source.Replace(
            "%% options",
            "-compile({no_auto_import, [make_ref/0]}).",
            StringComparison.Ordinal
        ) + "\nmake_ref() -> get(previous).\n");
        byte[] unrelated = Compile(Source.Replace(
            "{reply, Ref, Result} -> Result",
            "{reply, Ref, Result} -> Result; _ -> unrelated",
            StringComparison.Ordinal
        ));

        byte[] parameterMatch = Compile(Source.Replace("    Self = self(),", "    Value = incompatible,\n    Self = self(),",
            StringComparison.Ordinal));
        byte[] repeatedMatch = Compile(Source.Replace(
            "    Self = self(),",
            "    Same = Value,\n    Same = incompatible,\n    Self = self(),",
            StringComparison.Ordinal
        ));

        CheckRecordedOptions(optOutSource);

        foreach (byte[] negative in new[] { disabled, moduleDisabled, shadowed, unrelated, parameterMatch, repeatedMatch })
        {
            if (Instructions(negative).Any(opcode => opcode is
                ReceiveMarkerReserve or ReceiveMarkerBind or ReceiveMarkerUse or ReceiveMarkerClear))
            {
                throw new InvalidOperationException(UnsafeMarker);
            }
        }

        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await CheckMaterializedSend(runtime, actor);
        foreach (byte[] candidate in new[] { enabled, nestedSend, disabled, moduleDisabled, shadowed, unrelated })
        {
            BeamModule beam = BeamModule.Prepare(candidate);
            await actor.InvokeAsync(async context =>
            {
                Term previous = runtime.MakeReference();
                Term old = Term.A("old");
                Term current = Term.A("new");
                Term priorMessage = Term.Tuple(Term.A("reply"), previous, old);
                context.Dictionary[Term.A("previous")] = previous;
                context.Mailbox.Send(priorMessage);
                Term actual = await beam.Invoke(context, "run", current);
                bool shadow = ReferenceEquals(candidate, shadowed);
                bool catchall = ReferenceEquals(candidate, unrelated);
                Term expected = shadow ? old : catchall ? Term.A("unrelated") : current;
                Term? leftover = await context.ReceiveAsync(value => value, TimeSpan.Zero);
                bool correctLeftover = shadow ? Term.Tuple(Term.A("reply"), previous, current).Equals(leftover)
                    : catchall ? leftover is TupleTerm message && message.Items is [Atom { Name: "reply" }, ReferenceTerm, var payload]
                        && payload.Equals(current)
                    : priorMessage.Equals(leftover);
                if (!actual.Equals(expected) || !correctLeftover
                    || await context.ReceiveAsync(value => value, TimeSpan.Zero) is not null)
                {
                    throw new InvalidOperationException(ChangedMailbox);
                }

                return Nil.Value;
            });
        }

        foreach (byte[] candidate in new[] { parameterMatch, repeatedMatch })
        {
            BeamModule beam = BeamModule.Prepare(candidate);
            await actor.InvokeAsync(async context =>
            {
                try
                {
                    _ = await beam.Invoke(context, "run", Term.A("new"));
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), Term.A("incompatible"))))
                {
                    return Nil.Value;
                }

                throw new InvalidOperationException(ChangedMailbox);
            });
        }
    }

    private static async Task CheckMaterializedSend(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor)
    {
        byte[] reusable = Compile(MaterializedSource);
        byte[] retainedInput = Compile(MaterializedSource.Replace(
            "{reply, Recipient, Ref, Result} -> Result",
            "{reply, Recipient, Ref, Value} -> Value",
            StringComparison.Ordinal
        ));
        if (!HasMaterializedRegisterHandoff(reusable) || HasMaterializedRegisterHandoff(retainedInput))
        {
            throw new InvalidOperationException(BadMarkerSequence);
        }

        foreach (byte[] candidate in new[] { reusable, retainedInput })
        {
            BeamModule beam = BeamModule.Prepare(candidate);
            await actor.InvokeAsync(async context =>
            {
                Term current = Term.A("new");
                Term previous = Term.Tuple(
                    Term.A("reply"),
                    context.Self,
                    runtime.MakeReference(),
                    Term.A("old")
                );
                context.Mailbox.Send(previous);
                Term actual = await beam.Invoke(context, "run", current);
                Term? leftover = await context.ReceiveAsync(value => value, TimeSpan.Zero);
                if (!actual.Equals(current) || !previous.Equals(leftover)
                    || await context.ReceiveAsync(value => value, TimeSpan.Zero) is not null)
                {
                    throw new InvalidOperationException(ChangedMailbox);
                }

                return Nil.Value;
            });
        }
    }

    private static bool HasMaterializedRegisterHandoff(byte[] beam)
    {
        object[] decoded = DecodeInstructions(beam).Where(instruction => Opcode(instruction) != Line).ToArray();
        byte[] opcodes = decoded.Select(Opcode).ToArray();
        ReadOnlySpan<byte> handoff =
        [
            ReceiveMarkerReserve, ExternalCall, TestHeap, BifZero, PutTuple, ReceiveMarkerBind,
            Move, LocalCall, Move, Move, ExternalCall, ReceiveMarkerUse
        ];
        int start = opcodes.AsSpan().IndexOf(handoff);
        int frame = Array.IndexOf(opcodes, Allocate);
        if (start < 0 || frame < 0 || OperandValue(decoded[frame], 0) != 2 || OperandValue(decoded[start + 2], 1) != 1)
        {
            return false;
        }

        int message = OperandValue(decoded[start + 4], 0);
        int reference = OperandValue(decoded[start], 0);

        return message != reference
            && OperandValue(decoded[start + 6], 1) == reference
            && OperandValue(decoded[start + 8], 0) == message
            && OperandValue(decoded[start + 8], 1) == 1
            && OperandValue(decoded[start + 9], 1) == message
            && OperandValue(decoded[start + 11], 0) == reference;
    }

    private static void CheckRecordedOptions(string source)
    {
        var directory = Directory.CreateTempSubdirectory("erlang-beam-fresh-reference-");
        try
        {
            string path = Path.Combine(directory.FullName, Module + ".erl");
            File.WriteAllText(path, source);
            var options = new Term[] { Term.A("debug_info") };
            var profile = new BeamReleaseBuildProfile(
                "10.0.6",
                path,
                directory.FullName,
                [],
                [],
                RecordedCompileOptions: options,
                RecordedDebugOptions: options
            );
            byte[] beam = CodeGeneration.CompileBeamFile(path, buildProfile: profile);
            var chunks = BeamDocumentationEmissionChecks.Chunks(beam);
            Term compile = ExternalTermFormat.Decode(chunks.Single(chunk => chunk.Id == "CInf").Data);
            byte[] debug = chunks.Single(chunk => chunk.Id == "Dbgi").Data;
            Term abstractCode = ExternalTermFormat.Decode(BeamDocumentationEmissionChecks.RawEtf(debug));
            if (!Cons.Items(compile).Contains(Term.Tuple(Term.A("options"), Cons.From(options)))
                || abstractCode is not TupleTerm { Items: [_, _, TupleTerm { Items: [var forms, var debugOptions] }] }
                || !Cons.Items(debugOptions).SequenceEqual(options)
                || !Cons.Items(forms).Any(form => form is TupleTerm
                {
                    Items: [Atom { Name: "attribute" }, _, Atom { Name: "compile" },
                        Atom { Name: CompilerOptionNames.DisableReceiveOptimization }]
                }))
            {
                throw new InvalidOperationException(ChangedOptions);
            }
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static void RequireMarkerSequence(byte[] beam)
    {
        byte[] opcodes = Instructions(beam);
        int reserve = Array.IndexOf(opcodes, ReceiveMarkerReserve);
        int bind = Array.IndexOf(opcodes, ReceiveMarkerBind);
        int use = Array.IndexOf(opcodes, ReceiveMarkerUse);
        int clear = Array.IndexOf(opcodes, ReceiveMarkerClear);
        int send = Array.IndexOf(opcodes, Send);
        int receive = Array.IndexOf(opcodes, LoopReceive);
        int remove = Array.IndexOf(opcodes, RemoveMessage);
        if (reserve < 0 || !(reserve < bind && bind < send && send < use && use < receive && receive < clear
            && clear + 1 == remove) || !opcodes.AsSpan(reserve + 1, bind - reserve - 1).Contains(PutTuple))
        {
            throw new InvalidOperationException(BadMarkerSequence);
        }
    }

    private static void RequireDirectRegisterConstruction(byte[] beam)
    {
        object[] decoded = DecodeInstructions(beam).Where(instruction => Opcode(instruction) != Line).ToArray();
        byte[] opcodes = decoded.Select(Opcode).ToArray();
        ReadOnlySpan<byte> constructor = [ReceiveMarkerReserve, ExternalCall, TestHeap, BifZero, PutTuple, ReceiveMarkerBind];
        int start = opcodes.AsSpan().IndexOf(constructor);
        int frame = Array.IndexOf(opcodes, Allocate);
        if (start < 0 || frame < 0 || OperandValue(decoded[frame], 0) != 2 || OperandValue(decoded[start + 2], 1) != 1)
        {
            throw new InvalidOperationException(BadMarkerSequence);
        }
    }

    private static int OperandValue(object instruction, int index)
    {
        var operands = (IEnumerable)instruction.GetType().GetProperty("Operands")!.GetValue(instruction)!;
        object operand = operands.Cast<object>().ElementAt(index);

        return (int)(System.Numerics.BigInteger)operand.GetType().GetProperty("Value")!.GetValue(operand)!;
    }

    private static byte[] Compile(string source) => CodeGeneration.CompileBeamModule(source, Module + ".erl");

    private static byte[] Instructions(byte[] beam) => DecodeInstructions(beam).Select(Opcode).ToArray();

    private static byte Opcode(object instruction)
    {
        object definition = instruction.GetType().GetProperty(BeamOnLoadMutationParameters.Definition)!.GetValue(instruction)!;

        return (byte)definition.GetType().GetProperty("Opcode")!.GetValue(definition)!;
    }

    private static IEnumerable<object> DecodeInstructions(byte[] beam)
    {
        Assembly assembly = typeof(BeamModule).Assembly;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        Type reader = assembly.GetType(BeamOnLoadMutationParameters.StructureType)!;
        object structure = reader.GetMethod(BeamOnLoadMutationParameters.Read, flags)!
            .Invoke(null, [new ReadOnlyMemory<byte>(beam), null])!;
        Type decoder = assembly.GetType(BeamOnLoadMutationParameters.InstructionType)!;

        return ((IEnumerable)decoder.GetMethod(BeamOnLoadMutationParameters.Read, flags)!
            .Invoke(null, [structure])!).Cast<object>();
    }
}
