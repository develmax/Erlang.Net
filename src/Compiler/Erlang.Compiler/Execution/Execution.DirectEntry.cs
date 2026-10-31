namespace Erlang.Compiler;

public static partial class Execution
{
    // This actual interpreter service is the provenance. Public unregistered
    // ModuleDefinition/Expr data do not require synthetic registry publication.
    private static bool AdmitDirectGuestCollector(ProcessContext context)
    {
        if (!context.OwnedExecutionEnabled683 && RuntimeActorExecutionTurn.IsOwnedBy(context))
            context.AdmitOrdinaryGuestCollector();

        return context.OwnedExecutionEnabled683;
    }
}
