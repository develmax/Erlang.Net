using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record ListTailCall(Expr.Call Call, Term[] Constants, Term? Selector, Pattern.Tuple? Prefix, bool ListGuard);

        private bool TryEmitPairedListTailCalls(FunctionDefinition function)
        {
            if (function.Arity is not (2 or 3) || function.Clauses.Count < 2 || function.Arity == 2 && function.Clauses.Count != 2)
            {
                return false;
            }

            Clause? redirect = function.Clauses.Count % 2 == 0 ? null : function.Clauses[0];
            int redirectEntry = 0;
            if (redirect is not null && (function.Arity != 3 || redirect is not
                {
                    Guard: null,
                    Patterns: [Pattern.Variable first, Pattern.Literal { }, Pattern.Variable third],
                    Body: Expr.Call { Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable left, Expr.Literal, Expr.Variable right] } call
                } || !DistinctBindings(first, third) || left.Name != first.Name || right.Name != third.Name
                || !entries.TryGetValue((call.Function, 3), out redirectEntry)))
            {
                return false;
            }

            var pairs = new List<(ListTailCall Prefixed, ListTailCall Plain)>();
            for (int index = redirect is null ? 0 : 1; index < function.Clauses.Count; index += 2)
            {
                if (ReadListTailCall(function.Clauses[index], function.Arity, prefixed: true) is not { } prefixed
                    || ReadListTailCall(function.Clauses[index + 1], function.Arity, prefixed: false) is not { } plain
                    || !Equals(prefixed.Selector, plain.Selector) || prefixed.ListGuard != plain.ListGuard
                    || pairs.Count > 0 && pairs[0].Plain.ListGuard != plain.ListGuard)
                {
                    return false;
                }

                pairs.Add((prefixed, plain));
            }

            int failure = owner.nextLabel - 2;
            if (redirect is not null)
            {
                int next = Label();
                EmitKeyTest(1, ((Pattern.Literal)redirect.Patterns[1]).Value, next);
                owner.EmitConstant(((Expr.Call)redirect.Body).Arguments[1], 1);
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, 3);
                owner.Operand(LabelTag, redirectEntry);
                Mark(next);
            }

            for (int index = 0; index < pairs.Count; index++)
            {
                var (prefixed, plain) = pairs[index];
                int ordinary = Label();
                int next = index + 1 == pairs.Count ? failure : Label();
                int head = function.Arity;
                int tail = head + 1;
                owner.EmitNonemptyList(0, ordinary);
                owner.EmitListParts(0, XRegisterTag, head, XRegisterTag, tail);
                var pattern = prefixed.Prefix!;
                if (pattern.Items[1] is Pattern.Literal value)
                {
                    EmitKeyTest(head, Term.Tuple(((Pattern.Literal)pattern.Items[0]).Value, value.Value), ordinary);
                }
                else
                {
                    owner.Op(IsTaggedTuple, LabelTag, ordinary);
                    owner.Operand(XRegisterTag, head);
                    owner.Operand(BeamWire.UnsignedTag, 2);
                    owner.LiteralOperand(((Pattern.Literal)pattern.Items[0]).Value);
                }

                if (prefixed.Selector is { } key)
                {
                    EmitKeyTest(1, key, next);
                    EmitListGuard(prefixed);
                    owner.MoveRegister(XRegisterTag, 2, XRegisterTag, 1);
                }
                for (int arg = 2; arg < Math.Min(tail, prefixed.Call.Arguments.Count); arg++)
                {
                    owner.EmitConstant(prefixed.Constants[arg - 2], arg);
                }
                owner.MoveRegister(XRegisterTag, tail, XRegisterTag, 0);
                if (tail < prefixed.Call.Arguments.Count)
                {
                    owner.EmitConstant(prefixed.Constants[tail - 2], tail);
                }
                EmitTail(prefixed);

                Mark(ordinary);
                if (plain.Selector is { } plainKey)
                {
                    EmitKeyTest(1, plainKey, next);
                    EmitListGuard(plain);
                    for (int arg = 3; arg < plain.Call.Arguments.Count; arg++)
                    {
                        owner.EmitConstant(plain.Constants[arg - 2], arg);
                    }
                    owner.MoveRegister(XRegisterTag, 2, XRegisterTag, 1);
                    owner.EmitConstant(plain.Constants[0], 2);
                }
                else
                {
                    owner.EmitConstant(plain.Constants[1], 3);
                    owner.EmitConstant(plain.Constants[0], 2);
                }

                EmitTail(plain);
                if (next != failure)
                {
                    Mark(next);
                }
            }

            return true;

            void EmitKeyTest(int register, Term key, int alternative)
            {
                owner.Op(ExactEqualTest, LabelTag, alternative);
                owner.Operand(XRegisterTag, register);
                owner.LiteralOperand(key);
            }

            void EmitListGuard(ListTailCall arm)
            {
                if (arm.ListGuard)
                {
                    owner.Op(IsList, LabelTag, failure);
                    owner.Operand(XRegisterTag, 2);
                }
            }

            void EmitTail(ListTailCall arm)
            {
                owner.EmitCallLine(arm.Call, location);
                int arity = arm.Call.Arguments.Count;
                owner.Op(TailExternalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(arm.Call.Module!, arm.Call.Function, arity));
            }
        }

        private ListTailCall? ReadListTailCall(Clause clause, int arity, bool prefixed)
        {
            if (clause.Patterns[^1] is not Pattern.Variable payload)
            {
                return null;
            }

            bool listGuard = clause.Guard is not null;
            if (listGuard && (arity != 3 || clause.Guard is not Expr.Call
                {
                    Module: null or ErlangModuleNames.Module,
                    Function: AutoImportNames.IsList,
                    DynamicRemote: false,
                    Arguments: [Expr.Variable tested]
                } guard || tested.Name != payload.Name || guard.Module is null && entries.ContainsKey((guard.Function, 1))))
            {
                return null;
            }

            Pattern.Tuple? prefix = null;
            Pattern.Variable? config = null;
            if (prefixed && clause.Patterns[0] is Pattern.List
                {
                    Items: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom },
                        Pattern.Literal { } or Pattern.Variable { Name: VariableScopeNames.Wildcard }] } tuple],
                    Tail: Pattern.Variable tail
                })
            {
                prefix = tuple;
                config = tail;
            }
            else if (!prefixed && clause.Patterns[0] is Pattern.Variable variable)
            {
                config = variable;
            }

            Term? selector = arity == 3 && clause.Patterns[1] is Pattern.Literal key ? key.Value : null;
            if (config is null || !DistinctBindings(config, payload) || arity == 3 && selector is not (Atom or Integer))
            {
                return null;
            }

            var names = new HashSet<string>(StringComparer.Ordinal) { config.Name, payload.Name };
            var constants = new Dictionary<string, Term>(StringComparer.Ordinal);
            Expr body = clause.Body;
            if (body is Expr.Sequence sequence)
            {
                foreach (Expr item in sequence.Items.SkipLast(1))
                {
                    if (item is not Expr.Match { Pattern: Pattern.Variable binding, Value: var value }
                        || binding.Name == VariableScopeNames.Wildcard || !names.Add(binding.Name)
                        || !AttributeLiteral.TryNormalizeConstant(value, out Term constant, constants))
                    {
                        return null;
                    }

                    constants.Add(binding.Name, constant);
                }
                body = sequence.Items[^1];
            }

            if (body is not Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                    Arguments: [Expr.Variable argument, Expr.Variable input, ..] } call
                || argument.Name != config.Name || input.Name != payload.Name
                || call.Arguments.Count is not (4 or 5) || arity == 2 && call.Arguments.Count != 4)
            {
                return null;
            }

            var values = new Term[call.Arguments.Count - 2];
            for (int index = 0; index < values.Length; index++)
            {
                if (!AttributeLiteral.TryNormalizeConstant(call.Arguments[index + 2], out values[index], constants))
                {
                    return null;
                }
            }

            return new(call, values, selector, prefix, listGuard);
        }
    }
}
