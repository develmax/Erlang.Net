namespace Erlang;

internal static class ListUniqueMergeMachine
{
    internal static async ValueTask<Term> Run(ProcessContext context, Term lists)
    {
        using var tail = context.EnterSourceTailCall(ListModuleNames.Module, ListModuleNames.UniqueMerge);
        var continuations = new Stack<ListUniqueMergeContinuation>();
        var state = ListUniqueMergeState.Umergel1;
        Term[] arguments = [lists];
        while (true)
        {
            await context.ReduceAsync();
            Term? result = null;
            bool complete = false;
            switch (state)
            {
                case ListUniqueMergeState.Umergel1:
                {
                    {
                        if (arguments[0] is var vL)
                        {
                            state = ListUniqueMergeState.Umergel3;
                            arguments = [vL, Nil.Value, ListUniqueMergeNames.AscTerm];
                            continue;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Umergel3:
                {
                    {
                        if (arguments[0] is Nil && arguments[1] is Nil && arguments[2] is var v_O)
                        {
                            result = Nil.Value;
                            complete = true;
                            break;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is Cons cell1 && cell0.Head is var vL && cell0.Tail is Nil && arguments[1] is Nil && arguments[2] is var v_O)
                        {
                            result = vL;
                            complete = true;
                            break;
                        }
                    }
                    {
                        if (arguments[0] is Nil && arguments[1] is var vAcc && arguments[2] is var vO)
                        {
                            state = ListUniqueMergeState.Rumergel3;
                            arguments = [vAcc, Nil.Value, vO];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is Cons cell1 && cell0.Head is var vL && cell0.Tail is Nil && arguments[1] is var vAcc && arguments[2] is var vO)
                        {
                            continuations.Push(new ListUniqueMergeContinuation(
                                ListUniqueMergeState.Rumergel3,
                                [Nil.Value, Nil.Value, vO],
                                0,
                                vAcc,
                                state,
                                ListUniqueMergeNames.CallLine3294
                            ));
                            result = await ListUniqueMergeTrace.Reverse(context, vL, Nil.Value, continuations);
                            complete = true;
                            break;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is Nil && cell0.Tail is var vL && arguments[1] is var vAcc && arguments[2] is var vO)
                        {
                            state = ListUniqueMergeState.Umergel3;
                            arguments = [vL, vAcc, vO];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is Cons cell1 && cell0.Head is var vA && cell0.Tail is Cons cell2 && cell2.Head is Nil && cell2.Tail is var vL && arguments[1] is var vAcc && arguments[2] is var vO)
                        {
                            state = ListUniqueMergeState.Umergel3;
                            arguments = [new Cons(vA, vL), vAcc, vO];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is Cons cell1 && cell0.Head is var vA && cell0.Tail is Cons cell2 && cell2.Head is Cons cell3 && cell2.Head is var vB && cell2.Tail is Cons cell4 && cell4.Head is Nil && cell4.Tail is var vL && arguments[1] is var vAcc && arguments[2] is var vO)
                        {
                            state = ListUniqueMergeState.Umergel3;
                            arguments = [new Cons(vA, new Cons(vB, vL)), vAcc, vO];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is Cons cell1 && cell0.Head is var vT1 && cell0.Tail is Cons cell2 && cell2.Head is Cons cell3 && cell3.Head is var vH2 && cell3.Tail is var vT2 && cell2.Tail is Cons cell4 && cell4.Head is Cons cell5 && cell5.Head is var vH3 && cell5.Tail is var vT3 && cell4.Tail is var vL && arguments[1] is var vAcc && arguments[2] is Atom { Name: ListUniqueMergeNames.Asc })
                        {
                            continuations.Push(new ListUniqueMergeContinuation(
                                ListUniqueMergeState.Umergel3,
                                [vL, Nil.Value, ListUniqueMergeNames.AscTerm],
                                1,
                                vAcc,
                                state,
                                ListUniqueMergeNames.CallLine3302
                            ));
                            state = ListUniqueMergeState.Umerge3_17;
                            arguments = [vT1, new Cons(vH2, vH3), vT2, vH2, Nil.Value, vT3, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is Cons cell1 && cell1.Head is var vH3 && cell1.Tail is var vT3 && cell0.Tail is Cons cell2 && cell2.Head is Cons cell3 && cell3.Head is var vH2 && cell3.Tail is var vT2 && cell2.Tail is Cons cell4 && cell4.Head is Cons cell5 && cell4.Head is var vT1 && cell4.Tail is var vL && arguments[1] is var vAcc && arguments[2] is Atom { Name: ListUniqueMergeNames.Desc })
                        {
                            continuations.Push(new ListUniqueMergeContinuation(
                                ListUniqueMergeState.Umergel3,
                                [vL, Nil.Value, ListUniqueMergeNames.DescTerm],
                                1,
                                vAcc,
                                state,
                                ListUniqueMergeNames.CallLine3304
                            ));
                            state = ListUniqueMergeState.Umerge3_17;
                            arguments = [vT1, new Cons(vH2, vH3), vT2, vH2, Nil.Value, vT3, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is Cons cell1 && cell1.Head is var vH1 && cell1.Tail is var vT1 && cell0.Tail is Cons cell2 && cell2.Head is Cons cell3 && cell2.Head is var vT2 && cell2.Tail is var vL && arguments[1] is var vAcc && arguments[2] is Atom { Name: ListUniqueMergeNames.Asc })
                        {
                            continuations.Push(new ListUniqueMergeContinuation(
                                ListUniqueMergeState.Umergel3,
                                [vL, Nil.Value, ListUniqueMergeNames.AscTerm],
                                1,
                                vAcc,
                                state,
                                ListUniqueMergeNames.CallLine3306
                            ));
                            state = ListUniqueMergeState.Umerge2_24;
                            arguments = [vT1, vT2, Nil.Value, vH1];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is Cons cell1 && cell0.Head is var vT2 && cell0.Tail is Cons cell2 && cell2.Head is Cons cell3 && cell3.Head is var vH1 && cell3.Tail is var vT1 && cell2.Tail is var vL && arguments[1] is var vAcc && arguments[2] is Atom { Name: ListUniqueMergeNames.Desc })
                        {
                            continuations.Push(new ListUniqueMergeContinuation(
                                ListUniqueMergeState.Umergel3,
                                [vL, Nil.Value, ListUniqueMergeNames.DescTerm],
                                1,
                                vAcc,
                                state,
                                ListUniqueMergeNames.CallLine3308
                            ));
                            state = ListUniqueMergeState.Umerge2_24;
                            arguments = [vT1, vT2, Nil.Value, vH1];
                            continue;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Rumergel3:
                {
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is Cons cell1 && cell1.Head is var vH3 && cell1.Tail is var vT3 && cell0.Tail is Cons cell2 && cell2.Head is Cons cell3 && cell3.Head is var vH2 && cell3.Tail is var vT2 && cell2.Tail is Cons cell4 && cell4.Head is var vT1 && cell4.Tail is var vL && arguments[1] is var vAcc && arguments[2] is Atom { Name: ListUniqueMergeNames.Asc })
                        {
                            continuations.Push(new ListUniqueMergeContinuation(
                                ListUniqueMergeState.Rumergel3,
                                [vL, Nil.Value, ListUniqueMergeNames.AscTerm],
                                1,
                                vAcc,
                                state,
                                ListUniqueMergeNames.CallLine3311
                            ));
                            state = ListUniqueMergeState.Rumerge3_16;
                            arguments = [vT1, vT2, vH2, Nil.Value, vT3, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is var vT1 && cell0.Tail is Cons cell1 && cell1.Head is Cons cell2 && cell2.Head is var vH2 && cell2.Tail is var vT2 && cell1.Tail is Cons cell3 && cell3.Head is Cons cell4 && cell4.Head is var vH3 && cell4.Tail is var vT3 && cell3.Tail is var vL && arguments[1] is var vAcc && arguments[2] is Atom { Name: ListUniqueMergeNames.Desc })
                        {
                            continuations.Push(new ListUniqueMergeContinuation(
                                ListUniqueMergeState.Rumergel3,
                                [vL, Nil.Value, ListUniqueMergeNames.DescTerm],
                                1,
                                vAcc,
                                state,
                                ListUniqueMergeNames.CallLine3313
                            ));
                            state = ListUniqueMergeState.Rumerge3_16;
                            arguments = [vT1, vT2, vH2, Nil.Value, vT3, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is Cons cell1 && cell1.Head is var vH2 && cell1.Tail is var vT2 && cell0.Tail is Cons cell2 && cell2.Head is var vT1 && cell2.Tail is var vL && arguments[1] is var vAcc && arguments[2] is Atom { Name: ListUniqueMergeNames.Asc })
                        {
                            continuations.Push(new ListUniqueMergeContinuation(
                                ListUniqueMergeState.Rumergel3,
                                [vL, Nil.Value, ListUniqueMergeNames.AscTerm],
                                1,
                                vAcc,
                                state,
                                ListUniqueMergeNames.CallLine3315
                            ));
                            state = ListUniqueMergeState.Rumerge2_14;
                            arguments = [vT1, vT2, Nil.Value, vH2];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is var vT1 && cell0.Tail is Cons cell1 && cell1.Head is Cons cell2 && cell2.Head is var vH2 && cell2.Tail is var vT2 && cell1.Tail is var vL && arguments[1] is var vAcc && arguments[2] is Atom { Name: ListUniqueMergeNames.Desc })
                        {
                            continuations.Push(new ListUniqueMergeContinuation(
                                ListUniqueMergeState.Rumergel3,
                                [vL, Nil.Value, ListUniqueMergeNames.DescTerm],
                                1,
                                vAcc,
                                state,
                                ListUniqueMergeNames.CallLine3317
                            ));
                            state = ListUniqueMergeState.Rumerge2_14;
                            arguments = [vT1, vT2, Nil.Value, vH2];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is var vL && cell0.Tail is Nil && arguments[1] is var vAcc && arguments[2] is var vO)
                        {
                            continuations.Push(new ListUniqueMergeContinuation(
                                ListUniqueMergeState.Umergel3,
                                [Nil.Value, Nil.Value, vO],
                                0,
                                vAcc,
                                state,
                                ListUniqueMergeNames.CallLine3319
                            ));
                            result = await ListUniqueMergeTrace.Reverse(context, vL, Nil.Value, continuations);
                            complete = true;
                            break;
                        }
                    }
                    {
                        if (arguments[0] is Nil && arguments[1] is var vAcc && arguments[2] is var vO)
                        {
                            state = ListUniqueMergeState.Umergel3;
                            arguments = [vAcc, Nil.Value, vO];
                            continue;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Umerge3_17:
                {
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is var vH1 && cell0.Tail is var vT1 && arguments[1] is var vHdM && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3 && TermOrder.Compare(vH1, vH2) <= 0)
                        {
                            state = ListUniqueMergeState.Umerge3_128;
                            arguments = [vT1, vH1, vT2, vH2, vM, vT3, vH3, vHdM];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is var vH1 && cell0.Tail is var vT1 && arguments[1] is var vHdM && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3 && vH2.NumericEquals(vHdM))
                        {
                            state = ListUniqueMergeState.Umerge3_27;
                            arguments = [vT1, vH1, vT2, vH2, vM, vT3, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is var vH1 && cell0.Tail is var vT1 && arguments[1] is var vHdM && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3)
                        {
                            state = ListUniqueMergeState.Umerge3_218;
                            arguments = [vT1, vH1, vT2, vH2, vM, vT3, vH3, vHdM];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Nil && arguments[1] is var vHdM && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3 && vH2.NumericEquals(vHdM))
                        {
                            state = ListUniqueMergeState.Umerge2_15;
                            arguments = [vT2, vT3, vM, vHdM, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Nil && arguments[1] is var v_HdM && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3 && TermOrder.Compare(vH2, vH3) <= 0)
                        {
                            state = ListUniqueMergeState.Umerge2_15;
                            arguments = [vT2, vT3, new Cons(vH2, vM), vH2, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Nil && arguments[1] is var vHdM && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3 && vH3.NumericEquals(vHdM))
                        {
                            state = ListUniqueMergeState.Umerge2_24;
                            arguments = [vT2, vT3, vM, vH2];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Nil && arguments[1] is var v_HdM && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3)
                        {
                            state = ListUniqueMergeState.Umerge2_24;
                            arguments = [vT2, vT3, new Cons(vH3, vM), vH2];
                            continue;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Umerge3_27:
                {
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is Cons cell0 && cell0.Head is var vH2 && cell0.Tail is var vT2 && arguments[3] is var vHdM && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3 && TermOrder.Compare(vH1, vH2) <= 0)
                        {
                            state = ListUniqueMergeState.Umerge3_128;
                            arguments = [vT1, vH1, vT2, vH2, vM, vT3, vH3, vHdM];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is Cons cell0 && cell0.Head is var vH2 && cell0.Tail is var vT2 && arguments[3] is var vHdM && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3)
                        {
                            state = ListUniqueMergeState.Umerge3_218;
                            arguments = [vT1, vH1, vT2, vH2, vM, vT3, vH3, vHdM];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is Nil && arguments[3] is var v_HdM && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3 && TermOrder.Compare(vH1, vH3) <= 0)
                        {
                            state = ListUniqueMergeState.Umerge2_15;
                            arguments = [vT1, vT3, new Cons(vH1, vM), vH1, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is Nil && arguments[3] is var vHdM && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3 && vH3.NumericEquals(vHdM))
                        {
                            state = ListUniqueMergeState.Umerge2_24;
                            arguments = [vT1, vT3, vM, vH1];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is Nil && arguments[3] is var v_HdM && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3)
                        {
                            state = ListUniqueMergeState.Umerge2_24;
                            arguments = [vT1, vT3, new Cons(vH3, vM), vH1];
                            continue;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Umerge3_128:
                {
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3 && arguments[7] is var v_HdM && TermOrder.Compare(vH1, vH3) <= 0)
                        {
                            state = ListUniqueMergeState.Umerge3_17;
                            arguments = [vT1, vH1, vT2, vH2, new Cons(vH1, vM), vT3, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3 && arguments[7] is var vHdM && vH3.NumericEquals(vHdM))
                        {
                            state = ListUniqueMergeState.Umerge3_12_36;
                            arguments = [vT1, vH1, vT2, vH2, vM, vT3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3 && arguments[7] is var v_HdM)
                        {
                            state = ListUniqueMergeState.Umerge3_12_36;
                            arguments = [vT1, vH1, vT2, vH2, new Cons(vH3, vM), vT3];
                            continue;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Umerge3_12_36:
                {
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is Cons cell0 && cell0.Head is var vH3 && cell0.Tail is var vT3 && TermOrder.Compare(vH1, vH3) <= 0)
                        {
                            state = ListUniqueMergeState.Umerge3_17;
                            arguments = [vT1, vH1, vT2, vH2, new Cons(vH1, vM), vT3, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is Cons cell0 && cell0.Head is var vH3 && cell0.Tail is var vT3)
                        {
                            state = ListUniqueMergeState.Umerge3_12_36;
                            arguments = [vT1, vH1, vT2, vH2, new Cons(vH3, vM), vT3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is Nil)
                        {
                            state = ListUniqueMergeState.Umerge2_15;
                            arguments = [vT1, vT2, new Cons(vH1, vM), vH1, vH2];
                            continue;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Umerge3_218:
                {
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3 && arguments[7] is var v_HdM && TermOrder.Compare(vH2, vH3) <= 0)
                        {
                            state = ListUniqueMergeState.Umerge3_27;
                            arguments = [vT1, vH1, vT2, vH2, new Cons(vH2, vM), vT3, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3 && arguments[7] is var vHdM && vH3.NumericEquals(vHdM))
                        {
                            state = ListUniqueMergeState.Umerge3_21_36;
                            arguments = [vT1, vH1, vT2, vH2, vM, vT3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3 && arguments[7] is var v_HdM)
                        {
                            state = ListUniqueMergeState.Umerge3_21_36;
                            arguments = [vT1, vH1, vT2, vH2, new Cons(vH3, vM), vT3];
                            continue;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Umerge3_21_36:
                {
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is Cons cell0 && cell0.Head is var vH3 && cell0.Tail is var vT3 && TermOrder.Compare(vH2, vH3) <= 0)
                        {
                            state = ListUniqueMergeState.Umerge3_27;
                            arguments = [vT1, vH1, vT2, vH2, new Cons(vH2, vM), vT3, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is Cons cell0 && cell0.Head is var vH3 && cell0.Tail is var vT3)
                        {
                            state = ListUniqueMergeState.Umerge3_21_36;
                            arguments = [vT1, vH1, vT2, vH2, new Cons(vH3, vM), vT3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is Nil)
                        {
                            state = ListUniqueMergeState.Umerge2_24;
                            arguments = [vT1, vT2, new Cons(vH2, vM), vH1];
                            continue;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Rumerge3_16:
                {
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is var vH1 && cell0.Tail is var vT1 && arguments[1] is var vT2 && arguments[2] is var vH2 && arguments[3] is var vM && arguments[4] is var vT3 && arguments[5] is var vH3 && TermOrder.Compare(vH1, vH2) <= 0)
                        {
                            state = ListUniqueMergeState.Rumerge3_12a7;
                            arguments = [vT1, vH1, vT2, vH2, vM, vT3, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is var vH1 && cell0.Tail is var vT1 && arguments[1] is var vT2 && arguments[2] is var vH2 && arguments[3] is var vM && arguments[4] is var vT3 && arguments[5] is var vH3 && TermOrder.Compare(vH1, vH3) <= 0)
                        {
                            state = ListUniqueMergeState.Rumerge3_21_37;
                            arguments = [vT1, vT2, vH2, vM, vT3, vH3, vH1];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is var vH1 && cell0.Tail is var vT1 && arguments[1] is var vT2 && arguments[2] is var vH2 && arguments[3] is var vM && arguments[4] is var vT3 && arguments[5] is var vH3)
                        {
                            state = ListUniqueMergeState.Rumerge3_16;
                            arguments = [vT1, vT2, vH2, new Cons(vH1, vM), vT3, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Nil && arguments[1] is var vT2 && arguments[2] is var vH2 && arguments[3] is var vM && arguments[4] is var vT3 && arguments[5] is var vH3 && TermOrder.Compare(vH2, vH3) <= 0)
                        {
                            state = ListUniqueMergeState.Rumerge2_25;
                            arguments = [vT2, vT3, vM, vH3, vH2];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Nil && arguments[1] is var vT2 && arguments[2] is var vH2 && arguments[3] is var vM && arguments[4] is var vT3 && arguments[5] is var vH3)
                        {
                            state = ListUniqueMergeState.Rumerge2_14;
                            arguments = [vT2, vT3, new Cons(vH2, vM), vH3];
                            continue;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Rumerge3_12a7:
                {
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3 && TermOrder.Compare(vH2, vH3) <= 0)
                        {
                            state = ListUniqueMergeState.Rumerge3_12_37;
                            arguments = [vT1, vT2, vH2, vM, vT3, vH3, vH1];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3)
                        {
                            state = ListUniqueMergeState.Rumerge3_27;
                            arguments = [vT1, vT2, vH2, vM, vT3, vH3, vH1];
                            continue;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Rumerge3_27:
                {
                    {
                        if (arguments[0] is var vT1 && arguments[1] is Cons cell0 && cell0.Head is var vH2 && cell0.Tail is var vT2 && arguments[2] is var vH2M && arguments[3] is var vM && arguments[4] is var vT3 && arguments[5] is var vH3 && arguments[6] is var vH1 && TermOrder.Compare(vH1, vH2) <= 0)
                        {
                            state = ListUniqueMergeState.Rumerge3_12b8;
                            arguments = [vT1, vH1, vT2, vH2, vM, vT3, vH3, vH2M];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is Cons cell0 && cell0.Head is var vH2 && cell0.Tail is var vT2 && arguments[2] is var vH2M && arguments[3] is var vM && arguments[4] is var vT3 && arguments[5] is var vH3 && arguments[6] is var vH1 && vH1.NumericEquals(vH2M))
                        {
                            state = ListUniqueMergeState.Rumerge3_16;
                            arguments = [vT1, vT2, vH2, new Cons(vH1, vM), vT3, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is Cons cell0 && cell0.Head is var vH2 && cell0.Tail is var vT2 && arguments[2] is var vH2M && arguments[3] is var vM && arguments[4] is var vT3 && arguments[5] is var vH3 && arguments[6] is var vH1 && TermOrder.Compare(vH1, vH3) <= 0)
                        {
                            state = ListUniqueMergeState.Rumerge3_21_37;
                            arguments = [vT1, vT2, vH2, new Cons(vH2M, vM), vT3, vH3, vH1];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is Cons cell0 && cell0.Head is var vH2 && cell0.Tail is var vT2 && arguments[2] is var vH2M && arguments[3] is var vM && arguments[4] is var vT3 && arguments[5] is var vH3 && arguments[6] is var vH1)
                        {
                            state = ListUniqueMergeState.Rumerge3_16;
                            arguments = [vT1, vT2, vH2, new Cons(vH1, new Cons(vH2M, vM)), vT3, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is Nil && arguments[2] is var vH2M && arguments[3] is var vM && arguments[4] is var vT3 && arguments[5] is var vH3 && arguments[6] is var vH1 && vH1.NumericEquals(vH2M))
                        {
                            state = ListUniqueMergeState.Rumerge2_14;
                            arguments = [vT1, vT3, new Cons(vH1, vM), vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is Nil && arguments[2] is var vH2M && arguments[3] is var vM && arguments[4] is var vT3 && arguments[5] is var vH3 && arguments[6] is var vH1 && TermOrder.Compare(vH1, vH3) <= 0)
                        {
                            state = ListUniqueMergeState.Rumerge2_25;
                            arguments = [vT1, vT3, new Cons(vH2M, vM), vH3, vH1];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is Nil && arguments[2] is var vH2M && arguments[3] is var vM && arguments[4] is var vT3 && arguments[5] is var vH3 && arguments[6] is var vH1)
                        {
                            state = ListUniqueMergeState.Rumerge2_14;
                            arguments = [vT1, vT3, new Cons(vH1, new Cons(vH2M, vM)), vH3];
                            continue;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Rumerge3_12b8:
                {
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3 && arguments[7] is var vH2M && TermOrder.Compare(vH2, vH3) <= 0)
                        {
                            state = ListUniqueMergeState.Rumerge3_12_37;
                            arguments = [vT1, vT2, vH2, new Cons(vH2M, vM), vT3, vH3, vH1];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vH1 && arguments[2] is var vT2 && arguments[3] is var vH2 && arguments[4] is var vM && arguments[5] is var vT3 && arguments[6] is var vH3 && arguments[7] is var vH2M)
                        {
                            state = ListUniqueMergeState.Rumerge3_27;
                            arguments = [vT1, vT2, vH2, new Cons(vH2M, vM), vT3, vH3, vH1];
                            continue;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Rumerge3_12_37:
                {
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vT2 && arguments[2] is var vH2 && arguments[3] is var vM && arguments[4] is Cons cell0 && cell0.Head is var vH3 && cell0.Tail is var vT3 && arguments[5] is var vH3M && arguments[6] is var vH1 && TermOrder.Compare(vH2, vH3) <= 0)
                        {
                            state = ListUniqueMergeState.Rumerge3_12_37;
                            arguments = [vT1, vT2, vH2, new Cons(vH3M, vM), vT3, vH3, vH1];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vT2 && arguments[2] is var vH2 && arguments[3] is var vM && arguments[4] is Cons cell0 && cell0.Head is var vH3 && cell0.Tail is var vT3 && arguments[5] is var vH3M && arguments[6] is var vH1 && vH2.NumericEquals(vH3M))
                        {
                            state = ListUniqueMergeState.Rumerge3_27;
                            arguments = [vT1, vT2, vH2, vM, vT3, vH3, vH1];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vT2 && arguments[2] is var vH2 && arguments[3] is var vM && arguments[4] is Cons cell0 && cell0.Head is var vH3 && cell0.Tail is var vT3 && arguments[5] is var vH3M && arguments[6] is var vH1)
                        {
                            state = ListUniqueMergeState.Rumerge3_27;
                            arguments = [vT1, vT2, vH2, new Cons(vH3M, vM), vT3, vH3, vH1];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vT2 && arguments[2] is var vH2 && arguments[3] is var vM && arguments[4] is Nil && arguments[5] is var vH3M && arguments[6] is var vH1 && vH2.NumericEquals(vH3M))
                        {
                            state = ListUniqueMergeState.Rumerge2_25;
                            arguments = [vT1, vT2, vM, vH2, vH1];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vT2 && arguments[2] is var vH2 && arguments[3] is var vM && arguments[4] is Nil && arguments[5] is var vH3M && arguments[6] is var vH1)
                        {
                            state = ListUniqueMergeState.Rumerge2_25;
                            arguments = [vT1, vT2, new Cons(vH3M, vM), vH2, vH1];
                            continue;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Rumerge3_21_37:
                {
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vT2 && arguments[2] is var vH2 && arguments[3] is var vM && arguments[4] is Cons cell0 && cell0.Head is var vH3 && cell0.Tail is var vT3 && arguments[5] is var vH3M && arguments[6] is var vH1 && TermOrder.Compare(vH1, vH3) <= 0)
                        {
                            state = ListUniqueMergeState.Rumerge3_21_37;
                            arguments = [vT1, vT2, vH2, new Cons(vH3M, vM), vT3, vH3, vH1];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vT2 && arguments[2] is var vH2 && arguments[3] is var vM && arguments[4] is Cons cell0 && cell0.Head is var vH3 && cell0.Tail is var vT3 && arguments[5] is var vH3M && arguments[6] is var vH1 && vH1.NumericEquals(vH3M))
                        {
                            state = ListUniqueMergeState.Rumerge3_16;
                            arguments = [vT1, vT2, vH2, new Cons(vH1, vM), vT3, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vT2 && arguments[2] is var vH2 && arguments[3] is var vM && arguments[4] is Cons cell0 && cell0.Head is var vH3 && cell0.Tail is var vT3 && arguments[5] is var vH3M && arguments[6] is var vH1)
                        {
                            state = ListUniqueMergeState.Rumerge3_16;
                            arguments = [vT1, vT2, vH2, new Cons(vH1, new Cons(vH3M, vM)), vT3, vH3];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vT2 && arguments[2] is var vH2 && arguments[3] is var vM && arguments[4] is Nil && arguments[5] is var vH3M && arguments[6] is var vH1 && vH1.NumericEquals(vH3M))
                        {
                            state = ListUniqueMergeState.Rumerge2_14;
                            arguments = [vT1, vT2, new Cons(vH1, vM), vH2];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is var vT2 && arguments[2] is var vH2 && arguments[3] is var vM && arguments[4] is Nil && arguments[5] is var vH3M && arguments[6] is var vH1)
                        {
                            state = ListUniqueMergeState.Rumerge2_14;
                            arguments = [vT1, vT2, new Cons(vH1, new Cons(vH3M, vM)), vH2];
                            continue;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Umerge2_15:
                {
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is var vH1 && cell0.Tail is var vT1 && arguments[1] is var vT2 && arguments[2] is var vM && arguments[3] is var v_HdM && arguments[4] is var vH2 && TermOrder.Compare(vH1, vH2) <= 0)
                        {
                            state = ListUniqueMergeState.Umerge2_15;
                            arguments = [vT1, vT2, new Cons(vH1, vM), vH1, vH2];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is var vH1 && cell0.Tail is var vT1 && arguments[1] is var vT2 && arguments[2] is var vM && arguments[3] is var vHdM && arguments[4] is var vH2 && vH2.NumericEquals(vHdM))
                        {
                            state = ListUniqueMergeState.Umerge2_24;
                            arguments = [vT1, vT2, vM, vH1];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is var vH1 && cell0.Tail is var vT1 && arguments[1] is var vT2 && arguments[2] is var vM && arguments[3] is var v_HdM && arguments[4] is var vH2)
                        {
                            state = ListUniqueMergeState.Umerge2_24;
                            arguments = [vT1, vT2, new Cons(vH2, vM), vH1];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Nil && arguments[1] is var vT2 && arguments[2] is var vM && arguments[3] is var vHdM && arguments[4] is var vH2 && vH2.NumericEquals(vHdM))
                        {
                            result = await ListUniqueMergeTrace.Reverse(context, vT2, vM, continuations);
                            complete = true;
                            break;
                        }
                    }
                    {
                        if (arguments[0] is Nil && arguments[1] is var vT2 && arguments[2] is var vM && arguments[3] is var v_HdM && arguments[4] is var vH2)
                        {
                            result = await ListUniqueMergeTrace.Reverse(context, vT2, new Cons(vH2, vM), continuations);
                            complete = true;
                            break;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Umerge2_24:
                {
                    {
                        if (arguments[0] is var vT1 && arguments[1] is Cons cell0 && cell0.Head is var vH2 && cell0.Tail is var vT2 && arguments[2] is var vM && arguments[3] is var vH1 && TermOrder.Compare(vH1, vH2) <= 0)
                        {
                            state = ListUniqueMergeState.Umerge2_15;
                            arguments = [vT1, vT2, new Cons(vH1, vM), vH1, vH2];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is Cons cell0 && cell0.Head is var vH2 && cell0.Tail is var vT2 && arguments[2] is var vM && arguments[3] is var vH1)
                        {
                            state = ListUniqueMergeState.Umerge2_24;
                            arguments = [vT1, vT2, new Cons(vH2, vM), vH1];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is Nil && arguments[2] is var vM && arguments[3] is var vH1)
                        {
                            result = await ListUniqueMergeTrace.Reverse(context, vT1, new Cons(vH1, vM), continuations);
                            complete = true;
                            break;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Rumerge2_14:
                {
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is var vH1 && cell0.Tail is var vT1 && arguments[1] is var vT2 && arguments[2] is var vM && arguments[3] is var vH2 && TermOrder.Compare(vH1, vH2) <= 0)
                        {
                            state = ListUniqueMergeState.Rumerge2_25;
                            arguments = [vT1, vT2, vM, vH2, vH1];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Cons cell0 && cell0.Head is var vH1 && cell0.Tail is var vT1 && arguments[1] is var vT2 && arguments[2] is var vM && arguments[3] is var vH2)
                        {
                            state = ListUniqueMergeState.Rumerge2_14;
                            arguments = [vT1, vT2, new Cons(vH1, vM), vH2];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is Nil && arguments[1] is var vT2 && arguments[2] is var vM && arguments[3] is var vH2)
                        {
                            result = await ListUniqueMergeTrace.Reverse(context, vT2, new Cons(vH2, vM), continuations);
                            complete = true;
                            break;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                case ListUniqueMergeState.Rumerge2_25:
                {
                    {
                        if (arguments[0] is var vT1 && arguments[1] is Cons cell0 && cell0.Head is var vH2 && cell0.Tail is var vT2 && arguments[2] is var vM && arguments[3] is var vH2M && arguments[4] is var vH1 && TermOrder.Compare(vH1, vH2) <= 0)
                        {
                            state = ListUniqueMergeState.Rumerge2_25;
                            arguments = [vT1, vT2, new Cons(vH2M, vM), vH2, vH1];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is Cons cell0 && cell0.Head is var vH2 && cell0.Tail is var vT2 && arguments[2] is var vM && arguments[3] is var vH2M && arguments[4] is var vH1 && vH1.NumericEquals(vH2M))
                        {
                            state = ListUniqueMergeState.Rumerge2_14;
                            arguments = [vT1, vT2, new Cons(vH1, vM), vH2];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is Cons cell0 && cell0.Head is var vH2 && cell0.Tail is var vT2 && arguments[2] is var vM && arguments[3] is var vH2M && arguments[4] is var vH1)
                        {
                            state = ListUniqueMergeState.Rumerge2_14;
                            arguments = [vT1, vT2, new Cons(vH1, new Cons(vH2M, vM)), vH2];
                            continue;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is Nil && arguments[2] is var vM && arguments[3] is var vH2M && arguments[4] is var vH1 && vH1.NumericEquals(vH2M))
                        {
                            result = await ListUniqueMergeTrace.Reverse(context, vT1, new Cons(vH1, vM), continuations);
                            complete = true;
                            break;
                        }
                    }
                    {
                        if (arguments[0] is var vT1 && arguments[1] is Nil && arguments[2] is var vM && arguments[3] is var vH2M && arguments[4] is var vH1)
                        {
                            result = await ListUniqueMergeTrace.Reverse(context, vT1, new Cons(vH1, new Cons(vH2M, vM)), continuations);
                            complete = true;
                            break;
                        }
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
                default:
                    throw new InvalidOperationException(ListUniqueMergeNames.InvalidState);
            }
            if (complete)
            {
                if (!continuations.TryPop(out var continuation))
                    return result!;
                state = continuation.State;
                arguments = continuation.Arguments;
                arguments[continuation.ResultArgument] = new Cons(result!, continuation.ResultTail);
            }
        }
    }
}
