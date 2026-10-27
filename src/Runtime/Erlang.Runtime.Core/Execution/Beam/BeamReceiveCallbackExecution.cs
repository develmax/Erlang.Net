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
        using var wordOperation504 = before.EnterWordOperation504();
        await before.ReceiveCheckpoint507(BeamOwnedReceive507Names.CallbackEntered);
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
        // On failure the caller's handler captures this continuation before
        // unwinding it. Clearing in a finally would change original stacks.
        Term result = await callable.Invoke(context, Array.AsReadOnly(new[] { message }));
        scope.ClearContinuation();
        if (result is Atom { Name: BeamReceiveCallbackNames.NoMatch })
            return null;
        BeamRegisterState registers = before.Clone();
        try
        {
            registers.ClearXExcept();
            registers.ClearFloats();
            registers.Write(0, result);

            var selection = new BeamReceiveSelection(callback.SuccessInstructionIndex, registers, []);
            try
            {
                await before.ReceiveCheckpoint507(BeamOwnedReceive507Names.CallbackSelected);

                return selection;
            }
            catch
            {
                selection.Dispose();
                throw;
            }
        }
        catch
        {
            registers.Dispose();
            throw;
        }
    }
}
