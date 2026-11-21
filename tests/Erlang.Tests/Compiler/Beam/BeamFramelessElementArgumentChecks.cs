using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFramelessElementArgumentChecks
{
    internal const string TestName = "compiler/beam/extracts-element-before-effect-frame";
    private const string Fixture = "beam_frameless_element_argument.erl";
    private const string WrongSchedule = "The tuple argument was not extracted before its caller frame.";
    private const string WrongResult = "The tuple argument changed the effect result or exception.";

    private const string Source = """
        -module(beam_frameless_element_argument).
        -export([run/1]).
        run(Input) ->
            _ = effect([{kind,0},{shape,1}], element(1,Input)),
            success.
        effect(Options,stop) -> erlang:error({stopped,Options});
        effect(_,Value) -> Value.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] shadowed = CodeGeneration.CompileBeamModule(
            Source.Replace("-export([run/1]).", "-export([run/1]).\n-compile({no_auto_import,[element/2]}).", StringComparison.Ordinal)
                + "\nelement(_,_) -> good.\n",
            Fixture
        );
        var module = BeamModule.Prepare(image);
        var selection = new RuntimeLibrarySelection([RuntimeModuleSelection.Managed("erlang")]);
        await using var runtime = await ProcessRuntime.CreateAsync(selection, RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (!Term.A("success").Equals(await module.Invoke(context, "run", Term.Tuple(Term.A("good")))))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                await module.Invoke(context, "run", Term.A("not_tuple"));
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A("badarg")))
            {
            }

            try
            {
                await module.Invoke(context, "run", Term.Tuple(Term.A("stop")));
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("stopped"), Term.List(
                    Term.Tuple(Term.A("kind"), Term.I(0)), Term.Tuple(Term.A("shape"), Term.I(1))))))
            {
            }

            return Nil.Value;
        });

        byte[] body = Opcodes(image, "run");
        int extraction = Array.IndexOf(body, BifTwo);
        int frame = Array.IndexOf(body, Allocate);
        if (extraction < 0 || frame <= extraction || Opcodes(shadowed, "run").Contains(BifTwo))
        {
            throw new InvalidOperationException(WrongSchedule);
        }
    }

    private static byte[] Opcodes(byte[] image, string name)
    {
        var module = BeamModule.Prepare(image);
        object code = typeof(BeamModule).GetField(BeamBinaryHostNames.CodeField, System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.NonPublic)!.GetValue(module)!;
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var nameAtom, _]
                && atoms[(int)(BigInteger)Property(nameAtom, "Value")] == name
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        return instructions.Skip(start + 1).Select(Opcode).TakeWhile(opcode => opcode != FunctionInfo).ToArray();
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
