                case ListUniqueMergeState.Usort1:
                {
                    if (arguments[0] is Cons { Tail: Cons second } first)
                    {
                        Term x = first.Head;
                        Term y = second.Head;
                        Term rest = second.Tail;
                        if (TermOrder.Compare(x, y) < 0)
                        {
                            if (rest is Nil)
                                result = arguments[0];
                            else if (rest is Cons { Tail: Nil } third)
                            {
                                Term z = third.Head;
                                if (TermOrder.Compare(y, z) < 0)
                                    result = arguments[0];
                                else if (y.NumericEquals(z))
                                    result = new Cons(x, new Cons(y, Nil.Value));
                                else if (TermOrder.Compare(z, x) < 0)
                                    result = new Cons(z, new Cons(x, new Cons(y, Nil.Value)));
                                else if (z.NumericEquals(x))
                                    result = new Cons(x, new Cons(y, Nil.Value));
                                else
                                    result = new Cons(x, new Cons(z, new Cons(y, Nil.Value)));
                            }
                            else
                            {
                                state = ListUniqueMergeState.Usplit_15;
                                arguments = [x, y, rest, Nil.Value, Nil.Value];
                                continue;
                            }
                            complete = true;
                            break;
                        }
                        if (TermOrder.Compare(x, y) > 0)
                        {
                            if (rest is Nil)
                                result = new Cons(y, new Cons(x, Nil.Value));
                            else if (rest is Cons { Tail: Nil } third)
                            {
                                Term z = third.Head;
                                if (TermOrder.Compare(x, z) < 0)
                                    result = new Cons(y, new Cons(x, rest));
                                else if (x.NumericEquals(z))
                                    result = new Cons(y, new Cons(x, Nil.Value));
                                else if (TermOrder.Compare(z, y) < 0)
                                    result = new Cons(z, new Cons(y, new Cons(x, Nil.Value)));
                                else if (z.NumericEquals(y))
                                    result = new Cons(y, new Cons(x, Nil.Value));
                                else
                                    result = new Cons(y, new Cons(z, new Cons(x, Nil.Value)));
                            }
                            else
                            {
                                state = ListUniqueMergeState.Usplit_25;
                                arguments = [x, y, rest, Nil.Value, Nil.Value];
                                continue;
                            }
                            complete = true;
                            break;
                        }
                        state = ListUniqueMergeState.Usort_12;
                        arguments = [x, rest];
                        continue;
                    }
                    if (arguments[0] is Nil || arguments[0] is Cons { Tail: Nil })
                    {
                        result = arguments[0];
                        complete = true;
                        break;
                    }
                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);
                }
