using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record GuardedOptionFilter(
        Expr.Call Filter,
        Expr.Call Apply,
        string Socket,
        string Options,
        string Rejected,
        Atom Error,
        Atom Detail
    );

    private static GuardedOptionFilter? ReadGuardedOptionFilter(Expr expression)
    {
        if (expression is not Expr.Case
        {
            Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable input] } filter,
            Clauses: [
                {
                    Patterns: [Pattern.List { Items: [], Tail: null } or Pattern.Literal { Value: Nil }],
                    Guard: null,
                    Body: Expr.Call
                    {
                        Module: not null and not ErlangModuleNames.Module, DynamicRemote: false,
                        Arguments: [Expr.Variable socket, Expr.Variable options]
                    } apply
                },
                {
                    Patterns: [Pattern.Variable rejected], Guard: null,
                    Body: Expr.Tuple { Items: [
                        Expr.Literal { Value: Atom error },
                        Expr.Tuple { Items: [Expr.Literal { Value: Atom detail }, Expr.Variable returned] }
                    ] }
                }
            ]
        } || input.Name != options.Name || rejected.Name != returned.Name
            || rejected.Name == VariableScopeNames.Wildcard)
        {
            return null;
        }

        return new(filter, apply, socket.Name, options.Name, rejected.Name, error, detail);
    }

    private void EmitGuardedOptionFilter(
        Clause body,
        GuardedOptionFilter flow,
        int fallback,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (!entries.TryGetValue((flow.Filter.Function, 1), out int target))
        {
            throw new NotSupportedException(Unsupported);
        }

        int options = ArgumentRegister(body, flow.Options);
        int socket = ArgumentRegister(body, flow.Socket);
        Op(Allocate, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Math.Max(options, socket) + 1);
        MoveRegister(XRegisterTag, options, YRegisterTag, 0);
        MoveRegister(XRegisterTag, socket, YRegisterTag, 1);
        if (options != 0)
        {
            MoveToX0(XRegisterTag, options);
        }
        EmitCallLine(flow.Filter, location);
        Op(LocalCall, BeamWire.UnsignedTag, 1);
        Operand(LabelTag, target);
        Op(IsNil, LabelTag, fallback);
        if (ReturnsOptionList(flow.Filter.Function))
        {
            const int listTypeBits = ConsTypeBits | (1 << 7); // BEAM_TYPE_CONS | BEAM_TYPE_NIL
            EmitTypedRegister(0, listTypeBits);
        }
        else
        {
            Operand(XRegisterTag, 0);
        }
        MoveRegister(YRegisterTag, 0, XRegisterTag, 1);
        MoveToX0(YRegisterTag, 1);
        EmitCallLine(flow.Apply, CallLocation(flow.Filter, location));
        Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import(flow.Apply.Module!, flow.Apply.Function, 2));
        Operand(BeamWire.UnsignedTag, 2);
        Op(Label, BeamWire.UnsignedTag, fallback);
        Op(TestHeap, BeamWire.UnsignedTag, 6);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTagMatchTuple(0, (AtomTag, Intern(flow.Detail.Name)), (XRegisterTag, 0));
        EmitTagMatchTuple(0, (AtomTag, Intern(flow.Error.Name)), (XRegisterTag, 0));
        Op(Deallocate, BeamWire.UnsignedTag, 2);
        instructions.WriteByte(Return);
    }

    private bool ReturnsOptionList(string name)
    {
        var filter = sourceModule?.Functions.FirstOrDefault(function => function.Name == name && function.Arity == 1);
        return filter is not null && filter.Clauses.All(clause => clause.Body is
            Expr.List { Tail: null } or Expr.ListComprehension or Expr.Literal { Value: Nil });
    }
}
