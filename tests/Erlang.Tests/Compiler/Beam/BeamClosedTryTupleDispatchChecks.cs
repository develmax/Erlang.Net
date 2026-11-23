using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedTryTupleDispatchChecks
{
    internal const string TestName = "compiler/beam/keeps-closed-tuple-match-outside-protected-dispatch";
    private const string Fixture = "beam_closed_try_tuple_dispatch.erl";
    private const string WrongSchedule = "The closed tuple dispatch did not preserve its expected entry and protection schedule.";
    private const string WrongResult = "The closed tuple dispatch changed its Erlang result or exception.";

    private const string Source = """
        -module(beam_closed_try_tuple_dispatch).
        -export([run/1]).
        run(Data) -> handle(token, Data, toggle).
        handle(Type, Input, Option) ->
            {Data,_Rest} = read(Option,Input),
            try {ok,dispatch(Type,Data)}
            catch
                Class:Exception:Stk when Class =:= error; Class =:= exit ->
                    case Exception of
                        {error,{asn1,Reason}} -> {error,{asn1,{Reason,Stk}}};
                        Reason -> {error,{asn1,{Reason,Stk}}}
                    end
            end.
        read(toggle,zero) -> absent;
        read(toggle,Input) -> {Input,ignored}.
        dispatch(token,Value) -> execute(Value).
        execute(wrapped) -> erlang:error({error,{asn1,wrapped}});
        execute(exiting) -> exit(plain);
        execute(thrown) -> throw(bubbled);
        execute(Value) -> Value.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] exported = CodeGeneration.CompileBeamModule(
            Source.Replace("[run/1]", "[run/1, handle/3]", StringComparison.Ordinal),
            Fixture
        );
        byte[] wider = CodeGeneration.CompileBeamModule(
            Source.Replace("read(toggle,zero) -> absent;", "read(toggle,zero) -> absent; read(toggle,none) -> other;", StringComparison.Ordinal),
            Fixture
        );
        var prepared = BeamModule.Prepare(image);
        var selection = new RuntimeLibrarySelection([RuntimeModuleSelection.Managed("erlang")]);
        await using var runtime = await ProcessRuntime.CreateAsync(selection, RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (!Term.Tuple(Term.A("ok"), Term.A("plain")).Equals(await prepared.Invoke(context, "run", Term.A("plain"))))
            {
                throw new InvalidOperationException(WrongResult);
            }

            foreach (var (input, reason) in new[] { ("wrapped", "wrapped"), ("exiting", "plain") })
            {
                Term result = await prepared.Invoke(context, "run", Term.A(input));
                if (result is not TupleTerm outer || !outer.Items[0].Equals(Term.A("error"))
                    || outer.Items[1] is not TupleTerm inner || !inner.Items[0].Equals(Term.A("asn1"))
                    || inner.Items[1] is not TupleTerm data || !data.Items[0].Equals(Term.A(reason)) || data.Items[1] is not Cons)
                {
                    throw new InvalidOperationException(WrongResult);
                }
            }

            try
            {
                await prepared.Invoke(context, "run", Term.A("zero"));
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("badmatch"), Term.A("absent"))))
            {
            }

            try
            {
                await prepared.Invoke(context, "run", Term.A("thrown"));
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Throw
                && error.Reason.Equals(Term.A("bubbled")))
            {
            }

            return Nil.Value;
        });

        byte[] body = Opcodes(image, "handle");
        if (!body.Contains(SelectValue) || !body.Contains(TryEnter) || body.Contains(TestArity)
            || Opcodes(image, "dispatch").Contains(ExactEqualTest)
            || !Opcodes(exported, "dispatch").Contains(ExactEqualTest)
            || !Opcodes(wider, "handle").Contains(TestArity))
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
