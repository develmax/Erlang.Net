%% This is an -*- erlang -*- file.
%%
%% %CopyrightBegin%
%%
%% SPDX-License-Identifier: Apache-2.0
%%
%% Copyright Ericsson AB 2010-2025. All Rights Reserved.
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

{application, wx,
 [{description, "Yet another graphics system"},
  {vsn, "2.7"},
  {modules,
   [
    %% Generated modules
  wxFileDataObject, wxAuiNotebookEvent, wxGLCanvas, wxWebViewEvent, wxSashEvent, wxUpdateUIEvent, wxIcon, wxBitmapButton, wxImage, wxGraphicsContext, wxComboBox, wxFontPickerCtrl, wxEvtHandler, wxFileDialog, wxFlexGridSizer, wxPrintDialogData, wxColourData, wxDisplay, wxPreviewFrame, wxMoveEvent, wxChoicebook, wxSystemOptions, wxNotificationMessage, wxGridCellFloatRenderer, wxColourDialog, wxStatusBar, wxInitDialogEvent, wxXmlResource, wxToggleButton, wxTaskBarIconEvent, wxGraphicsObject, wxPrintout, wxDateEvent, wxSysColourChangedEvent, wxGridCellRenderer, wxLocale, wxGraphicsMatrix, wxBitmap, wxQueryNewPaletteEvent, wxSizerItem, wxGridCellBoolRenderer, wxPasswordEntryDialog, wxFrame, wxNavigationKeyEvent, wxFontData, wxGraphicsRenderer, wxMouseCaptureLostEvent, wxTextEntryDialog, wxIdleEvent, wxChoice, wxListItem, wxSpinCtrl, wxMDIClientWindow, wxMDIChildFrame, wxStdDialogButtonSizer, wxPrintPreview, wxPrintData, wxDirPickerCtrl, wxKeyEvent, wxEraseEvent, wxRadioBox, wxGridCellEditor, wxGridCellNumberRenderer, wxLogNull, wxDCOverlay, wxPreviewCanvas, wxTextAttr, wxScrollWinEvent, wxCalendarCtrl, wxJoystickEvent, wxAuiDockArt, wxWindowDestroyEvent, wxSetCursorEvent, wxMirrorDC, wxControl, wxActivateEvent, wxGraphicsFont, wxStaticText, wxStaticBitmap, wxGridBagSizer, wxListbook, wxGridSizer, wxScrollEvent, wxGLContext, wxWindowCreateEvent, wxGridCellFloatEditor, wxStyledTextEvent, wxPrintDialog, wxStaticBox, wxBufferedDC, wxTextCtrl, wxDropFilesEvent, wxControlWithItems, wxStaticBoxSizer, wxListCtrl, wxGridCellAttr, wxCalendarEvent, wxGauge, wxGridCellTextEditor, wxDataObject, wxShowEvent, wxBitmapDataObject, wxFindReplaceDialog, wxTextDataObject, wxMiniFrame, wxDisplayChangedEvent, wxListEvent, wxCursor, wxDialog, wxBrush, wxTopLevelWindow, wxPaintDC, wxScreenDC, wxPopupWindow, wxColourPickerCtrl, wxFilePickerCtrl, wxPostScriptDC, wxGrid, wxAuiSimpleTabArt, wxScrolledWindow, wxSizerFlags, wxMask, wxSplitterEvent, wxScrollBar, wxBookCtrlEvent, wxMenu, wxCheckBox, wxPaletteChangedEvent, wxIconBundle, wxListItemAttr, wxGraphicsGradientStops, wxAuiManager, wxFileDirPickerEvent, wxBoxSizer, wxMouseCaptureChangedEvent, wxClipboard, wxMouseEvent, wxSashWindow, wxAuiPaneInfo, wxPaintEvent, wxSplitterWindow, wxProgressDialog, wxListBox, wxColourPickerEvent, wxMenuItem, wxChildFocusEvent, wxMessageDialog, wxButton, wxMenuBar, wxMaximizeEvent, wxToolBar, wxGraphicsPen, wxNotifyEvent, wxArtProvider, wxHtmlEasyPrinting, wxBufferedPaintDC, wxTreeCtrl, wxFindReplaceData, wxClipboardTextEvent, wxListView, wxFontDialog, wxHtmlLinkEvent, wxContextMenuEvent, wxLayoutAlgorithm, wxCheckListBox, wxGridCellBoolEditor, wxMultiChoiceDialog, wxOverlay, wxWebView, wxWindowDC, wxSizeEvent, wxHtmlWindow, wxPageSetupDialog, wxCommandEvent, wxCloseEvent, wxBookCtrlBase, wxGraphicsPath, wxPreviewControlBar, wxFocusEvent, wxGridCellChoiceEditor, wxImageList, wxAuiNotebook, wxToolTip, wxPalette, wxSlider, wxPanel, wxSizer, wxAuiManagerEvent, wxGBSizerItem, wxPen, wxTaskBarIcon, wxPickerBase, wxPageSetupDialogData, wxSplashScreen, wxMemoryDC, wxGridCellStringRenderer, wxPopupTransientWindow, wxGCDC, wxAcceleratorEntry, wxRadioButton, wxNotebook, wxCalendarDateAttr, wxDC, wxCaret, wxIconizeEvent, wxAcceleratorTable, wxMenuEvent, wxGraphicsBrush, wxDirDialog, wxMDIParentFrame, wxHelpEvent, wxStaticLine, wxGenericDirCtrl, wxToolbook, wxFont, wxGridCellNumberEditor, wxEvent, wxDatePickerCtrl, wxSystemSettings, wxWindow, wxTreeEvent, wxSpinEvent, wxSingleChoiceDialog, wxFontPickerEvent, wxPrinter, wxAuiTabArt, wxRegion, wxClientDC, wxSpinButton, wxTreebook, wxSashLayoutWindow, wxStyledTextCtrl, wxGridEvent, wx_misc, glu, gl,
    %% Handcrafted modules
    wx,
    wx_object,
    wxe_master,
    wxe_server,
    wxe_util
   ]},
  {registered, []},
  {applications, [stdlib, kernel]},
  {env, []},
  {runtime_dependencies, ["stdlib-5.0","kernel-8.0","erts-12.0"]}
 ]}.
