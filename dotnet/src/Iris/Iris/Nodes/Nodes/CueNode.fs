namespace VVVV.Nodes

open System
open System.ComponentModel.Composition
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.Utils.VColor
open VVVV.Utils.VMath
open VVVV.Core.Logging
open Iris.Raft
open Iris.Core
open Iris.Nodes

//   ____
//  / ___|   _  ___
// | |  | | | |/ _ \
// | |__| |_| |  __/
//  \____\__,_|\___|

[<PluginInfo(Name="Cue", Category="Iris", AutoEvaluate=true)>]
type CueNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Cue")>]
  val mutable InCue: ISpread<Cue>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Id")>]
  val mutable OutId: ISpread<string>

  [<DefaultValue>]
  [<Output("Name")>]
  val mutable OutName: ISpread<string>

  [<DefaultValue>]
  [<Output("Pins")>]
  val mutable OutPins: ISpread<ISpread<Pin>>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then

        self.OutId.SliceCount <- self.InCue.SliceCount
        self.OutName.SliceCount <- self.InCue.SliceCount
        self.OutPins.SliceCount <- self.InCue.SliceCount

        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNullReference self.InCue.[n]) then
            let cue = self.InCue.[n]
            self.OutId.[n] <- string cue.Id
            self.OutName.[n] <- cue.Name
            self.OutPins.[n].SliceCount <- Array.length cue.Pins
            self.OutPins.[n].AssignFrom cue.Pins

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]