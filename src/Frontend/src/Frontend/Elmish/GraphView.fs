module Iris.Web.GraphView

open System
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core
open Elmish.React
open Iris.Core
open Helpers
open Types

// * PinGroupProps

type [<Pojo>] PinGroupProps =
  { key: string
    Group: PinGroup
    Model: Model
    Dispatch: Msg -> unit }

// * PinGroupState

type [<Pojo>] PinGroupState =
  { IsOpen: bool }

// * GraphViewProps

type [<Pojo>] GraphViewProps =
  { Id: Guid
    Name: string
    Model: Model
    Dispatch: Msg -> unit }

// * GraphViewState

type [<Pojo>] GraphViewState =
  { ContextMenuActive: bool }

let private defaultGraphState =
  { ContextMenuActive = false }

let onDragStart (model: Model) pin multiple =
  let newItems = DragItems.Pins [pin]
  if multiple then model.selectedDragItems.Append(newItems) else newItems
  |> Drag.start

let isSelected (model: Model) (pin: Pin) =
  match model.selectedDragItems with
  | DragItems.Pins pinIds ->
    Seq.exists ((=) pin.Id) pinIds
  | _ -> false

let makeInputPin dispatch model (pid: PinId) (pin: Pin) =
  com<PinView.PinView,_,_>
    { key = string pid
      pin = pin
      output = false
      selected = isSelected model pin
      slices = None
      model = model
      updater = Some { new IUpdater with
                        member __.Update(_, index, value) =
                          Lib.updatePinValue(pin, index, value) }
      onSelect = fun multi ->
        Select.pin dispatch pin
        Drag.selectPin dispatch multi pin.Id
      onDragStart = Some(onDragStart model pin.Id)
    } []

let makeOutputPin dispatch model (pid: PinId) (pin: Pin) =
  com<PinView.PinView,_,_>
    { key = string pid
      pin = pin
      output = true
      selected = isSelected model pin
      slices = None
      model = model
      updater = None
      onSelect = fun multi ->
        Select.pin dispatch pin
        Drag.selectPin dispatch multi pin.Id
      onDragStart = Some(onDragStart model pin.Id)
    } []

// * Components
// ** PinGroupView

type PinGroupView(props) =
  inherit React.Component<PinGroupProps, PinGroupState>(props)
  do base.setInitState({ IsOpen = false })

  // *** render

  member this.render() =
    let { Group = group; Dispatch = dispatch; Model = model } = this.props
    li [] [
      yield div [] [
        button [
          ClassName ("iris-button iris-icon icon-control " +
            (if this.state.IsOpen then "icon-less" else "icon-more"))
          OnClick (fun ev ->
            ev.stopPropagation()
            this.setState({ this.state with IsOpen = not this.state.IsOpen}))
        ] []
        span [
            OnClick (fun _ -> Select.group dispatch group)
            Style [ Cursor "pointer" ]
          ] [str (unwrap group.Name)]
      ]
      if this.state.IsOpen then
        yield div [] (group.Pins |> Seq.choose (fun (KeyValue(pid, pin)) ->
          if not(Pin.isSource pin)
          then makeInputPin dispatch model pid pin |> Some
          else None) |> Seq.toList)
        yield div [] (group.Pins |> Seq.choose (fun (KeyValue(pid, pin)) ->
          if Pin.isSource pin
          then makeOutputPin dispatch model pid pin |> Some
          else None) |> Seq.toList)
    ]

// ** GraphView

type GraphView(props) =
  inherit React.Component<GraphViewProps, GraphViewState>(props)
  do base.setInitState(defaultGraphState)

  // *** menuOptions

  member this.menuOptions () =
    match this.props.Model.state with
    | None -> []
    | Some state ->
      let resetDirty =
        if state |> State.pinGroupMap |> PinGroupMap.hasDirtyPins
        then Some("Reset Dirty", fun () -> Lib.resetDirty state)
        else None

      let addAllUnpersisted =
        if state |> State.pinGroupMap |> PinGroupMap.hasUnpersistedPins
        then Some("Persist All", fun () -> Lib.persistAll state)
        else None

      let persistSelected =
        match this.props.Model.selectedDragItems with
        | DragItems.Pins pinIds ->
          let pins =
            List.collect
              (flip State.findPin state
               >> Map.toList
               >> List.map snd
               >> List.filter (Pin.isPersisted >> not))
              pinIds
          if List.isEmpty pins
          then None
          else Some("Persist Selected", fun () -> Lib.persistPins pins state)
        | _ -> None

      let showPlayers =
        if state |> State.pinGroupMap |> PinGroupMap.hasUnpersistedPins
        then Some("Show Player Groups", fun () -> printfn "persist all")
        else None

      let showWidgets =
        if state |> State.pinGroupMap |> PinGroupMap.hasUnpersistedPins
        then Some("Show Widget Groups", fun () -> printfn "persist all")
        else None

      List.choose id [
        resetDirty
        addAllUnpersisted
        persistSelected
        showPlayers
        showWidgets
      ]

  // *** renderTitleBar

  member this.renderTitleBar() =
    let onOpen () = this.setState({ ContextMenuActive = not this.state.ContextMenuActive })
    div [] [
      /// TODO: replace this fake element so the titleBar isn't all jacked up
      button [ Class "iris-button"; Style [ Visibility "hidden" ] ] [
        i [ Class "fa fa-snowflake-o" ] []
      ]
      ContextMenu.create this.state.ContextMenuActive onOpen (this.menuOptions())
    ]

  // *** renderBody

  member this.renderBody() =
    let pinGroups =
      match this.props.Model.state with
      | Some state ->
        state.PinGroups
        |> PinGroupMap.unifiedPins
        |> PinGroupMap.byGroup
      | None -> Map.empty
    ul [Class "iris-graphview"] (
      pinGroups |> Seq.map (fun (KeyValue(gid, group)) ->
        com<PinGroupView,_,_>
          { key = string gid
            Group = group
            Dispatch = this.props.Dispatch
            Model = this.props.Model } [])
      |> Seq.toList)

  // *** render

  member this.render() =
    widget this.props.Id this.props.Name
      (Some (fun _ _ -> this.renderTitleBar()))
      (fun _ _ -> this.renderBody())
      this.props.Dispatch
      this.props.Model

  // *** shouldComponentUpdate

  member this.shouldComponentUpdate(nextProps: GraphViewProps, nextState: GraphViewState) =
    this.state <> nextState
      || distinctRef this.props.Model.selectedDragItems nextProps.Model.selectedDragItems
      || match this.props.Model.state, nextProps.Model.state with
         | Some s1, Some s2 -> distinctRef s1.PinGroups s2.PinGroups
         | None, None -> false
         | _ -> true

// * createWidget

let createWidget (id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.GraphView
    member __.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 8; h = 5
        minW = 2; maxW = 20
        minH = 2; maxH = 20 }
    member this.Render(dispatch, model) =
      com<GraphView,_,_>
        { Model = model
          Dispatch = dispatch
          Id = this.Id
          Name = this.Name } [] }
