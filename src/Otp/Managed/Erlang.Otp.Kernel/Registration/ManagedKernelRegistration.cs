namespace Erlang;

public sealed class ManagedKernelRegistration : IRuntimeManagedLibrary456
{
    public void Register(ModuleRegistry registry, RuntimeManagedLibraryStage456 stage)
    {
        if (stage == RuntimeManagedLibraryStage456.Erpc)
        {
            ErpcLibrary.Register(registry);

            return;
        }
        if (stage == RuntimeManagedLibraryStage456.ErrorLogger)
        {
            ErrorLoggerModule.Register(registry);

            return;
        }
        if (stage == RuntimeManagedLibraryStage456.Logger)
        {
            LoggerModule.Register(registry);

            return;
        }
        if (stage == RuntimeManagedLibraryStage456.Application)
        {
            ApplicationModule.Register(registry);

            return;
        }
        if (stage != RuntimeManagedLibraryStage456.File)
        {
            throw new ArgumentOutOfRangeException(nameof(stage));
        }
        FileModule.Register(registry);
    }
}
