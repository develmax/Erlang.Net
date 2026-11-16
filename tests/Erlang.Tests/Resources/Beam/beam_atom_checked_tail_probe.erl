%%
%% %CopyrightBegin%
%%
%% SPDX-License-Identifier: Apache-2.0
%%
%% Copyright Ericsson AB 2013-2025. All Rights Reserved.
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
-module(beam_atom_checked_tail_probe).
-export([display_progress/2, destroy_progress/1]).

display_progress(ParentFrame,cdv) ->
    observer_lib:display_progress_dialog(ParentFrame,
                                         "Crashdump Viewer",
                                         "Reading data");
display_progress(_,_) ->
    ok.

destroy_progress(cdv) ->
    observer_lib:sync_destroy_progress_dialog();
destroy_progress(_) ->
    ok.
