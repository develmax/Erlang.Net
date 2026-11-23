using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWxCallbackOptionChecks
{
    internal const string TestName = "compiler/beam/protects-callback-option-calls-with-branch-local-frames";
    private const string Fixture = "beam_wx_callback_options.erl";
    private const string WrongCommandFrame = "The callback option command retained a constant or local mapper on the stack.";
    private const string WrongMapper = "The callback mapper no longer shares the tuple dispatch and guards the registration call.";
    private const string WrongBoundEnvironment = "The wx environment check lost its equality against an existing binding.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_wx_callback_options).
        -export([enqueue/1]).
        -define(ENV,
            case erlang:get(wx_env) of
                {wx_env,Environment,_,_} -> Environment;
                _ -> erlang:error({wx,unknown_env})
            end).

        enqueue(Options) when is_list(Options) ->
            Command = 951,
            Mapper = fun({style,_}=Option) -> Option;
                        ({action, Callback}) when is_function(Callback) ->
                            {action,wxe_util:get_cbId(Callback)};
                        (Bad) -> erlang:error({badoption,Bad}) end,
            Converted = lists:map(Mapper,Options),
            wxe_util:queue_cmd(Converted,?ENV,Command),
            wxe_util:rec(Command).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        var instructions = Items(code, "Instructions").ToArray();
        object[] command = Body("enqueue");
        object[] mapper = Body("-enqueue/1-fun-0-");

        object[] commandFrames = command.Where(instruction => Opcode(instruction) is Allocate or AllocateHeap).ToArray();
        if (commandFrames.Length != 1 || Opcode(commandFrames[0]) != AllocateHeap
            || Value(Operands(commandFrames[0])[0]) != 0
            || command.Count(instruction => Opcode(instruction) == MakeFun) != 1
            || command.Any(instruction => Opcode(instruction) == InitYRegisters))
        {
            throw new InvalidOperationException(WrongCommandFrame);
        }

        int tuple = Array.FindIndex(mapper, instruction => Opcode(instruction) == IsTuple);
        int select = Array.FindIndex(mapper, instruction => Opcode(instruction) == SelectValue);
        int guard = Array.FindIndex(mapper, instruction => Opcode(instruction) == IsFunction);
        int allocate = Array.FindIndex(mapper, instruction => Opcode(instruction) == Allocate);
        if (tuple < 0 || tuple >= select || select >= guard || guard >= allocate
            || Value(Operands(mapper[allocate])[0]) != 0
            || Value(Operands(mapper[allocate])[1]) != 2
            || Value(Operands(mapper[select])[1]) != Value(Operands(mapper[guard])[0])
            || mapper.Count(instruction => Opcode(instruction) == Allocate) != 1)
        {
            throw new InvalidOperationException(WrongMapper);
        }

        string boundEnvironment = Source.Replace("Environment", "Options", StringComparison.Ordinal);
        code = Code(CodeGeneration.CompileBeamModule(boundEnvironment, Fixture));
        atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        instructions = Items(code, "Instructions").ToArray();
        if (!Body("enqueue").Any(instruction => Opcode(instruction) == ExactEqualTest))
        {
            throw new InvalidOperationException(WrongBoundEnvironment);
        }

        return Task.CompletedTask;

        object[] Body(string name)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name
            );
            if (entry < 0)
            {
                throw new InvalidOperationException(Missing);
            }

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
