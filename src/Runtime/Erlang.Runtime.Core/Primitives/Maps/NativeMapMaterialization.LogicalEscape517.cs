namespace Erlang;

internal static partial class NativeMapMaterialization505
{
    internal static MapTerm PublishLogicalEscape517(MapTerm copiedRaw, MapTerm logical)
    {
        if (!layouts.TryGetValue(copiedRaw, out var layout))
            throw new InvalidOperationException(MapRouting517Names.MissingPlan);
        layouts.Add(logical, layout);

        return logical;
    }
}
