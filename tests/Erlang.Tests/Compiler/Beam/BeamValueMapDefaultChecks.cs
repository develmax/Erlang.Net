using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamValueMapDefaultChecks
{
    internal const string TestName = "compiler/beam/lowers-prepared-map-defaults-in-value-positions";
    private const string WrongLayout = "The map default did not retain a distinct cold error with an empty line.";
    private const string WrongValue = "The map default changed eager operand order, a result, or the original error.";
    private const string Source = """
        -module(beam_value_map_default).
        -export([bound/3, computed/3, protected/3, old_catch/3, same_line/4, saved/1]).

        bound(Key, Map, Default) ->
            Value = maps:get(Key, Map, Default),
            erlang:put(saved_value, Value),
            {ok,Value}.

        computed(Key, Map, Default) ->
            Value = maps:get(note(key, Key), note(map, Map), note(default, Default)),
            erlang:put(saved_value, Value),
            {ok,Value}.

        protected(Key, Map, Default) ->
            try maps:get(note(key, Key), note(map, Map), note(default, Default)) of
                Value -> {ok,Value}
            catch
                error:Reason:Stack -> {caught,Reason,Stack}
            end.

        same_line(Key, Map, Default, Input) ->
            erlang:put(before_lookup, true),
            {maps:get(Key, Map, Default), lists:reverse(Input)},
            erlang:put(after_lookup, true),
            ok.

        saved(Map) ->
            First = maps:get(first, Map, none),
            Second = maps:get(second, Map, missing),
            Third = maps:get(third, Map, none),
            tuple(Map, First, Second, Third).
        tuple(Map, First, Second, Third) -> {Map, First, Second, Third}.

        old_catch(Key, Map, Default) ->
            catch maps:get(Key, Map, Default).

        note(Stage, Value) ->
            erlang:put(trace, [Stage|erlang:get(trace)]),
            case Value of
                fail_operand -> erlang:error(argument_failed);
                _ -> Value
            end.
        """;

    private static async Task CheckComputedKey(byte[] image)
    {
        var module = BeamModule.Prepare(image, new BeamDependencySelection(["maps"]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("key");
            Term value = Term.A("value");
            Term fallback = Term.A("fallback");
            Term traceKey = Term.A("trace");
            Term saved = Term.A("saved_value");
            Term expectedTrace = Term.List(Term.A("default"), Term.A("map"), key);
            var map = new MapTerm([new(key, value)]);
            foreach (var (lookup, expected) in new[] { (key, value), (Term.A("missing"), fallback) })
            {
                context.Dictionary[traceKey] = Nil.Value;
                Term result = await module.Invoke(
                    context,
                    "computed",
                    lookup,
                    map,
                    fallback
                );
                if (!Term.Tuple(Term.A("ok"), expected).Equals(result)
                    || !expectedTrace.Equals(context.Dictionary[traceKey])
                    || !expected.Equals(context.Dictionary[saved]))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            return Nil.Value;
        });
    }


    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_value_map_default.erl");
        await CheckComputedKey(image);
        CheckLayout(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);

        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("key");
            Term missing = Term.A("missing");
            Term value = Term.A("value");
            Term fallback = Term.A("fallback");
            Term invalid = Term.Tuple(Term.A("bad_source"), Term.I(9));
            Term failure = Term.A("fail_operand");
            Term stageTrace = Term.A("trace");
            Term saved = Term.A("saved_value");
            Term trace = Term.List(Term.A("default"), Term.A("map"), key);
            Term map = new MapTerm([new(key, value)]);
            if (!Term.Tuple(Term.A("ok"), value).Equals(await module.Invoke(
                context,
                "bound",
                key,
                map,
                fallback
            )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            await Check(
                key,
                map,
                fallback,
                value
            );
            await Check(
                missing,
                map,
                fallback,
                fallback
            );
            await Reject(
                key,
                invalid,
                fallback,
                Term.Tuple(Term.A(ErlangErrorReasons.BadMap), invalid),
                trace
            );
            await Reject(
                key,
                map,
                failure,
                Term.A("argument_failed"),
                trace
            );
            await Reject(
                failure,
                map,
                fallback,
                Term.A("argument_failed"),
                Term.List(key)
            );
            await Reject(
                key,
                failure,
                fallback,
                Term.A("argument_failed"),
                Term.List(Term.A("map"), key)
            );

            context.Dictionary[stageTrace] = Nil.Value;
            Term caught = await module.Invoke(
                context,
                "protected",
                key,
                invalid,
                fallback
            );
            if (caught is not TupleTerm
                {
                    Items: [Atom { Name: "caught" }, var reason, Cons
                    {
                        Head: TupleTerm { Items: [Atom { Name: "beam_value_map_default" }, Atom { Name: "protected" }, _, _] }
                    }]
                }
                || !reason.Equals(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), invalid))
                || !trace.Equals(context.Dictionary[stageTrace]))
            {
                throw new InvalidOperationException(WrongValue);
            }

            context.Dictionary[stageTrace] = Nil.Value;
            Term protectedValue = await module.Invoke(
                context,
                "protected",
                missing,
                map,
                fallback
            );
            if (!Term.Tuple(Term.A("ok"), fallback).Equals(protectedValue) || !trace.Equals(context.Dictionary[stageTrace]))
            {
                throw new InvalidOperationException(WrongValue);
            }

            await CheckSameLine();
            Term savedMap = new MapTerm([new(Term.A("second"), value)]);
            Term none = Term.A("none");
            if (!Term.Tuple(
                savedMap,
                none,
                value,
                none
            ).Equals(await module.Invoke(context, "saved", savedMap)))
            {
                throw new InvalidOperationException(WrongValue);
            }

            await CheckSavedFailure();

            return Nil.Value;

            async Task CheckSavedFailure()
            {
                try
                {
                    _ = await module.Invoke(context, "saved", invalid);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), invalid))
                    && error.StackTraceTerm is Cons
                    {
                        Head: TupleTerm
                        {
                            Items: [Atom { Name: "beam_value_map_default" }, Atom { Name: "saved" }, _, Nil]
                        }
                    })
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }

            async Task CheckSameLine()
            {
                int line = Array.FindIndex(Source.Split('\n'), row => row.Contains("lists:reverse(Input)", StringComparison.Ordinal)) + 1;
                Term expected = Term.Tuple(Term.A("line"), Term.I(line));
                try
                {
                    _ = await module.Invoke(
                        context,
                        "same_line",
                        key,
                        map,
                        fallback,
                        invalid
                    );
                }
                catch (ErlangException error)
                {
                    if (Cons.Items(error.StackTraceTerm).Any(frame => frame is TupleTerm
                        {
                            Items: [Atom { Name: "beam_value_map_default" }, Atom { Name: "same_line" }, _, var position]
                        } && Cons.Items(position).Contains(expected))
                    && context.Dictionary.ContainsKey(Term.A("before_lookup"))
                    && !context.Dictionary.ContainsKey(Term.A("after_lookup")))
                    {
                        return;
                    }
                }

                throw new InvalidOperationException(WrongValue);
            }

            async Task Check(
                Term lookup,
                Term input,
                Term defaultValue,
                Term expected
            )
            {
                context.Dictionary[stageTrace] = Nil.Value;
                Term result = await module.Invoke(
                    context,
                    "computed",
                    lookup,
                    input,
                    defaultValue
                );
                if (!Term.Tuple(Term.A("ok"), expected).Equals(result)
                    || !trace.Equals(context.Dictionary[stageTrace]) || !expected.Equals(context.Dictionary[saved]))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            async Task Reject(
                Term lookup,
                Term input,
                Term defaultValue,
                Term reason,
                Term expectedTrace
            )
            {
                context.Dictionary[stageTrace] = Nil.Value;
                context.Dictionary.Remove(saved);
                try
                {
                    _ = await module.Invoke(
                        context,
                        "computed",
                        lookup,
                        input,
                        defaultValue
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(reason))
                {
                    if (expectedTrace.Equals(context.Dictionary[stageTrace]) && !context.Dictionary.ContainsKey(saved))
                    {
                        return;
                    }
                }

                throw new InvalidOperationException(WrongValue);
            }
        });
    }

    private static void CheckLayout(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        object structure = Property(code, "Structure");
        string[] atoms = Items(structure, "Atoms").Cast<string>().ToArray();
        object[] imports = Items(structure, "Imports");
        foreach (string function in new[] { "bound", "computed", "same_line", "saved" })
        {
            object[] body = Body(function);
            int test = Array.FindIndex(body, item => Opcode(item) == IsMap);
            int projection = Array.FindIndex(body, item => Opcode(item) == GetMapElements);
            int error = Array.FindIndex(body, item => IsRemote(
                item,
                "erlang",
                "error",
                1
            ));
            if (test < 0 || projection <= test || error <= projection || Opcode(body[error - 1]) != Line
                || Value(Operands(body[error - 1])[0]) != 0
                || body.Any(item => IsRemote(
                    item,
                    "maps",
                    "get",
                    3
                )))
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        object[] protectedBody = Body("protected");
        int enter = Array.FindIndex(protectedBody, item => Opcode(item) == TryEnter);
        int exit = Array.FindIndex(protectedBody, item => Opcode(item) == TryEnd);
        int protectedProjection = Array.FindIndex(protectedBody, item => Opcode(item) == GetMapElements);
        int protectedError = Array.FindIndex(protectedBody, item => IsRemote(
            item,
            "erlang",
            "error",
            1
        ));
        if (enter < 0 || protectedProjection <= enter || exit <= protectedProjection || protectedError <= exit
            || Opcode(protectedBody[protectedError]) != ExternalCall
            || Opcode(protectedBody[protectedError - 1]) != Line || Value(Operands(protectedBody[protectedError - 1])[0]) != 0
            || protectedBody.Any(item => IsRemote(
                item,
                "maps",
                "get",
                3
            ))
            || !Body("old_catch").Any(item => IsRemote(
                item,
                "maps",
                "get",
                3
            )))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] Body(string function)
        {
            int start = Array.FindIndex(instructions, item => Opcode(item) == FunctionInfo
                && atoms[Value(Operands(item)[1])] == function);
            if (start < 0)
            {
                throw new InvalidOperationException(WrongLayout);
            }

            return instructions.Skip(start + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
        }

        bool IsRemote(
            object item,
            string module,
            string function,
            int arity
        )
        {
            if (Opcode(item) is not (ExternalCall or TailExternalCall or TailExternalCallWithFrame))
            {
                return false;
            }

            object[] operands = Operands(item);
            object imported = imports[Value(operands[1])];

            return Value(operands[0]) == arity
                && atoms[(int)Property(imported, "ModuleAtomIndex")] == module
                && atoms[(int)Property(imported, "FunctionAtomIndex")] == function;
        }
    }

    private static object[] Items(object value, string name) =>
        ((IEnumerable)Property(value, name)).Cast<object>().ToArray();

    private static object[] Operands(object value) => Items(value, "Operands");

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
