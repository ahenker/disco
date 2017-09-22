module Iris.Web.InspectorView

open System
open System.Collections.Generic
open Fable.Import
open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Elmish.React
open Iris.Core
open Iris.Web.Core
open Iris.Web.Inspectors
open Helpers
open State
open Types

///  ____       _            _
/// |  _ \ _ __(_)_   ____ _| |_ ___
/// | |_) | '__| \ \ / / _` | __/ _ \
/// |  __/| |  | |\ V / (_| | ||  __/
/// |_|   |_|  |_| \_/ \__,_|\__\___|

let empty dispatch model =
  div [] [
    Common.bar dispatch model
    div [
      Style [
        PaddingTop "10px"
        TextAlign "center"
      ]
    ] [
      str "Nothing selected."
    ]
  ]

let private body dispatch (model: Model) =
  match model.history.selected with
  | Pin      (client,pin)   -> PinInspector.render        dispatch model client pin
  | PinGroup (client,group) -> PinGroupInspector.render   dispatch model client group
  | Client   client         -> ClientInspector.render     dispatch model client
  | Member   mem            -> MemberInspector.render     dispatch model mem
  | Cue      cue            -> CueInspector.render        dispatch model cue
  | CueList  cuelist        -> CueListInspector.render    dispatch model cuelist
  | Player   player         -> CuePlayerInspector.render  dispatch model player
  | Mapping  mapping        -> PinMappingInspector.render dispatch model mapping
  | Session  session        -> SessionInspector.render    dispatch model session
  | User     user           -> UserInspector.render       dispatch model user
  | Nothing                 -> empty dispatch model

///  ____        _     _ _
/// |  _ \ _   _| |__ | (_) ___
/// | |_) | | | | '_ \| | |/ __|
/// |  __/| |_| | |_) | | | (__
/// |_|    \__,_|_.__/|_|_|\___|

let createWidget(id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.InspectorView
    member __.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 4; h = 12
        minW = 4; maxW = 10
        minH = 1; maxH = 20 }
    member this.Render(dispatch, model) =
      widget id this.Name None body dispatch model }
