using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedAttemptCarrierChecks
{
    internal const string TestName = "compiler/beam/returns-the-checked-carrier-after-the-accepted-attempt";
    private const string WrongSchedule = "The accepted attempt should return the original checked carrier.";
    private const string WrongResult = "The attempt or its failed-result cleanup changed behavior.";
    private const string Source = """
        -module(beam_checked_attempt_carrier).
        -export([run/1, changed_tag/1, computed/1, computed_changed_tag/1]).

        run(Input) ->
            case produce(Input) of
                {resource, Payload} ->
                    case attempt(Payload) of
                        accepted -> {resource, Payload};
                        Failed -> cleanup(Payload), Failed
                    end;
                Other -> Other
            end.

        changed_tag(Input) ->
            case produce(Input) of
                {resource, Payload} ->
                    case attempt(Payload) of
                        accepted -> {different, Payload};
                        Failed -> cleanup(Payload), Failed
                    end;
                Other -> Other
            end.

        computed(Input) ->
            case produce(compute(Input)) of
                {resource, Payload} ->
                    case attempt(Payload) of
                        accepted -> {resource, Payload};
                        Failed -> cleanup(Payload), Failed
                    end;
                Other -> Other
            end.

        computed_changed_tag(Input) ->
            case produce(compute(Input)) of
                {resource, Payload} ->
                    case attempt(Payload) of
                        accepted -> {different, Payload};
                        Failed -> cleanup(Payload), Failed
                    end;
                Other -> Other
            end.

        compute({compute_error, Reason}) -> erlang:error(Reason);
        compute(Input) ->
            erlang:put(computed_input, [Input | erlang:get(computed_input)]),
            Input.

        produce({present, Input}) -> {resource, Input};
        produce(Other) -> Other.

        attempt({ready, _}) -> accepted;
        attempt({fail, Reason}) -> {error, Reason};
        attempt({bad_cleanup, Reason}) -> {error, Reason};
        attempt({bad_attempt, Reason}) -> erlang:error(Reason).

        cleanup({bad_cleanup, Reason}) -> erlang:error(Reason);
        cleanup(_) -> ok.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "beam_checked_attempt_carrier.erl");
        CheckSchedule(bytes);
        BeamModule module = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term token = Term.A("token");
            Term ready = Term.Tuple(Term.A("ready"), token);
            if (!Term.Tuple(Term.A("resource"), ready).Equals(await module.Invoke(context, "run", Present(ready)))
                || !Term.Tuple(Term.A("different"), ready).Equals(await module.Invoke(context, "changed_tag", Present(ready)))
                || !Term.Tuple(Term.A("error"), token).Equals(await module.Invoke(context, "run", Present(Term.Tuple(Term.A("fail"), token))))
                || !token.Equals(await module.Invoke(context, "run", token)))
            {
                throw new InvalidOperationException(WrongResult);
            }

            Term computedKey = Term.A("computed_input");
            Term[] computedInputs = [Present(ready), Present(Term.Tuple(Term.A("fail"), token)), token, Present(ready)];
            Term[] computedResults = [Term.Tuple(Term.A("resource"), ready), Term.Tuple(Term.A("error"), token),
                token, Term.Tuple(Term.A("different"), ready)];
            string[] computedFunctions = ["computed", "computed", "computed", "computed_changed_tag"];
            for (int index = 0; index < computedInputs.Length; index++)
            {
                context.Dictionary[computedKey] = Nil.Value;
                if (!computedResults[index].Equals(await module.Invoke(context, computedFunctions[index], computedInputs[index]))
                    || !Term.List(computedInputs[index]).Equals(context.Dictionary[computedKey]))
                {
                    throw new InvalidOperationException(WrongResult);
                }
            }

            Term computeError = Term.A("compute_failed");
            try
            {
                _ = await module.Invoke(context, "computed", Term.Tuple(Term.A("compute_error"), computeError));
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error) when (error.Reason.Equals(computeError))
            {
            }

            await RequireException("bad_attempt", Term.A("attempt_failed"));
            await RequireException("bad_cleanup", Term.A("cleanup_failed"));

            return Nil.Value;

            async Task RequireException(string branch, Term reason)
            {
                try
                {
                    _ = await module.Invoke(context, "run", Present(Term.Tuple(Term.A(branch), reason)));
                }
                catch (ErlangException error) when (error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }
        });

        static Term Present(Term value) => Term.Tuple(Term.A("present"), value);
    }

    private static void CheckSchedule(byte[] bytes)
    {
        object code = Code(bytes);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (Body("run").Any(instruction => Opcode(instruction) == PutTuple)
            || !Body("changed_tag").Any(instruction => Opcode(instruction) == PutTuple)
            || Body("computed").Any(instruction => Opcode(instruction) == PutTuple)
            || !Body("computed_changed_tag").Any(instruction => Opcode(instruction) == PutTuple))
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        IEnumerable<object> Body(string function)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands")[1], "Value")] == function
            );
            if (start < 0)
            {
                throw new InvalidOperationException(WrongSchedule);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
