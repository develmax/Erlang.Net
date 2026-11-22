using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record DeferredEntryTupleFields(
            VirtualTerminalCaseArm Arm,
            TerminalField[] Entry,
            IReadOnlyDictionary<int, TerminalField[]> ByCall,
            TerminalField[] Returns
        );

        private static DeferredEntryTupleFields? PlanDeferredEntryTupleFields(
            VirtualTerminalCaseArm arm,
            int carrier,
            TerminalField[] fields
        )
        {
            if (!arm.RequiresFrame || fields.Length < 2)
            {
                return null;
            }

            int[] calls = TerminalCallPositions(arm);
            var firstAndLast = new Dictionary<int, (int First, int Last)>();
            for (int position = 0; position < arm.Operations.Length; position++)
            {
                foreach (int value in arm.Operations[position].Uses)
                {
                    int first = firstAndLast.TryGetValue(value, out var range) ? range.First : position;
                    firstAndLast[value] = (first, position);
                }
            }

            // An ordinary call separates the possible collection points.
            // Sink a checked, single-region projection into its using block.
            // Keep the entry strategy for one late field across recursion.
            TerminalField[] late = fields.Where(field => field.Opcode == GetTupleElement
                && firstAndLast.TryGetValue(field.Value, out var use) && Region(use.First) == Region(use.Last)
                && use.First > calls[0]).ToArray();
            if (late.Length < 2)
            {
                return null;
            }

            var uses = late.ToLookup(field => Region(firstAndLast[field.Value].First));
            var revised = new List<BeamFrameValueLiveness.Operation>();
            for (int position = 0; position < arm.Operations.Length; position++)
            {
                if (position == 0 || arm.Operations[position - 1].ClobbersXRegisters)
                {
                    foreach (TerminalField field in uses[Region(position)])
                    {
                        revised.Add(new(
                            [carrier],
                            field.Value,
                            ClobbersXRegisters: false
                        ));
                    }
                }

                revised.Add(arm.Operations[position]);
            }

            return new(
                arm with { Operations = revised.ToArray() },
                fields.Except(late).ToArray(),
                Enumerable.Range(0, calls.Length).Select(call => (call, fields: uses[call].ToArray()))
                    .Where(row => row.fields.Length != 0).ToDictionary(row => row.call, row => row.fields),
                uses[calls.Length].ToArray()
            );

            int Region(int position)
            {
                int match = Array.BinarySearch(calls, position);

                return match >= 0 ? match : ~match;
            }
        }

        private static TerminalProjection[]? AllocateDeferredTupleFields(
            TerminalField[] fields,
            VirtualOperand[] preferred,
            IEnumerable<VirtualOperand> used,
            Dictionary<int, (int Tag, int Register)> locations,
            int carrier,
            int removed,
            bool preserveX0
        )
        {
            if (!locations.TryGetValue(carrier, out var home) || home.Tag != YRegisterTag || home.Register < removed)
            {
                return null;
            }

            var occupied = ReadValues(used)
                .Where(value => locations.TryGetValue(value, out var place) && place.Tag == XRegisterTag)
                .Select(value => locations[value].Register).ToHashSet();
            if (preserveX0)
            {
                occupied.Add(0);
            }

            var projections = new List<TerminalProjection>();
            foreach (TerminalField field in fields)
            {
                int register = Array.FindLastIndex(preferred, argument => argument.Value == field.Value);
                if (register < 0 || occupied.Contains(register))
                {
                    register = 0;
                    while (occupied.Contains(register))
                    {
                        register++;
                    }
                }

                if (register < 0 || register >= FirstReservedXRegister)
                {
                    return null;
                }

                occupied.Add(register);
                locations[field.Value] = (XRegisterTag, register);
                projections.Add(new(
                    home.Register - removed,
                    XRegisterTag,
                    register,
                    field.Opcode,
                    field.Index,
                    InputTag: YRegisterTag
                ));
            }

            return projections.ToArray();
        }
    }
}
