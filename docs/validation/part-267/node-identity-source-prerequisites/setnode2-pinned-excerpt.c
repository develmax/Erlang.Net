BIF_RETTYPE setnode_2(BIF_ALIST_2)
{
    Process *net_kernel = NULL;
    Uint32 creation;
    int success;

    /* valid creation ? */
    if(!term_to_Uint32(BIF_ARG_2, &creation))
	goto error;

    /* valid node name ? */
    if (!is_node_name_atom(BIF_ARG_1))
	goto error;

    if (BIF_ARG_1 == am_Noname) /* can't use this name !! */
	goto error;
    if (erts_is_alive)     /* must not be alive! */
	goto error;

    /* Check that all trap functions are defined !! */
    if (dmonitor_node_trap->dispatch.addresses[0] == NULL) {
	goto error;
    }

    net_kernel = erts_whereis_process(BIF_P,
                                      ERTS_PROC_LOCK_MAIN,
				      am_net_kernel,
                                      0,
                                      ERTS_P2P_FLG_INC_REFC);
    if (!net_kernel)
	goto error;

#ifdef DEBUG
    erts_rwmtx_rlock(&erts_dist_table_rwmtx);
    ASSERT(!erts_visible_dist_entries && !erts_hidden_dist_entries);
    erts_rwmtx_runlock(&erts_dist_table_rwmtx);
#endif

    erts_proc_unlock(BIF_P, ERTS_PROC_LOCK_MAIN);
    
    erts_thr_progress_block();

    success = (!ERTS_PROC_IS_EXITING(net_kernel)
               && !ERTS_PROC_GET_DIST_ENTRY(net_kernel));
    if (success) {
        /*
         * Ensure we don't use a nodename-creation pair with
         * external identifiers existing in the system.
         */
        while (!0) {
            ErlNode *nep;
            if (creation < 4)
                creation = 4;
            nep = erts_find_node(BIF_ARG_1, creation);
            if (!nep || erts_node_refc(nep) == 0)
                break;
            creation++;
        }
        inc_no_nodes();
        erts_set_this_node(BIF_ARG_1, (Uint32) creation);
        erts_this_dist_entry->creation = creation;
        erts_is_alive = 1;
        send_nodes_mon_msgs(NULL, am_nodeup, BIF_ARG_1, ~((Uint32) 0),
                            am_visible, NIL);
        erts_proc_lock(net_kernel, ERTS_PROC_LOCKS_ALL);

        /* By setting F_DISTRIBUTION on net_kernel,
         * erts_do_net_exits will be called when net_kernel is terminated !! */
        net_kernel->flags |= F_DISTRIBUTION;

        /*
         * Note erts_this_dist_entry is changed by erts_set_this_node(),
         * so we *need* to use the new one after erts_set_this_node()
         * is called.
         */
        erts_ref_dist_entry(erts_this_dist_entry);
        ERTS_PROC_SET_DIST_ENTRY(net_kernel, erts_this_dist_entry);
        erts_proc_unlock(net_kernel, ERTS_PROC_LOCKS_ALL);
    }
    
    erts_thr_progress_unblock();

    erts_proc_lock(BIF_P, ERTS_PROC_LOCK_MAIN);
    
    if (success) {
        erts_proc_dec_refc(net_kernel);
        BIF_RET(am_true);
    }

 error:
    if (net_kernel)
        erts_proc_dec_refc(net_kernel);
    BIF_ERROR(BIF_P, BADARG);
