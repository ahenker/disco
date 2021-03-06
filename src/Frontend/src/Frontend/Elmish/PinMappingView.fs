module Disco.Web.PinMappingView

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
open Helpers
open State
open Types

let touchesElement(el: Browser.Element option, x: float, y: float): bool = importMember "../../js/Util"

let inline padding5() =
  Style [PaddingLeft "5px"]

let inline topBorder() =
  Style [BorderTop "1px solid lightgray"]

let inline padding5AndTopBorder() =
  Style [PaddingLeft "5px"; BorderTop "1px solid lightgray"]

let renderPin model dispatch (pin: Pin) =
  com<PinView.PinView,_,_>
    { key = string pin.Id
      pin = pin
      output = false
      selected = false
      // Not needed as the pin is not editable
      slices = None
      model = model
      updater = None
      dispatch = dispatch
      onSelect = fun _ -> Select.pin dispatch pin
      onDragStart = None } []

type [<Pojo>] PinHoleProps =
  { Model: Model
    Classes: string list
    Padding: bool
    AddPins: Pin list -> unit
    Render: unit -> ReactElement list }

type [<Pojo>] PinHoleState =
  { IsHighlit: bool }

type PinHole(props) =
  inherit Component<PinHoleProps, PinHoleState>(props)
  let mutable selfRef: Browser.Element option = None
  let mutable disposable: IDisposable option = None
  do base.setInitState({ IsHighlit = false })

  member this.componentWillUnmount() =
    disposable |> Option.iter (fun disp -> disp.Dispose())

  member this.componentDidMount() =
    disposable <-
      Drag.observe()
      |> Observable.choose(function
        | Drag.Moved(x,y,data) -> Some(data,x,y,false)
        | Drag.Stopped(x,y,data) -> Some(data,x,y,true))
      |> Observable.subscribe(fun (data,x,y,stopped) ->
        let isHighlit  =
          if touchesElement(selfRef, x, y) then
            if not stopped then
              true
            else
              match this.props.Model.state, data with
              | Some state, DragItems.Pins pinIds ->
                List.map (fun id -> Lib.findPin id state) pinIds
                |> this.props.AddPins
              | _ -> ()
              false
          else
            false
        if isHighlit <> this.state.IsHighlit then
          this.setState({ IsHighlit = isHighlit })
      ) |> Some

  member this.render() =
    let isHighlit = this.state.IsHighlit
    let classes =
      [ for c in this.props.Classes do
          yield c, true
        yield "disco-highlight", isHighlit]
    td [classList classes
        Style [PaddingLeft (if this.props.Padding then "5px" else "0")
               Border "2px solid transparent"]
        Ref (fun el -> selfRef <- Option.ofObj el)] [
          div [Class "disco-pin-hole"] (this.props.Render())
        ]

type [<Pojo>] PinMappingProps =
  { Id: Guid
    Name: string
    Model: Model
    Dispatch: Msg -> unit }

type [<Pojo>] PinMappingState =
  { SourceCandidate: Pin option
    SinkCandidates: Set<Pin> }

type PinMappingView(props) =
  inherit Component<PinMappingProps, PinMappingState>(props)
  do base.setInitState({ SourceCandidate = None; SinkCandidates = Set.empty })

  member this.shouldComponentUpdate(nextProps, nextState, nextContext) =
    if distinctRef this.state nextState then
      true
    else
      match this.props.Model.state, nextProps.Model.state with
      | Some s1, Some s2 ->
        distinctRef s1 s2
      | None, None -> false
      | _ -> true

  member this.renderLastRow() =
    let disabled =
      Option.isNone this.state.SourceCandidate
        || Set.isEmpty this.state.SinkCandidates
    tr [Class "disco-pinmapping-add"] [
      com<PinHole,_,_>
        { Classes = ["width-20"]
          Padding = true
          Model = props.Model
          AddPins = function
            | pin::_ -> this.setState({ this.state with SourceCandidate = Some pin })
            | _ -> ()
          Render = fun () ->
            [ this.state.SourceCandidate
              |> Option.map (renderPin this.props.Model this.props.Dispatch)
              |> opt ]
        } []
      com<PinHole,_,_>
        { Model = this.props.Model
          Classes = ["width-75"]
          Padding = true
          AddPins = fun pins ->
            let sinks = (this.state.SinkCandidates, pins)
                        ||> List.fold (fun sinks pin -> Set.add pin sinks)
            this.setState({ this.state with SinkCandidates = sinks })
          Render = fun () ->
            this.state.SinkCandidates
            |> Seq.map (renderPin this.props.Model this.props.Dispatch)
            |> Seq.toList
         } []
      td [Class "width-5"] [
        button [
          Class "disco-button disco-icon icon-more"
          Disabled disabled
          OnClick (fun ev ->
            ev.stopPropagation()
            let { SourceCandidate = source; SinkCandidates = sinks } = this.state
            match source with
            | Some source when not(Set.isEmpty sinks) ->
                this.setState({SourceCandidate = None; SinkCandidates = Set.empty})
                { Id = DiscoId.Create()
                  Source = source.Id
                  Sinks = sinks |> Set.map (fun s -> s.Id) }
                |> AddPinMapping |> ClientContext.Singleton.Post
            // In this case the button shouldn't be enabled, but just in case
            // don't do anything if source or sinks are empty
            | _ -> ())
        ] []
      ]
    ]

  member this.renderBody() =
    let model = this.props.Model
    table [Class "disco-pinmapping disco-table"] [
      thead [] [
        tr [] [
          th [Class "width-20"; padding5()] [str "Source"]
          th [Class "width-75"] [str "Sinks"]
          th [Class "width-5"] []
        ]
      ]
      tbody [] [
        match model.state with
        | Some state ->
          for kv in state.PinMappings do
            let pinMapping = kv.Value
            let source =
              Lib.findPin pinMapping.Source state
              |> renderPin this.props.Model this.props.Dispatch
            let sinks =
              pinMapping.Sinks
              |> Seq.map (fun id ->
                Lib.findPin id state
                |> renderPin this.props.Model this.props.Dispatch)
              |> Seq.toList
            yield tr [Key (string kv.Key); Class "disco-pinmapping-row"] [
              td [Class "width-20"; padding5AndTopBorder()] [
                div [Class "disco-pinmapping-source"] [source]
              ]
              td [Class "width-75"; topBorder()] [
                div [Class "disco-pinmapping-sinks"] sinks
              ]
              td [Class "width-5"; topBorder()] [
                button [
                  Class "disco-button disco-icon icon-close"
                  OnClick (fun ev ->
                    ev.stopPropagation()
                    RemovePinMapping pinMapping |> ClientContext.Singleton.Post)
                ] []
              ]
            ]
          yield this.renderLastRow()
        | None -> ()
        ]
      ]

  member this.render() =
    widget this.props.Id this.props.Name
      None
      (fun _ _ -> this.renderBody()) this.props.Dispatch this.props.Model

let createWidget(id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.PinMapping
    member __.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 8; h = 5
        minW = 4
        minH = 1 }
    member this.Render(dispatch, model) =
      com<PinMappingView,_,_>
        { Id = this.Id
          Name = this.Name
          Model = model
          Dispatch = dispatch } []
  }
