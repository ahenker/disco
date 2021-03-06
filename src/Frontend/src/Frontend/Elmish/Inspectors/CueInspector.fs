namespace Disco.Web.Inspectors

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
open Disco.Core
open Disco.Web.Core
open Disco.Web.Helpers
open Disco.Web.Types
open Disco.Web.PinView
open State

module CueInspector =

  let private renderCueList dispatch (model:Model) (cuelist: CueList)  =
    li [] [
      Common.link
        (string cuelist.Name)
        (fun () -> Select.cuelist dispatch cuelist)
    ]

  let private renderCueLists tag dispatch (model: Model) (cue: Cue) =
    match model.state with
    | None -> Common.row tag []
    | Some state ->
      state.CueLists
      |> CueList.filter (CueList.contains cue.Id)
      |> Map.toList
      |> List.map (snd >> renderCueList dispatch model)
      |> fun cuelists -> Common.row tag [ ul [] cuelists ]

  let private buildPin dispatch (model: Model) (pin: Pin) =
    li [] [
      com<PinView,_,_> {
        key = string pin.Id
        pin = pin
        output = false
        selected = false
        slices = None
        model = model
        updater = None
        onSelect = fun _ -> Select.pin dispatch pin
        onDragStart = None
        dispatch = dispatch
      } []
    ]

  let private renderSlices tag dispatch (model: Model) (cue: Cue) =
    match model.state with
    | None -> Common.row tag []
    | Some state ->
      cue.Slices
      |> List.ofArray
      |> List.choose
        (fun (slices: Slices) ->
          state.PinGroups
          |> PinGroupMap.findPin slices.PinId
          |> Map.tryPick
            (fun clientId pin ->
              match slices.ClientId with
              | Some client when client = clientId -> Some pin
              | Some _ -> None
              | None -> Some pin)
          |> function
          | Some pin ->
            pin
            |> Pin.setSlices slices
            |> buildPin dispatch model
            |> Some
          | None -> None)
      |> fun items ->
        Common.row tag [
          ul [ Class "disco-graphview" ] items
        ]

  let render dispatch (model: Model) (cue: CueId) =
    match model.state with
    | None ->
      Common.render dispatch model "Cue" [
        str (string cue + " (orphaned)")
      ]
    | Some state ->
      match Map.tryFind cue state.Cues with
      | None ->
        Common.render dispatch model "Cue" [
          str (string cue + " (orphaned)")
        ]
      | Some cue ->
        Common.render dispatch model "Cue" [
          Common.stringRow "Id"       (string cue.Id)
          Common.stringRow "Name"     (string cue.Name)
          renderSlices     "Values"    dispatch model cue
          renderCueLists   "Cue Lists" dispatch model cue
        ]
