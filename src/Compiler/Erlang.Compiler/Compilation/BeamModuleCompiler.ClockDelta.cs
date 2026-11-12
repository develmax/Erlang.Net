using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const string MonotonicClockFunction = "monotonic_time";
    private const string ConvertClockUnitFunction = "convert_time_unit";

    private bool TryEmitClockDelta(Clause clause, BeamSourceLocation location)
    {
        if (clause is not
            {
                Guard: null, Patterns: [Pattern.Variable start],
                Body: Expr.Binary
                {
                    Operator: ErlangOperators.IntegerDivide, Right: Expr.Literal { Value: Integer divisor },
                    Left: Expr.Call
                    {
                        Module: ErlangModuleNames.Module, Function: ConvertClockUnitFunction, DynamicRemote: false,
                        Arguments: [
                            Expr.Binary
                        {
                            Operator: ErlangOperators.Minus, Right: Expr.Variable subtracted,
                            Left: Expr.Call
                            {
                                Module: ErlangModuleNames.Module, Function: MonotonicClockFunction,
                                DynamicRemote: false, Arguments: []
                            } clock
                        } delta,
                            Expr.Literal { Value: Atom from }, Expr.Literal { Value: Atom to }
                        ]
                    } convert
                } divide
            } || !DistinctBindings(start) || start.Name != subtracted.Name)
        {
            return false;
        }

        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 1);
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            0
        );
        EmitCallLine(clock, location);
        EmitStaticRemoteCall(clock);
        var previous = CallLocation(clock, location);
        EmitArithmeticLine(delta.SourceLine, ref previous);
        Op(GcBifTwo, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, ErlangOperators.Minus, 2));
        EmitTypedRegister(0, IntegerTypeBits);
        Operand(YRegisterTag, 0);
        Operand(XRegisterTag, 0);
        EmitConstant(to, 2);
        EmitConstant(from, 1);
        Op(TrimFrame, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 0);
        EmitCallLine(convert, previous);
        EmitStaticRemoteCall(convert);
        previous = CallLocation(convert, previous);
        EmitArithmeticLine(divide.SourceLine, ref previous);
        Op(GcBifTwo, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, ErlangOperators.IntegerDivide, 2));
        EmitTypedRegister(0, IntegerTypeBits);
        LiteralOperand(divisor);
        Operand(XRegisterTag, 0);
        EmitFrameReturn();

        return true;
    }
}
