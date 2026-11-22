using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalMapDefaultChecks
{
    internal const string TestName = "compiler/beam/lowers-prepared-terminal-map-default-lookups";
    private const string WrongLayout = "The terminal lookup did not keep separate key-miss and generated badmap edges.";
    private const string WrongValue = "The terminal lookup changed an exact key, default, or original failure value.";
    private const string Source = """
        -module(beam_terminal_map_default).
        -export([lookup/3, from_box/3, identity_default/2, literal_key/1, computed/3, branch/3, dynamic/4, confirmed/3]).
        -import(maps, [get/3]).
        -record(box, {map, ignored}).
        lookup(Key, Map, Default) -> maps:get(Key, Map, Default).
        from_box(#box{map = Map}, Key, Default) -> get(Key, Map, Default).
        identity_default(Key, Map) -> maps:get(Key, Map, Key).
        literal_key(Map) -> maps:get(absent, Map, []).
        computed(Key, Map, Default) -> maps:get(Key, Map, note(Default)).
        branch(read, Key, Map) -> maps:get(Key, Map, fallback);
        branch(_, Key, Map) -> alternate(Key, Map).
        confirmed(read, Key, Map) when is_map(Map) -> maps:get(Key, Map, fallback);
        confirmed(_, Key, Map) -> alternate(Key, Map).
        dynamic(Module, Key, Map, Default) -> Module:get(Key, Map, Default).
        alternate(Key, Map) -> {Key,Map}.
        note(Default) ->
            erlang:put(last_default, Default),
            Default.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_terminal_map_default.erl");
        CheckLayout(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);

        await actor.InvokeAsync(async context =>
        {
            Term integer = Term.I(1);
            Term floating = new FloatTerm(1.0);
            Term integerValue = Term.A("integer");
            Term floatValue = Term.A("floating");
            Term missing = Term.A("missing");
            Term invalidMap = Term.Tuple(Term.A("invalid"), Term.I(7));
            Term fallback = Term.A("fallback");
            Term map = new MapTerm([new(integer, integerValue), new(floating, floatValue)]);
            Term box = Term.Tuple(Term.A("box"), map, Nil.Value);
            if (!integerValue.Equals(await module.Invoke(
                context,
                "lookup",
                integer,
                map,
                fallback
            ))
                || !floatValue.Equals(await module.Invoke(
                    context,
                    "lookup",
                    floating,
                    map,
                    fallback
                ))
                || !fallback.Equals(await module.Invoke(
                    context,
                    "lookup",
                    missing,
                    map,
                    fallback
                ))
                || !missing.Equals(await module.Invoke(
                    context,
                    "identity_default",
                    missing,
                    map
                ))
                || !floatValue.Equals(await module.Invoke(
                    context,
                    "from_box",
                    box,
                    floating,
                    fallback
                ))
                || !Nil.Value.Equals(await module.Invoke(context, "literal_key", map)))
            {
                throw new InvalidOperationException(WrongValue);
            }

            await Reject(
                "lookup",
                Term.Tuple(Term.A(ErlangErrorReasons.BadMap), invalidMap),
                integer,
                invalidMap,
                fallback
            );
            await Reject(
                "from_box",
                Term.Tuple(Term.A(ErlangErrorReasons.BadMap), invalidMap),
                Term.Tuple(Term.A("box"), invalidMap, Nil.Value),
                integer,
                fallback
            );
            await Reject(
                "from_box",
                Term.A(ErlangErrorReasons.FunctionClause),
                Term.Tuple(Term.A("other"), invalidMap, Nil.Value),
                integer,
                fallback
            );

            Term defaultEffect = Term.A("last_default");
            await RejectTerminal(
                "computed",
                integer,
                invalidMap,
                fallback
            );
            if (!fallback.Equals(context.Dictionary[defaultEffect])
                || !floatValue.Equals(await module.Invoke(
                    context,
                    "computed",
                    floating,
                    map,
                    integer
                ))
                || !integer.Equals(context.Dictionary[defaultEffect])
                || !fallback.Equals(await module.Invoke(
                    context,
                    "branch",
                    Term.A("read"),
                    missing,
                    map
                ))
                || !Term.Tuple(integer, invalidMap).Equals(await module.Invoke(
                    context,
                    "branch",
                    Term.A("alternate"),
                    integer,
                    invalidMap
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            await RejectTerminal(
                "branch",
                Term.A("read"),
                integer,
                invalidMap
            );
            if (!floatValue.Equals(await module.Invoke(
                context,
                "confirmed",
                Term.A("read"),
                floating,
                map
            )) || !fallback.Equals(await module.Invoke(
                context,
                "confirmed",
                Term.A("read"),
                missing,
                map
            )) || !Term.Tuple(integer, invalidMap).Equals(await module.Invoke(
                context,
                "confirmed",
                Term.A("read"),
                integer,
                invalidMap
            )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;

            async Task RejectTerminal(string function, params Term[] operands)
            {
                try
                {
                    _ = await module.Invoke(context, function, operands);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), invalidMap))
                    && error.StackTraceTerm is Cons
                    {
                        Head: TupleTerm
                        {
                            Items: [Atom { Name: "beam_terminal_map_default" }, Atom name, Integer { Value: var arity }, Nil]
                        }
                    } && name.Name == function && arity == operands.Length)
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }

            async Task Reject(string function, Term reason, params Term[] operands)
            {
                try
                {
                    _ = await module.Invoke(context, function, operands);
                }
                catch (ErlangException error) when (error.Reason.Equals(reason))
                {
                    return;
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
        foreach (string function in new[] { "lookup", "from_box", "identity_default", "literal_key", "computed", "branch" })
        {
            object[] body = Body(function);
            int map = Array.FindIndex(body, item => Opcode(item) == IsMap);
            int projection = Array.FindIndex(body, item => Opcode(item) == GetMapElements);
            int error = Array.FindIndex(body, item => IsRemote(
                item,
                "erlang",
                "error",
                1
            ));
            if (map < 0 || projection <= map || error <= projection || Opcode(body[error - 1]) != Line
                || Value(Operands(body[error - 1])[0]) != 0
                || Value(Operands(body[map])[0]) == Value(Operands(body[projection])[0])
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

        if (!Body("computed").Any(item => Opcode(item) == Deallocate)
            || !Body("computed").Any(item => Opcode(item) == TailExternalCallWithFrame && IsRemote(
                item,
                "erlang",
                "error",
                1
            ))
            || !Body("branch").Any(item => Opcode(item) == TailExternalCall && IsRemote(
                item,
                "erlang",
                "error",
                1
            ))
            || Body("dynamic").Any(item => Opcode(item) is IsMap or GetMapElements))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] confirmed = Body("confirmed");
        if (confirmed.Count(item => Opcode(item) == IsMap) != 1
            || confirmed.Count(item => Opcode(item) == GetMapElements) != 1
            || confirmed.Any(item => IsRemote(
                item,
                "erlang",
                "error",
                1
            ) || IsRemote(
                item,
                "maps",
                "get",
                3
            )))
        {
            throw new InvalidOperationException(WrongLayout);
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
    }

    private static object[] Items(object value, string name) =>
        ((IEnumerable)Property(value, name)).Cast<object>().ToArray();

    private static object[] Operands(object value) => Items(value, "Operands");

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
