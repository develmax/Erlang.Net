namespace Erlang.Tests;

internal static class DebugUnaligned421Checks
{
    internal static async Task Source()
    {
        await using var runtime = new ProcessRuntime();
        await DebugUnaligned421Access.Initialize(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            BitString[] inputs = [new([]), new([0xA8], 5), new([0, 255, 128, 1]), new(new byte[64]), new(new byte[129])];
            foreach (BitString input in inputs)
                for (int offset = 1; offset < 8; offset++)
                {
                    BitString result = (BitString)await runtime.Modules.Call(
                        context,
                        DebugUnaligned421Names.Module,
                        DebugUnaligned421Names.Function,
                        input,
                        Term.I(offset)
                    );
                    DebugUnaligned421Access.Representation(input, result, offset);
                    BitString nested = (BitString)await runtime.Modules.Call(
                        context,
                        DebugUnaligned421Names.Module,
                        DebugUnaligned421Names.Function,
                        result,
                        Term.I(8 - offset)
                    );
                    DebugUnaligned421Access.Representation(result, nested, 8 - offset);
                }

            return true;
        });
    }

    internal static async Task Owner()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        await DebugUnaligned421Access.Initialize(runtime);
        await foreign.InitializeModulesAsync(_ => { });
        ErlangFunction body = DebugUnaligned421Access.Capture(runtime);
        await using (var other = new RuntimeNodeFoundationHostActor(foreign))
            await other.InvokeAsync(async context =>
            {
                bool rejected = false;
                try
                {
                    await body(context, [new BitString([]), Term.I(1)]);
                }
                catch (ArgumentException)
                {
                    rejected = true;
                }
                DebugUnaligned421Access.Require(rejected);

                return true;
            });
        ProcessContext? saved = null;
        await using (var actor = new RuntimeNodeFoundationHostActor(runtime))
            await actor.InvokeAsync(async context =>
            {
                saved = context;
                foreach (Term offset in new Term[] { Term.I(0), Term.I(8), Term.I(-1), new Integer(System.Numerics.BigInteger.One << 100), Term.A(DebugUnaligned421Names.Invalid) })
                {
                    bool badarg = false;
                    try
                    {
                        await body(context, [new BitString([]), offset]);
                    }
                    catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                    {
                        badarg = true;
                    }
                    DebugUnaligned421Access.Require(badarg);
                }
                DebugLocks419Access.Cancel(context);
                bool canceled = false;
                try
                {
                    await body(context, [new BitString([]), Term.I(1)]);
                }
                catch (OperationCanceledException)
                {
                    canceled = true;
                }
                DebugUnaligned421Access.Require(canceled);

                return true;
            });
        await runtime.DisposeAsync();
        bool closed = false;
        try
        {
            await body(saved!, [new BitString([]), Term.I(1)]);
        }
        catch (ObjectDisposedException)
        {
            closed = true;
        }
        DebugUnaligned421Access.Require(closed);
    }
}
