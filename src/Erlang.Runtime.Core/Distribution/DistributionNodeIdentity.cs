namespace Erlang;

internal static class DistributionNodeIdentity
{
    private const char Separator = '@';

    internal static bool IsValid(Atom node)
    {
        int separator = node.Name.IndexOf(Separator);
        if (separator <= 0 || separator == node.Name.Length - 1 || node.Name.IndexOf(Separator, separator + 1) >= 0)
            return false;
        for (int index = 0; index < separator; index++)
            if (node.Name[index] is not ('-' or '_' or >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9'))
                return false;

        return true;
    }
}
