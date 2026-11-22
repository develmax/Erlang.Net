using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedRetryStateAliasChecks
{
    internal const string TestName = "compiler/beam/rematerializes-checked-retry-state-alias";
    private const string Fixture = "beam_closed_retry_state_alias_probe.erl";
    private const string WrongCode = "The checked state alias must lower as its exact literal without retaining the incoming register.";
    private const string WrongValue = "The checked state alias changed a terminal result or an existing pattern comparison.";

    private const string Source = """
        -module(beam_closed_retry_state_alias_probe).
        -export([dispatch/4]).
        -record(inner, {items}).
        -record(outer, {nested}).
        dispatch(internal, {failure, _, _}, {retry, client}, #outer{nested=#inner{items=[]}}) ->
            first;
        dispatch(internal, {failure, Payload, _}, {retry, client}=Route, #outer{nested=Inner}) ->
            Key = case Inner#inner.items of alternate -> a; _ -> b end,
            Result = check(Key, Payload),
            {second, Route, Result};
        dispatch(internal, {failure, _, _}, {state_b, client}, Data) -> {owned_b, Data};
        dispatch(internal, {failure, _, _}, {state_c, client}, Data) -> {owned_c, Data};
        dispatch(internal, {peer, Payload, _}, {state_b, client}, Data) -> {peer_b, Payload, Data};
        dispatch(internal, {peer, Payload, _}, {state_c, client}, Data) -> {peer_c, Payload, Data};
        dispatch(internal, {small}, {state_b, client}, Data) -> {small, Data};
        dispatch(Type, Event, State, Data) -> {fallback, Type, Event, State, Data}.
        check(_, Payload) -> Payload.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        if (!HasLiteralState(image))
        {
            throw new InvalidOperationException(WrongCode);
        }

        byte[] patternControl = CodeGeneration.CompileBeamModule(
            Source.Replace(
                "{second, Route, Result}",
                "case Result of Route -> {same, Route}; _ -> {different, Route} end",
                StringComparison.Ordinal
            ),
            Fixture
        );
        var positive = BeamModule.Prepare(image);
        var retained = BeamModule.Prepare(patternControl);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term route = Term.Tuple(Term.A("retry"), Term.A("client"));
            Term otherwise = Term.A("other");
            Term data = Term.Tuple(Term.A("outer"), Term.Tuple(Term.A("inner"), Term.A("alternate")));
            if (!(await Invoke(positive, otherwise)).Equals(Term.Tuple(Term.A("second"), route, otherwise))
                || !(await Invoke(retained, route)).Equals(Term.Tuple(Term.A("same"), route))
                || !(await Invoke(retained, otherwise)).Equals(Term.Tuple(Term.A("different"), route)))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;

            async Task<Term> Invoke(BeamModule module, Term payload) => await module.Invoke(
                context,
                "dispatch",
                Term.A("internal"),
                Term.Tuple(Term.A("failure"), payload, Term.A("spare")),
                route,
                data
            );
        });
    }

    private static bool HasLiteralState(byte[] image)
    {
        object code = Code(image);
        object structure = Property(code, "Structure");
        string[] atoms = Items(structure, "Atoms").Cast<string>().ToArray();
        Term[] literals = Items(structure, "Literals").Cast<Term>().ToArray();

        return Items(code, "Instructions").Any(instruction =>
            (byte)Property(Property(instruction, "Definition"), "Opcode") == PutTuple
            && Items(instruction, "Operands").ToArray() is [_, _, var tag, var state, _]
            && Property(tag, "Kind") is "atom" && atoms[Value(tag)] == "second"
            && Property(state, "Kind") is "literal"
            && literals[Value(state)].Equals(Term.Tuple(Term.A("retry"), Term.A("client"))));
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
