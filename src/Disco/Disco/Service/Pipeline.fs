(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Service

// * Imports

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Collections.Generic
open Disruptor
open Disruptor.Dsl
open Disco.Core
open Disco.Raft
open Disco.Net
open SharpYaml.Serialization

// * PipelineOptions

[<NoComparison;NoEquality>]
type PipelineOptions =
  { PreActions:  IHandler<DiscoEvent>[]
    Processors:  IHandler<DiscoEvent>[]
    Publishers:  IHandler<DiscoEvent>[]
    PostActions: IHandler<DiscoEvent>[] }

// * Pipeline

module Pipeline =

  // ** bufferSize

  [<Literal>]
  let private BufferSize = 2048

  // ** scheduler

  let private scheduler = TaskScheduler.Default

  // ** tag

  let private tag (str: string) = String.format "Pipeline.{0}" str

  // ** push

  let push (pipeline: IPipeline<_>) = pipeline.Push

  // ** createDisruptor

  let private createDisruptor () =
    Dsl.Disruptor<PipelineEvent<DiscoEvent>>(PipelineEvent<DiscoEvent>, BufferSize, scheduler)

  // ** handleEventsWith

  let private handleEventsWith (handlers: IHandler<DiscoEvent> [])
                               (disruptor: Disruptor<PipelineEvent<DiscoEvent>>) =
    disruptor.HandleEventsWith handlers

  // ** thenDo

  let private thenDo (handlers: IHandler<DiscoEvent>[]) (group: IHandlerGroup<DiscoEvent>) =
    group.Then handlers

  // ** insertInto

  let private insertInto (ringBuffer: RingBuffer<PipelineEvent<DiscoEvent>>) (cmd: DiscoEvent) =
    let seqno = ringBuffer.Next()
    let entry = ringBuffer.[seqno]
    entry.Event <- Some cmd
    ringBuffer.Publish(seqno)

  // ** clearEvent

  let private clearEvent =
    [|  { new IHandler<DiscoEvent> with
           member handler.OnEvent(ev: PipelineEvent<DiscoEvent>, _, _) =
             ev.Clear() } |]

  // ** createHandler

  let createHandler (f: EventProcessor<DiscoEvent>) : IHandler<DiscoEvent> =
    { new IHandler<DiscoEvent> with
        member handler.OnEvent(ev: PipelineEvent<DiscoEvent>, seqno, eob) =
          Option.iter (f seqno eob) ev.Event }

  // ** create

  let create (options: PipelineOptions) =
    let disruptor = createDisruptor()

    disruptor
    |> handleEventsWith options.PreActions
    |> thenDo options.Processors
    |> thenDo options.Publishers
    |> thenDo options.PostActions
    |> thenDo clearEvent
    |> ignore

    let ringBuffer = disruptor.Start()

    { new IPipeline<DiscoEvent> with
        member pipeline.Push(cmd: DiscoEvent) =
          insertInto ringBuffer cmd

        member pipeline.Dispose() =
          disruptor.Shutdown() }
