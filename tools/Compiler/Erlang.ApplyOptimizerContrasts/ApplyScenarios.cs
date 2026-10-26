namespace ApplyOptimizerContrasts;

internal static class ApplyScenarios
{
    internal static readonly ApplyScenarioBinding[] All =
    [
        new("literal", ApplyNames.MainModule, "literal"),
        new("known-list", ApplyNames.MainModule, "known_list"),
        new("bound-targets", ApplyNames.MainModule, "bound_targets"),
        new("unknown-list", ApplyNames.MainModule, "unknown_list"),
        new("module-argument", ApplyNames.MainModule, "module_argument"),
        new("targets-arguments", ApplyNames.MainModule, "targets_arguments"),
        new("item-effect", ApplyNames.MainModule, "item_effect"),
        new("item-throw", ApplyNames.MainModule, "item_throw"),
        new("local-override", ApplyNames.LocalModule, "run"),
    ];
}
