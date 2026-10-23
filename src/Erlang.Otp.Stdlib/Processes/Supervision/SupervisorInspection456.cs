namespace Erlang.Otp;

internal static class SupervisorInspection456
{
    private static IReadOnlyList<ChildSpec> Visible(SupervisorHandle handle) => handle.Specifications.Where(s => s.Restart != RestartPolicy.Temporary || handle.Children.Any(c => c.Id.Equals(s.Id))).ToArray();

    internal static Term WhichChildren(SupervisorHandle handle) => Term.List(Visible(handle).Reverse().Select(s => Term.Tuple(
        s.Id,
        handle.IsRestarting(s.Id) ? Term.A(SupervisorAtoms.Restarting) : handle.Children.FirstOrDefault(c => c.Id.Equals(s.Id)).Pid ?? (Term)Term.A(SupervisorAtoms.Undefined),
        s.Type ?? Term.A(SupervisorAtoms.Worker),
        s.Modules ?? Nil.Value
    )).ToArray());

    internal static Term CountChildren(SupervisorHandle handle, ProcessRuntime runtime)
    {
        var specs = Visible(handle);

        return Term.List(
            Term.Tuple(Term.A(SupervisorAtoms.Specs), Term.I(specs.Count)),
            Term.Tuple(Term.A(SupervisorAtoms.Active), Term.I(handle.Children.Count(c => runtime.IsAlive(c.Pid)))),
            Term.Tuple(Term.A(SupervisorAtoms.Supervisors), Term.I(specs.Count(s => s.Type is Atom { Name: SupervisorAtoms.Supervisor }))),
            Term.Tuple(Term.A(SupervisorAtoms.Workers), Term.I(specs.Count(s => s.Type is not Atom { Name: SupervisorAtoms.Supervisor })))
        );
    }
}
