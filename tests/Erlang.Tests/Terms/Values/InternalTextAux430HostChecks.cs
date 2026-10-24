using System.Text;
using Erlang;
namespace Erlang.Tests;

internal static class InternalTextAux430HostChecks
{
    internal static async Task TextBudget()
    {
        await using var runtime = new ProcessRuntime(1);
        await runtime.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ErlangFunction body = InternalTextAux430HostAccess.Body(runtime, InternalTextAux430HostNames.Text, 2);
        await actor.InvokeAsync(async context =>
        {
            foreach (var row in new (Term Value, int Limit, string Limited, string Full)[]
            {
                (Term.I(123),2,"123","123"),
                (Term.A("abc"),2,"abc... ","abc"),
                (Term.A("λ"),3,"'λ'... ","'λ'"),
                (Term.Tuple(Term.I(1),Term.I(2)),2,"{1,... ","{1,2}"),
                (Term.Tuple(Term.I(1),Term.I(2)),3,"{1,2}... ","{1,2}"),
                (Term.String("abc"),2,"\"a... ","\"abc\""),
                (new BitString(Encoding.ASCII.GetBytes("abc")),2,"<<\"ab... ","<<\"abc\">>"),
                (new BitString(new byte[]{0,1,2}),2,"<<0,1... ","<<0,1,2>>"),
                (new BitString(new byte[]{160},3),2,"<<5:3>>","<<5:3>>")
            })
            {
                InternalTextAux430HostAccess.Require((await body(context, [row.Value, Term.I(row.Limit)])).Equals(InternalTextAux430HostAccess.Bytes(row.Limited)));
                InternalTextAux430HostAccess.Require((await body(context, [row.Value, Term.A(InternalTextAux430HostNames.Undefined)])).Equals(InternalTextAux430HostAccess.Bytes(row.Full)));
                InternalTextAux430HostAccess.Require(InternalTextAux430HostAccess.Display(runtime, row.Value).AsSpan().SequenceEqual(Encoding.UTF8.GetBytes(row.Full + "\n")));
            }

            return true;
        });
    }

    internal static async Task OwnerAndFailure()
    {
        await using var runtime = new ProcessRuntime(1);
        await runtime.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext own = await actor.Ready;
        ErlangFunction body = InternalTextAux430HostAccess.Body(runtime, InternalTextAux430HostNames.Text, 2);
        await actor.InvokeAsync(async context =>
        {
            Term[] args = [Term.A("foo"), Term.I(1)];
            bool refused = false;
            try
            {
                await body(context, args);
            }
            catch (ErlangException error)
            {
                refused = error.ExceptionClass == ErlangExceptionClasses.Error && error.Reason.Equals(Term.A(InternalTextAux430HostNames.BadArgument));
                Term frame = Cons.Items(error.StackTraceTerm).First();
                InternalTextAux430HostAccess.Require(frame is TupleTerm { Items: [Atom { Name: InternalTextAux430HostNames.Module }, Atom { Name: InternalTextAux430HostNames.Text }, var values, _] } && values.Equals(Cons.From(args)));
            }
            InternalTextAux430HostAccess.Require(refused);

            return true;
        });
        await using var foreign = new ProcessRuntime(1);
        await foreign.InitializeModulesAsync(_ => { });
        await using var other = new RuntimeNodeFoundationHostActor(foreign);
        await other.InvokeAsync(async context =>
        {
            bool refused = false;
            try
            {
                await body(context, [Nil.Value, Term.I(2)]);
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }
            InternalTextAux430HostAccess.Require(refused);

            return true;
        });
        await actor.DisposeAsync();
        await runtime.DisposeAsync();
        bool closed = false;
        try
        {
            await body(own, [Nil.Value, Term.I(2)]);
        }
        catch (ObjectDisposedException)
        {
            closed = true;
        }
        InternalTextAux430HostAccess.Require(closed);
    }

    internal static async Task AuxiliaryParticipants()
    {
        await using var runtime = new ProcessRuntime(InternalTextAux430HostNames.Participants);
        await runtime.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ErlangFunction body = InternalTextAux430HostAccess.Body(runtime, InternalTextAux430HostNames.Auxiliary, 0);
        await actor.InvokeAsync(async context =>
        {
            InternalTextAux430HostAccess.Require((await body(context, Array.Empty<Term>())).Equals(Term.I(InternalTextAux430HostNames.Participants)));
            await runtime.Modules.Call(
                context,
                InternalTextAux430HostNames.Runtime,
                InternalTextAux430HostNames.SystemFlag,
                Term.A(InternalTextAux430HostNames.Online),
                Term.I(1)
            );
            InternalTextAux430HostAccess.Require((await body(context, Array.Empty<Term>())).Equals(Term.I(InternalTextAux430HostNames.Participants)));

            return true;
        });
        IReadOnlyList<int> identities = await InternalTextAux430HostAccess.RunAuxiliary(runtime);
        InternalTextAux430HostAccess.Require(identities.SequenceEqual(Enumerable.Range(1, InternalTextAux430HostNames.Participants)));
        await actor.InvokeAsync(async context =>
{
    await runtime.Modules.Call(
        context,
        InternalTextAux430HostNames.Runtime,
        InternalTextAux430HostNames.SystemFlag,
        Term.A(InternalTextAux430HostNames.Online),
        Term.I(InternalTextAux430HostNames.Participants)
    );

    return true;
});
    }
}
