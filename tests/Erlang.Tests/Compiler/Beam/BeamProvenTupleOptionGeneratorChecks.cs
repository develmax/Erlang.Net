using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProvenTupleOptionGeneratorChecks
{
    internal const string TestName = "compiler/beam/proven-tuple-option-and-result-generators";
    private const string MissingTuplePlan = "The proven tuple source did not pack the option before the call.";
    private const string MissingProperListFact = "The discarded effect generator retested a proven proper list.";
    private const string MissingUnknownSourceCheck = "The unchecked generator source lost its list checks.";
    private const string Source = """
        -module(beam_proven_tuple_option_generator).
        -export([known/1, unknown/3]).
        known(Panel) ->
            Labels = [{first,1},{second,2}],
            Buttons = [service:new(Panel,Id,[{label,L}]) || {L,Id} <- Labels],
            [service:add(Panel,B) || B <- Buttons],
            Buttons.
        unknown(Panel, Labels, Extra) ->
            Buttons = [service:new(Panel,Id,[{label,L}]) || {L,Id} <- Labels],
            [service:add(Panel,B) || B <- Extra],
            Buttons.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_proven_tuple_option_generator.erl"));
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>().ToArray();
        byte[][] known = Helpers("known");
        byte[][] unknown = Helpers("unknown");

        if (!known.Any(helper => helper.Contains(PutList) && helper.Contains(AllocateHeap) && !helper.Contains(TestArity)))
        {
            throw new InvalidOperationException(MissingTuplePlan);
        }

        if (!known.Any(helper => !helper.Contains(PutList) && !helper.Contains(IsNil)))
        {
            throw new InvalidOperationException(MissingProperListFact);
        }

        if (!unknown.Any(helper => helper.Contains(TestArity)) || !unknown.Any(helper => helper.Contains(IsNil)))
        {
            throw new InvalidOperationException(MissingUnknownSourceCheck);
        }

        byte[][] Helpers(string function) => functions
            .Where(candidate => ((string)Property(candidate, "Name")).StartsWith($"-{function}/", StringComparison.Ordinal))
            .Select(candidate =>
            {
                int start = (int)Property(candidate, "FirstInstructionIndex");
                int end = (int)Property(candidate, "EndInstructionIndex");

                return instructions[start..end]
                    .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
            }).ToArray();
    }
}
