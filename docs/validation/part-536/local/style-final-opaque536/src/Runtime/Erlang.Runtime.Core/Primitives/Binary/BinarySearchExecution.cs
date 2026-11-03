namespace Erlang;

internal static class BinarySearchExecution
{
    internal static async ValueTask<Term> Run(ProcessContext context, IReadOnlyList<Term> arguments, BinarySearchOperation operation)
    {
        await context.ChargeAsync(0);
        if (arguments[0] is not BitString { IsBinary: true } subject)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        Term options = arguments.Count == 3 ? arguments[2] : Nil.Value;
        BinarySearchOptions parsed = await BinarySearchOptionParser.Parse(
            context,
            options,
            subject.ByteLength,
            operation
        );
        using BinarySearchPatternHandle handle = await BinarySearchPatterns.Resolve(context, arguments[1]);
        BinarySearchPattern pattern = handle.Pattern;
        var work = new BinarySearchWork(context);
        var found = new List<BinarySearchHit>();
        for (int position = parsed.Start; position < parsed.End;)
        {
            await work.Step();
            BinarySearchHit? next = await BinaryArenaSearch598.FindFirst598(
                pattern.Backing598.Arena598,
                subject,
                position,
                parsed.End,
                work,
                handle.Reader598
            );
            if (next is not { } hit) break;
            found.Add(hit);
            if (!parsed.Global)
                break;
            position = checked(hit.Position + hit.Length);
        }
        if (operation == BinarySearchOperation.Split)
            return await BinarySearchResults.Split(
                context,
                subject,
                found,
                parsed
            );
        if (operation == BinarySearchOperation.Match)
            return found.Count == 0 ? Term.A(BinarySearchNames.NoMatch) : BinarySearchResults.Position(found[0]);
        var matches = new List<Term>(found.Count);
        foreach (BinarySearchHit hit in found)
        {
            await work.Step();
            matches.Add(BinarySearchResults.Position(hit));
        }

        return await BinarySearchResults.List(context, matches);
    }
}
