namespace Erlang.Validation.AsyncMicrostates;

internal static class AsyncOriginalComparison
{
    private static Term At(MapTerm map, string key) => map.Get(Term.A(key));

    private static TupleTerm Tuple(Term value, int arity) => value is TupleTerm tuple
        && tuple.Items.Count == arity ? tuple : throw new InvalidDataException(AsyncOriginalComparisonNames.TupleShapeFailureMessage);

    private static void Require(bool value, string name)
    {
        if (!value)
        {
            throw new InvalidDataException(AsyncOriginalComparisonNames.MismatchPrefix + name);
        }
    }

    private static Term RoleStack(Term exception)
    {
        TupleTerm outcome = Tuple(exception, AsyncOriginalComparisonNames.ExceptionOutcomeArity);
        Require(
            outcome.Items[AsyncOriginalComparisonNames.OutcomeTagIndex].Equals(Term.A(AsyncOriginalComparisonNames.ExceptionTag))
                && outcome.Items[AsyncOriginalComparisonNames.ExceptionClassIndex].Equals(Term.A(AsyncOriginalComparisonNames.ErrorClassAtom))
                && outcome.Items[AsyncOriginalComparisonNames.ExceptionReasonIndex].Equals(Term.A(AsyncOriginalComparisonNames.BadArgumentAtom)),
            AsyncOriginalComparisonNames.InvalidControlExceptionAssertion
        );
        Term[] frames = Cons.Items(outcome.Items[AsyncOriginalComparisonNames.ExceptionStackIndex]).ToArray();
        Require(frames.Length == AsyncOriginalComparisonNames.FullStackFrameCount, AsyncOriginalComparisonNames.CompleteStackAssertion);
        TupleTerm first = Tuple(frames[AsyncOriginalComparisonNames.FirstStackFrameIndex], AsyncOriginalComparisonNames.StackFrameArity);
        Term[] arguments = Cons.Items(first.Items[AsyncOriginalComparisonNames.FrameArgumentsIndex]).ToArray();
        Require(
            first.Items[AsyncOriginalComparisonNames.FrameModuleIndex].Equals(Term.A(AsyncOriginalComparisonNames.ErlangModule))
                && first.Items[AsyncOriginalComparisonNames.FrameFunctionIndex].Equals(Term.A(AsyncOriginalComparisonNames.PortControlFunction))
                && arguments.Length == AsyncOriginalComparisonNames.PortControlArgumentCount
                && arguments[AsyncOriginalComparisonNames.PortArgumentIndex] is PortTerm,
            AsyncOriginalComparisonNames.LiveFixturePortAssertion
        );
        // Singleton bijection: THIS invalid_control/with_port live P only. Raw bytes remain unchanged.
        arguments[AsyncOriginalComparisonNames.PortArgumentIndex] = Term.A(AsyncOriginalComparisonNames.FixturePortRoleAtom);
        frames[AsyncOriginalComparisonNames.FirstStackFrameIndex] = Term.Tuple(
            first.Items[AsyncOriginalComparisonNames.FrameModuleIndex],
            first.Items[AsyncOriginalComparisonNames.FrameFunctionIndex],
            Cons.From(arguments),
            first.Items[AsyncOriginalComparisonNames.FrameMetadataIndex]
        );

        return Term.Tuple(
            outcome.Items[AsyncOriginalComparisonNames.OutcomeTagIndex],
            outcome.Items[AsyncOriginalComparisonNames.ExceptionClassIndex],
            outcome.Items[AsyncOriginalComparisonNames.ExceptionReasonIndex],
            Cons.From(frames)
        );
    }

    internal static void Compare(Term actual, Term original, int count)
    {
        var actualMap = (MapTerm)actual;
        var wrapper = (MapTerm)original;
        Require(
            At(wrapper, AsyncOriginalComparisonNames.LoadKey).Equals(Term.Tuple(Term.A(AsyncOriginalComparisonNames.ReturnTag), Term.A(AsyncOriginalComparisonNames.OkAtom))),
            AsyncOriginalComparisonNames.OriginalLoadAssertion
        );
        TupleTerm returned = Tuple(At(wrapper, AsyncOriginalComparisonNames.ResultKey), AsyncOriginalComparisonNames.ReturnedOutcomeArity);
        Require(
            returned.Items[AsyncOriginalComparisonNames.OutcomeTagIndex].Equals(Term.A(AsyncOriginalComparisonNames.ReturnTag)),
            AsyncOriginalComparisonNames.OriginalResultAssertion
        );
        var expectedMap = (MapTerm)returned.Items[AsyncOriginalComparisonNames.ReturnValueIndex];
        Require(
            At(actualMap, AsyncOriginalComparisonNames.ThreadPoolSizeKey).Equals(Term.I(count))
                && At(expectedMap, AsyncOriginalComparisonNames.ThreadPoolSizeKey).Equals(Term.I(count)),
            AsyncOriginalComparisonNames.ConfiguredCountAssertion
        );
        foreach (string flag in new[]
        {
            AsyncOriginalComparisonNames.AllCasesReturnedKey,
            AsyncOriginalComparisonNames.FinalLiveJobsZeroKey,
            AsyncOriginalComparisonNames.FinalOnlyControllerStateKey
        })
        {
            Require(
                At(actualMap, flag).Equals(Term.A(AsyncOriginalComparisonNames.TrueAtom)) && At(actualMap, flag).Equals(At(expectedMap, flag)),
                flag
            );
        }
        Require(
            At(actualMap, AsyncOriginalComparisonNames.ManagedNegativeControlsKey).Equals(At(expectedMap, AsyncOriginalComparisonNames.ManagedNegativeControlsKey)),
            AsyncOriginalComparisonNames.ManagedNegativeBoundaryAssertion
        );
        Require(
            At(actualMap, AsyncOriginalComparisonNames.FinalNativeKey).Equals(At(expectedMap, AsyncOriginalComparisonNames.FinalNativeKey)),
            AsyncOriginalComparisonNames.FinalPhysicalCountsAssertion
        );
        Term[] cases = Cons.Items(At(actualMap, AsyncOriginalComparisonNames.CasesKey)).ToArray(), expected = Cons.Items(At(expectedMap, AsyncOriginalComparisonNames.CasesKey)).ToArray();
        Require(
            cases.Length == AsyncOriginalComparisonNames.CaseCount
            && expected.Length == AsyncOriginalComparisonNames.CaseCount,
            AsyncOriginalComparisonNames.AllOutcomesAssertion
        );
        int predicates = 0, excluded = 0;
        for (int index = 0; index < cases.Length; index++)
        {
            var row = (MapTerm)cases[index];
            var baseline = (MapTerm)expected[index];
            Require(
                At(row, AsyncOriginalComparisonNames.IdKey).Equals(At(baseline, AsyncOriginalComparisonNames.IdKey)),
                AsyncOriginalComparisonNames.CaseOrderAssertion
            );
            Require(
                At(row, AsyncOriginalComparisonNames.DisableCleanupKey).Equals(At(baseline, AsyncOriginalComparisonNames.DisableCleanupKey)),
                AsyncOriginalComparisonNames.DisableCleanupAssertion
            );
            TupleTerm observed = Tuple(At(row, AsyncOriginalComparisonNames.OutcomeKey), AsyncOriginalComparisonNames.ReturnedOutcomeArity), originalOutcome = Tuple(At(baseline, AsyncOriginalComparisonNames.OutcomeKey), AsyncOriginalComparisonNames.ReturnedOutcomeArity);
            Require(
                observed.Items[AsyncOriginalComparisonNames.OutcomeTagIndex].Equals(Term.A(AsyncOriginalComparisonNames.ReturnTag))
                && originalOutcome.Items[AsyncOriginalComparisonNames.OutcomeTagIndex].Equals(Term.A(AsyncOriginalComparisonNames.ReturnTag)),
                AsyncOriginalComparisonNames.ReturnedCaseAssertion
            );
            var values = (MapTerm)observed.Items[AsyncOriginalComparisonNames.ReturnValueIndex];
            var originals = (MapTerm)originalOutcome.Items[AsyncOriginalComparisonNames.ReturnValueIndex];
            Require(
                values.Entries.Count == originals.Entries.Count
                    && values.Entries.All(entry => originals.Entries.Any(other => other.Key.Equals(entry.Key))),
                AsyncOriginalComparisonNames.CaseFieldsAssertion
            );
            foreach (var entry in values.Entries)
            {
                if (entry.Value is Atom boolean && boolean.Name is AsyncOriginalComparisonNames.TrueAtom or AsyncOriginalComparisonNames.FalseAtom)
                {
                    Require(entry.Value.Equals(originals.Get(entry.Key)), AsyncOriginalComparisonNames.BooleanFieldAssertion);
                    predicates++;
                }
                else if (entry.Value is Atom or Integer && !entry.Key.Equals(Term.A(AsyncOriginalComparisonNames.SleepDeltaKey)))
                {
                    Require(entry.Value.Equals(originals.Get(entry.Key)), AsyncOriginalComparisonNames.ScalarFieldAssertion);
                }
            }
            if (values.Entries.Any(entry => entry.Key.Equals(Term.A(AsyncOriginalComparisonNames.StatusKey))
                && entry.Value.Equals(Term.A(AsyncOriginalComparisonNames.NotExecutedAtom))))
            {
                Require(values.Equals(originals), AsyncOriginalComparisonNames.ExcludedBoundaryAssertion);
                excluded++;
            }
            if (index == AsyncOriginalComparisonNames.TopologyCaseIndex)
            {
                foreach (string key in new[]
                {
                    AsyncOriginalComparisonNames.ActualCountKey,
                    AsyncOriginalComparisonNames.ExpectedCountKey,
                    AsyncOriginalComparisonNames.IdsKey
                })
                {
                    Require(At(values, key).Equals(At(originals, key)), AsyncOriginalComparisonNames.TopologyScalarPrefix + key);
                }
            }
            if (index == AsyncOriginalComparisonNames.BusyEnableCaseIndex)
            {
                Require(
                    At(values, AsyncOriginalComparisonNames.PortDeltaKey).Equals(At(originals, AsyncOriginalComparisonNames.PortDeltaKey)),
                    AsyncOriginalComparisonNames.EnablePortDeltaAssertion
                );
                Require(
                    At(values, AsyncOriginalComparisonNames.SleepDeltaKey) is Integer i
                        && i.Value > 0
                        && At(originals, AsyncOriginalComparisonNames.SleepDeltaKey) is Integer b
                        && b.Value > 0,
                    AsyncOriginalComparisonNames.EnableSleepDeltaAssertion
                );
            }
            if (index == AsyncOriginalComparisonNames.OwnerCloseCaseIndex)
            {
                Require(
                    At(values, AsyncOriginalComparisonNames.OwnerDownKey).Equals(At(originals, AsyncOriginalComparisonNames.OwnerDownKey)),
                    AsyncOriginalComparisonNames.OwnerReasonAssertion
                );
                Require(
                    At(values, AsyncOriginalComparisonNames.AfterCloseKey).Equals(At(originals, AsyncOriginalComparisonNames.AfterCloseKey)),
                    AsyncOriginalComparisonNames.OwnerFinalSnapshotAssertion
                );
            }
            if (index == AsyncOriginalComparisonNames.InvalidControlCaseIndex)
            {
                Require(
                    RoleStack(At(values, AsyncOriginalComparisonNames.InvalidControlKey)).Equals(RoleStack(At(originals, AsyncOriginalComparisonNames.InvalidControlKey))),
                    AsyncOriginalComparisonNames.WholeRoleStackAssertion
                );
            }
        }
        Require(
            predicates == (count == AsyncOriginalComparisonNames.SingleAsyncWorkerCount ? AsyncOriginalComparisonNames.SingleWorkerPredicateCount : AsyncOriginalComparisonNames.TwoWorkerPredicateCount)
            && excluded == (count == AsyncOriginalComparisonNames.SingleAsyncWorkerCount ? AsyncOriginalComparisonNames.SingleWorkerExcludedCount : AsyncOriginalComparisonNames.TwoWorkerExcludedCount),
            AsyncOriginalComparisonNames.FiniteCountAssertion
        );
        // No sorting, rewriting, or equality claim for dynamic raw whole rows/default topology.
    }
}
