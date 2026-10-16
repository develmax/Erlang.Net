namespace Erlang.Otp;

internal static class ProcLibProcessInfoQuery
{
    internal static async ValueTask<Term> Read(ProcessContext context, Term target, Term item)
    {
        if (target is not Pid remote || remote.Node == context.Runtime.Node)
            return await context.Runtime.Modules.Call(
                context,
                ProcLibNames.Erlang,
                ProcLibNames.ProcessInfo,
                target,
                item
            );
        Term visible = await context.Runtime.Modules.Call(context, ProcLibNames.Erlang, ProcLibRemoteInfoNames.Nodes);
        if (!Cons.Items(visible).Any(node => node.Equals(Term.A(remote.Node))))
            return ProcLibRemoteInfoAtoms.Hidden;
        Term value = await context.Runtime.Modules.Call(
            context,
            ErpcNames.Rpc,
            ErpcNames.Call,
            Term.A(remote.Node),
            ProcLibRemoteInfoAtoms.Erlang,
            ProcLibRemoteInfoAtoms.ProcessInfo,
            Term.List(target, item)
        );
        if (value is TupleTerm { Items: [Atom { Name: ErpcNames.BadRpc }, var reason] })
            return reason.Equals(ErpcAtoms.NodeDown) ? ProcessInfoAtoms.Undefined : reason;

        return value;
    }
}
