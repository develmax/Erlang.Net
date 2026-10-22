%%
%% %CopyrightBegin%
%%
%% SPDX-License-Identifier: Apache-2.0
%%
%% Copyright Ericsson AB 2012-2025. All Rights Reserved.
%%
%% Licensed under the Apache License, Version 2.0 (the "License");
%% you may not use this file except in compliance with the License.
%% You may obtain a copy of the License at
%%
%%     http://www.apache.org/licenses/LICENSE-2.0
%%
%% Unless required by applicable law or agreed to in writing, software
%% distributed under the License is distributed on an "AS IS" BASIS,
%% WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
%% See the License for the specific language governing permissions and
%% limitations under the License.
%%
%% %CopyrightEnd%
%%

%%
%% As the module name imply, this module is here for ERTS internal
%% functionality. As an application programmer you should *never*
%% call anything in this module directly. Functions exported by
%% this module may change behaviour or be removed at any time
%% without any notice whatsoever. Everything in this module is
%% intentionally left undocumented, and should remain so.
%%

-module(erts_internal).
-export([open_port/2, port_command/3, port_connect/2, port_close/1, port_info/1, port_info/2]).

open_port(_PortName, _PortSettings) ->
    erlang:nif_error(undefined).

port_command(_Port, _Data, _OptionList) ->
    erlang:nif_error(undefined).

port_connect(_Port, _Pid) ->
    erlang:nif_error(undefined).

port_close(_Port) ->
    erlang:nif_error(undefined).

port_info(_Result) ->
    erlang:nif_error(undefined).

port_info(_Result, _Item) ->
    erlang:nif_error(undefined).
