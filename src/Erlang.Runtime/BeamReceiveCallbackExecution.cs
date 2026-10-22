namespace Erlang;

internal static class BeamReceiveCallbackExecution
{
    internal static async ValueTask<BeamReceiveSelection?> Select(
        ProcessContext context,
        BeamLinkedCode code,
        BeamLinkedFunction function,
        BeamFunctionScope scope,
        BeamReceiveCallbackPlan callback,
        BeamRegisterState before,
        Term message
    )
    {
        Term callee = BeamRegisterInterpreter.Read(code, before, callback.Callee);
        if (callee is not FunctionTerm callable || callable.Arity != 1)
        {
            Term reason = callee is FunctionTerm
                ? Term.Tuple(Term.A(ErlangErrorReasons.BadArity), Term.Tuple(callee, Term.List(message)))
                : Term.Tuple(Term.A(ErlangErrorReasons.BadFunction), callee);
            // Invalid call_fun has its own current-function error frame as well
            // as the saved caller continuation in the original instruction.
            using var failure = context.EnterBeamFunction(function.Module, function.Name, function.Arity);
            throw context.CaptureSourceFailure(new ErlangException(reason));
        }
        scope.SetContinuation(code, callback.CallInstructionIndex + 1);
        Term result = await callable.Invoke(context, Array.AsReadOnly(new[] { message }));
        scope.ClearContinuation();
        if (result is Atom { Name: BeamReceiveCallbackNames.NoMatch })
            return null;
        BeamRegisterState registers = before.Clone();
        registers.ClearXExcept();
        registers.ClearFloats();
        registers.Write(0, result);

        return new(callback.SuccessInstructionIndex, registers, []);
    }
}
