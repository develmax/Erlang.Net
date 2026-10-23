namespace Erlang;

internal static class RuntimeNodeCreation
{
    internal static bool TryConvert(Term value, out uint creation)
    {
        if (value is Integer number && number.Value >= 0 && number.Value <= RuntimeNodeCreationParameters.MaximumSmallCreation)
        {
            creation = (uint)(number.Value & RuntimeNodeCreationParameters.WireCreationMask);

            return true;
        }
        creation = 0;

        return false;
    }
}
