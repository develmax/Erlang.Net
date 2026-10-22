using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.PortErrorStack399Names;

namespace Erlang.Tests;

internal static class PortErrorStack399Checks
{
    internal static async Task CompleteOpenAndUnchangedCloseStacks()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (int depth in new[] { 0, 1, 2, 8 })
            {
                int old = (int)Call(runtime, DepthMethod, depth);
                try
                {
                    using var caller = context.EnterFunction(CallerModule, CallerFunction, 0);
                    using var location = context.EnterSourceLocation(Location(CallerFile, CallerLine));
                    Term callerFrames = Capture(
                        context,
                        Term.I(0),
                        null,
                        null,
                        null
                    );
                    await VerifyCall(
                        context,
                        callerFrames,
                        depth,
                        PortNames.Open,
                        [Term.I(0), Nil.Value],
                        false,
                        GeneralErrorLine
                    );
                    await VerifyCall(
                        context,
                        callerFrames,
                        depth,
                        PortNames.Open,
                        [Term.I(0), Term.List(Term.A(InvalidOption))],
                        true,
                        BadOptionLine
                    );
                    await VerifyCall(
                        context,
                        callerFrames,
                        depth,
                        PortNames.Close,
                        [Term.I(0)],
                        false,
                        null
                    );
                }
                finally
                {
                    Call(runtime, DepthMethod, old);
                }
            }

            return true;
        });
    }

    internal static async Task OptionalPrefixPreservesSourceWrapperRule()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(context =>
        {
            using var caller = context.EnterFunction(CallerModule, CallerFunction, 0);
            using var location = context.EnterSourceLocation(Location(CallerFile, CallerLine));
            Term callerFrames = Capture(
                context,
                Term.I(0),
                null,
                null,
                null
            );
            Term arguments = Term.List(Term.I(0), Nil.Value);
            MapTerm information = Information(false);
            using var wrapper = (IDisposable)Call(
                context,
                WrapperMethod,
                ErlangModuleNames.Module,
                PortNames.Open,
                2
            );
            Term actual = Capture(
                context,
                arguments,
                information,
                PortNames.Open,
                Location(BuiltinFile, GeneralErrorLine)
            );
            Term expected = Expected(
                callerFrames,
                runtime.BacktraceDepth,
                PortNames.Open,
                arguments,
                information,
                GeneralErrorLine
            );
            Require(actual.Equals(expected));

            // Default-null remains byte-for-byte existing metadata: only error_info on the builtin.
            Term unchanged = Capture(
                context,
                arguments,
                information,
                PortNames.Open,
                null
            );
            Require(unchanged.Equals(Expected(
                callerFrames,
                runtime.BacktraceDepth,
                PortNames.Open,
                arguments,
                information,
                null
            )));

            return Task.FromResult(true);
        });
    }

    private static async Task VerifyCall(
        ProcessContext context,
        Term callers,
        int depth,
        string function,
        Term[] arguments,
        bool badOption,
        int? line
    )
    {
        bool failed = false;
        try
        {
            await context.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                function,
                arguments
            );
        }
        catch (ErlangException error)
        {
            failed = true;
            Require(error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
            Require(error.ExplicitStackTrace);
            Require(error.StackTraceTerm!.Equals(Expected(
                callers,
                depth,
                function,
                Cons.From(arguments),
                Information(badOption),
                line
            )));
        }
        Require(failed);
    }

    private static Term Capture(
        ProcessContext context,
        Term arguments,
        MapTerm? information,
        string? builtin,
        Term? source
    )
        => (Term)Call(
            context,
            CaptureMethod,
            arguments,
            information,
            builtin,
            source
        );

    private static Term Expected(
        Term callers,
        int depth,
        string function,
        Term arguments,
        MapTerm information,
        int? line
    )
    {
        Term metadata = Term.List(Term.Tuple(Term.A(ErlangErrorMetadataNames.ErrorInfo), information));
        if (line is { } value)
            metadata = Cons.From(Cons.Items(Location(BuiltinFile, value)).Concat(Cons.Items(metadata)));
        Term builtin = Term.Tuple(
            Term.A(ErlangModuleNames.Module),
            Term.A(function),
            arguments,
            metadata
        );

        return Cons.From(new[] { builtin }.Concat(Cons.Items(callers).Take(Math.Max(1, depth) - 1)));
    }

    private static Term Location(string file, int line)
        => Term.List(
            Term.Tuple(Term.A(FileKey), Term.String(file)),
            Term.Tuple(Term.A(LineKey), Term.I(line))
        );

    private static MapTerm Information(bool badOption)
    {
        var entries = new List<KeyValuePair<Term, Term>>
        {
            new(Term.A(PortNames.ErrorModule), Term.A(PortNames.ErrorFormatter))
        };
        if (badOption)
            entries.Add(new(Term.A(PortNames.Cause), Term.A(PortNames.BadOption)));

        return new MapTerm(entries);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
