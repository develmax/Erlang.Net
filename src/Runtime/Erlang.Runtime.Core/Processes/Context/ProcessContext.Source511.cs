namespace Erlang;

public sealed partial class ProcessContext
{
    internal SourceEvaluationTurn511 EnterSourceEvaluation511(bool functionBody)
        => CurrentOwned506 is { CanBorrow507: true } scope
            && scope.BindingKind507 is OwnedBindingKind507.CompilerGuest or OwnedBindingKind507.PreparedGuest
                ? new(scope, functionBody) : default;

    internal SourceStatementRoots511? EnterSourceStatementRoots511()
        => CurrentOwned506 is { CanBorrow507: true, SourceEvaluationDepth511: 1, SourceFunctionBody511: true } scope
            && scope.BindingKind507 is OwnedBindingKind507.CompilerGuest or OwnedBindingKind507.PreparedGuest
                ? new(scope) : null;
}
