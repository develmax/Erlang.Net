namespace Erlang.Tests;

internal static class RegexNative411Access
{
    internal static string Admission()
    {
        for (DirectoryInfo? path = new(AppContext.BaseDirectory); path is not null; path = path.Parent)
        {
            string candidate = Path.Combine(path.FullName, RegexNative411Names.AdmissionRelative);
            if (File.Exists(candidate)) return candidate;
        }
        throw new FileNotFoundException(RegexNative411Names.AdmissionRelative);
    }

    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(RegexNative411Names.Failed);
    }

    internal static async ValueTask<Term> Call(ProcessContext context, string function, params Term[] arguments)
        => await context.Runtime.Modules.Call(
            context,
            RegexNative411Names.Module,
            function,
            arguments
        );

    internal static Term Pattern(Term result)
    {
        Require(result is TupleTerm { Items.Count: 2 } value && value.Items[0].Equals(Term.A(RegexNative411Names.Ok)));

        return ((TupleTerm)result).Items[1];
    }

    internal static Term Match(long start, long length) => Term.Tuple(Term.A(RegexNative411Names.MatchAtom), Term.List(Term.Tuple(Term.I(start), Term.I(length))));

    internal static void Empty(RuntimeRegexEngine411 engine)
    {
        var report = engine.Report;
        Require(report.Closed && report.Active == 0 && report.RetainedCodes == 0 && report.CodesCreated == report.CodesFreed && report.MatchDataCreated == report.MatchDataFreed && report.LibraryFreed);
    }
}
