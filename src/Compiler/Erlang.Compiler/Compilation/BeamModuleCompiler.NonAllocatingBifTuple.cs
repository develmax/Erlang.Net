using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNonAllocatingBifOperands(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null } clause]
                || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
            if (clause.Body is Expr.Sequence { Items: [_, .., Expr.Call tail] } sequence)
            {
                return TryEmitNonAllocatingBifTail(sequence, tail, parameters);
            }

            return TryEmitNonAllocatingBifOperands(clause.Body, parameters);
        }

        private bool TryEmitNonAllocatingBifTail(Expr.Sequence sequence, Expr.Call tail, string[] parameters)
        {
            if (ReadStraightCalls(sequence.Items.SkipLast(1).ToArray(), parameters) is not { } calls)
            {
                return false;
            }

            var prefix = new List<EntryCall>();
            string[] registers = parameters;
            foreach (var (call, result) in calls)
            {
                if (ReadEntryCall(call, registers) is not { } step)
                {
                    return false;
                }

                prefix.Add(step);
                registers = result is null ? [] : [result];
            }

            return TryEmitNonAllocatingBifOperands(tail, registers, prefix, EntryCallLiveCount(prefix[0], parameters));
        }

        private bool TryEmitNonAllocatingBifOperands(
            Expr body,
            string[] parameters,
            IReadOnlyList<EntryCall>? prefix = null,
            int prefixLive = 0
        )
        {
            Expr.Call? target = null;
            IReadOnlyList<Expr> operands;
            if (body is Expr.Tuple tuple)
            {
                operands = tuple.Items;
            }
            else if (body is Expr.Call { DynamicRemote: false } call)
            {
                target = owner.ResolveImportedCall(call, entries);
                if (target.Module == ErlangModuleNames.Module
                    || (target.Module is null && !entries.ContainsKey((target.Function, target.Arguments.Count))))
                {
                    return false;
                }

                operands = target.Arguments;
            }
            else
            {
                return false;
            }

            var fields = new (int Register, Term? Literal)[operands.Count];
            var calls = new List<(int Field, Expr.Call Call, int Source, Term? Literal)>();
            var remainingReads = new int[parameters.Length];
            var occupied = new HashSet<int>();
            int live = 0;
            for (int index = 0; index < operands.Count; index++)
            {
                Expr item = operands[index];
                if (TryReadOperand(item, out int register, out Term? literal))
                {
                    fields[index] = (register, literal);
                    if (register >= 0)
                    {
                        occupied.Add(register);
                    }

                    continue;
                }

                if (item is not Expr.Call { DynamicRemote: false } sourceCall)
                {
                    return false;
                }

                Expr.Call bif = owner.ResolveImportedCall(sourceCall, entries);
                if (bif.Module is not (null or ErlangModuleNames.Module) || !owner.ResolvesErlangBif(bif))
                {
                    return false;
                }

                if (bif is { Function: AutoImportNames.Get or AutoImportNames.Hd or AutoImportNames.Tl
                    or AutoImportNames.Node or AutoImportNames.TupleSize, Arguments: [var key] }
                    && TryReadOperand(key, out register, out literal))
                {
                    calls.Add((index, bif, register, literal));
                    if (register >= 0)
                    {
                        remainingReads[register]++;
                    }
                }
                else if (bif is { Function: AutoImportNames.Self or AutoImportNames.Node, Arguments: [] })
                {
                    calls.Add((index, bif, -1, null));
                }
                else
                {
                    return false;
                }
            }

            if (calls.Count == 0 || parameters.Length + calls.Count > BeamWire.MaximumArity)
            {
                return false;
            }

            bool canFail = calls.Any(item => item.Call.Arguments.Count == 1 && item.Call.Function != AutoImportNames.Get);
            var previous = location;
            if (prefix is not null)
            {
                EmitFrameAllocation(prefixLive, 0);
                foreach (EntryCall step in prefix)
                {
                    EmitEntryCall(step, previous);
                    previous = CallLocation(step.Call, previous);
                }
            }
            else if (target is null && !canFail)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, operands.Count + 1);
                owner.Operand(BeamWire.UnsignedTag, live);
            }

            foreach (var (field, bif, source, literal) in calls)
            {
                if (source >= 0)
                {
                    remainingReads[source]--;
                }

                int destination = target is not null && IsAvailable(field) ? field : 0;
                while (!IsAvailable(destination))
                {
                    destination++;
                }

                if (bif.Arguments.Count == 1)
                {
                    if (bif.Function != AutoImportNames.Get)
                    {
                        owner.EmitCallLine(bif, previous);
                        previous = CallLocation(bif, previous);
                    }

                    EmitUnaryGuardInstruction(bif.Function, 0);
                    WriteOperand(source, literal);
                    owner.Operand(XRegisterTag, destination);
                }
                else
                {
                    EmitZeroGuardBif(bif.Function, destination);
                }

                fields[field] = (destination, null);
                occupied.Add(destination);
            }

            if (target is not null)
            {
                var moves = new List<(int Destination, int Source)>();
                var constants = new List<(int Destination, Term Value)>();
                for (int index = 0; index < fields.Length; index++)
                {
                    var (register, literal) = fields[index];
                    if (register < 0)
                    {
                        constants.Add((index, literal!));
                    }
                    else if (register != index)
                    {
                        moves.Add((index, register));
                    }
                }

                owner.EmitArgumentMoves(moves, constants);
                if (prefix is null)
                {
                    EmitRegisterTailTarget(target, fields.Length);
                }
                else
                {
                    EmitPreparedFrameTail(new EntryCall(target, target.Module, [], []), previous, 0);
                }

                return true;
            }

            if (canFail)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, fields.Length + 1);
                owner.Operand(BeamWire.UnsignedTag, occupied.Max() + 1);
            }

            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, fields.Length);
            foreach (var (register, literal) in fields)
            {
                WriteOperand(register, literal);
            }

            owner.instructions.WriteByte(Return);
            return true;

            bool IsAvailable(int register) => !occupied.Contains(register)
                && (register >= remainingReads.Length || remainingReads[register] == 0);

            bool TryReadOperand(Expr expression, out int register, out Term? literal)
            {
                register = -1;
                literal = null;
                if (expression is Expr.Variable variable && variable.Name != VariableScopeNames.Wildcard)
                {
                    register = Array.IndexOf(parameters, variable.Name);
                    if (register >= 0)
                    {
                        live = Math.Max(live, register + 1);
                        return true;
                    }
                }

                if (AttributeLiteral.TryNormalizeConstant(expression, out Term constant))
                {
                    literal = constant;
                    return true;
                }

                return false;
            }

            void WriteOperand(int register, Term? literal)
            {
                if (register >= 0)
                {
                    owner.Operand(XRegisterTag, register);
                }
                else
                {
                    owner.LiteralOperand(literal!);
                }
            }
        }
    }
}
