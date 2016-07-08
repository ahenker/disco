// automatically generated by the FlatBuffers compiler, do not modify

namespace Iris.Serialization.Raft
{

using System;
using FlatBuffers;

public sealed class RequestVoteFB : Table {
  public static RequestVoteFB GetRootAsRequestVoteFB(ByteBuffer _bb) { return GetRootAsRequestVoteFB(_bb, new RequestVoteFB()); }
  public static RequestVoteFB GetRootAsRequestVoteFB(ByteBuffer _bb, RequestVoteFB obj) { return (obj.__init(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public RequestVoteFB __init(int _i, ByteBuffer _bb) { bb_pos = _i; bb = _bb; return this; }

  public string NodeId { get { int o = __offset(4); return o != 0 ? __string(o + bb_pos) : null; } }
  public ArraySegment<byte>? GetNodeIdBytes() { return __vector_as_arraysegment(4); }
  public VoteRequestFB Request { get { return GetRequest(new VoteRequestFB()); } }
  public VoteRequestFB GetRequest(VoteRequestFB obj) { int o = __offset(6); return o != 0 ? obj.__init(__indirect(o + bb_pos), bb) : null; }

  public static Offset<RequestVoteFB> CreateRequestVoteFB(FlatBufferBuilder builder,
      StringOffset NodeIdOffset = default(StringOffset),
      Offset<VoteRequestFB> RequestOffset = default(Offset<VoteRequestFB>)) {
    builder.StartObject(2);
    RequestVoteFB.AddRequest(builder, RequestOffset);
    RequestVoteFB.AddNodeId(builder, NodeIdOffset);
    return RequestVoteFB.EndRequestVoteFB(builder);
  }

  public static void StartRequestVoteFB(FlatBufferBuilder builder) { builder.StartObject(2); }
  public static void AddNodeId(FlatBufferBuilder builder, StringOffset NodeIdOffset) { builder.AddOffset(0, NodeIdOffset.Value, 0); }
  public static void AddRequest(FlatBufferBuilder builder, Offset<VoteRequestFB> RequestOffset) { builder.AddOffset(1, RequestOffset.Value, 0); }
  public static Offset<RequestVoteFB> EndRequestVoteFB(FlatBufferBuilder builder) {
    int o = builder.EndObject();
    return new Offset<RequestVoteFB>(o);
  }
};


}