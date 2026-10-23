-module(ddll452_epmd).
-export([address_please/3,port_please/2]).
address_please(Name,Host,Family)->
    Node=list_to_atom(Name++"@"++Host),
    case persistent_term:get({?MODULE,barrier},undefined) of
        {Node,Owner}->Owner!{select_entered452,self(),Node},
            receive {release452,Owner,Mode}->case Mode of late_success->erl_epmd:address_please(Name,Host,Family);late_error->{error,nxdomain} end
            after 10000->error(discovery_barrier_timeout) end;
        _->erl_epmd:address_please(Name,Host,Family)
    end.
port_please(Name,Host)->erl_epmd:port_please(Name,Host).
