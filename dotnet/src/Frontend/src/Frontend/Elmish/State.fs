module rec Iris.Web.State

open System
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Fable.Import
open Elmish
open Types
open Helpers

[<PassGenerics>]
let loadFromLocalStorage<'T> (key: string) =
  let g = Fable.Import.Browser.window
  match g.localStorage.getItem(key) with
  | null -> None
  | value -> ofJson<'T> !!value |> Some

let saveToLocalStorage (key: string) (value: obj) =
  let g = Fable.Import.Browser.window
  g.localStorage.setItem(key, toJson value)

let init() =
  let startContext dispatch =
    let context = ClientContext.Singleton
    context.Start()
    |> Promise.iter (fun () ->
      context.OnMessage
      |> Observable.add (fun _ ->
        let state = context.Store |> Option.map (fun s -> s.State)
        UpdateState state |> dispatch))
  let widgets =
    let factory = Types.getFactory()
    loadFromLocalStorage<WidgetRef[]> StorageKeys.widgets
    |> Option.defaultValue [||]
    |> Array.map (fun (id, name) ->
      let widget = factory.CreateWidget(Some id, name)
      id, widget)
    |> Map
  let layout =
    loadFromLocalStorage<Layout[]> StorageKeys.layout
    |> Option.defaultValue [||]
  let logs = List.init 50 (fun _ -> Core.MockData.genLog())
  let initModel =
    { widgets = widgets
      layout = layout
      state = None
      modal = None
      logs = logs
      userConfig = UserConfig.Create() }
  initModel, [startContext]

let saveWidgetsAndLayout (widgets: Map<Guid,IWidget>) (layout: Layout[]) =
    widgets
    |> Seq.map (fun kv -> kv.Key, kv.Value.Name)
    |> Seq.toArray |> saveToLocalStorage StorageKeys.widgets
    layout |> saveToLocalStorage StorageKeys.layout

let addCue (cueList:CueList) (cueGroupIndex:int) (cueIndex:int) =
  // TODO: Select the cue list from the widget
  if cueList.Groups.Length = 0 then
    failwith "A Cue Group must be added first"
  // Create new Cue and CueReference
  let newCue = { Id = Id.Create(); Name = name "Untitled"; Slices = [||] }
  let newCueRef = { Id = Id.Create(); CueId = newCue.Id; AutoFollow = -1; Duration = -1; Prewait = -1 }
  // Insert new CueRef in the selected CueGroup after the selected cue
  let cueGroup = cueList.Groups.[max cueGroupIndex 0]
  let idx = if cueIndex < 0 then cueGroup.CueRefs.Length - 1 else cueIndex
  let newCueGroup = { cueGroup with CueRefs = Array.insertAfter idx newCueRef cueGroup.CueRefs }
  // Update the CueList
  let newCueList = { cueList with Groups = Array.replaceById newCueGroup cueList.Groups }
  // Send messages to backend
  AddCue newCue |> ClientContext.Singleton.Post
  UpdateCueList newCueList |> ClientContext.Singleton.Post

let update msg model =
  let newModel =
    match msg with
    | AddWidget(id, widget) ->
      let widgets = Map.add id widget model.widgets
      let layout = Array.append model.layout [|widget.InitialLayout|]
      saveWidgetsAndLayout widgets layout
      { model with widgets = widgets; layout = layout }
    | RemoveWidget id ->
      let widgets = Map.remove id model.widgets
      let layout = model.layout |> Array.filter (fun x -> x.i <> id)
      saveWidgetsAndLayout widgets layout
      { model with widgets = widgets; layout = layout }
    // | AddTab -> // Add tab and remove widget
    // | RemoveTab -> // Optional, add widget
    | AddLog log ->
      { model with logs = log::model.logs }
    | AddCueUI(cueList, cueGroupIndex, cueIndex) ->
      addCue cueList cueGroupIndex cueIndex
      model
    | UpdateLayout layout ->
      saveToLocalStorage StorageKeys.layout layout
      { model with layout = layout }
    | UpdateUserConfig cfg ->
      { model with userConfig = cfg }
    | UpdateState state ->
      { model with state = state }
    | UpdateModal modal ->
      { model with modal = modal }
  newModel, []