// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: ClientVoiceChannelLeave.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Blizzard.Telemetry.Wow.Client {

  /// <summary>Holder for reflection information generated from ClientVoiceChannelLeave.proto</summary>
  public static partial class ClientVoiceChannelLeaveReflection {

    #region Descriptor
    /// <summary>File descriptor for ClientVoiceChannelLeave.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static ClientVoiceChannelLeaveReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "Ch1DbGllbnRWb2ljZUNoYW5uZWxMZWF2ZS5wcm90bxIdQmxpenphcmQuVGVs",
            "ZW1ldHJ5Lldvdy5DbGllbnQaGnRlbGVtZXRyeV9leHRlbnNpb25zLnByb3Rv",
            "GiJUZWxlbWV0cnlTaGFyZWRDbGllbnRJbXBvcnRzLnByb3RvGhl2b2ljZV9j",
            "aGFubmVsX2xlYXZlLnByb3RvItsBChdDbGllbnRWb2ljZUNoYW5uZWxMZWF2",
            "ZRI5CgZjbGllbnQYASABKAsyKS5CbGl6emFyZC5UZWxlbWV0cnkuV293LkNs",
            "aWVudC5DbGllbnRJbmZvEjcKBXdvcmxkGAIgASgLMiguQmxpenphcmQuVGVs",
            "ZW1ldHJ5Lldvdy5DbGllbnQuV29ybGRJbmZvEkMKDWNoYW5uZWxfbGVhdmUY",
            "AyABKAsyLC5CbGl6emFyZC5UZWxlbWV0cnkuVm9pY2VDbGllbnQuQ2hhbm5l",
            "bExlYXZlOgfCzCUDoAYB"));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Blizzard.Telemetry.TelemetryExtensionsReflection.Descriptor, global::Blizzard.Telemetry.Wow.Client.TelemetrySharedClientImportsReflection.Descriptor, global::Blizzard.Telemetry.VoiceClient.VoiceChannelLeaveReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Blizzard.Telemetry.Wow.Client.ClientVoiceChannelLeave), global::Blizzard.Telemetry.Wow.Client.ClientVoiceChannelLeave.Parser, new[]{ "Client", "World", "ChannelLeave" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class ClientVoiceChannelLeave : pb::IMessage<ClientVoiceChannelLeave>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<ClientVoiceChannelLeave> _parser = new pb::MessageParser<ClientVoiceChannelLeave>(() => new ClientVoiceChannelLeave());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<ClientVoiceChannelLeave> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Blizzard.Telemetry.Wow.Client.ClientVoiceChannelLeaveReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ClientVoiceChannelLeave() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ClientVoiceChannelLeave(ClientVoiceChannelLeave other) : this() {
      client_ = other.client_ != null ? other.client_.Clone() : null;
      world_ = other.world_ != null ? other.world_.Clone() : null;
      channelLeave_ = other.channelLeave_ != null ? other.channelLeave_.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ClientVoiceChannelLeave Clone() {
      return new ClientVoiceChannelLeave(this);
    }

    /// <summary>Field number for the "client" field.</summary>
    public const int ClientFieldNumber = 1;
    private global::Blizzard.Telemetry.Wow.Client.ClientInfo client_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::Blizzard.Telemetry.Wow.Client.ClientInfo Client {
      get { return client_; }
      set {
        client_ = value;
      }
    }

    /// <summary>Field number for the "world" field.</summary>
    public const int WorldFieldNumber = 2;
    private global::Blizzard.Telemetry.Wow.Client.WorldInfo world_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::Blizzard.Telemetry.Wow.Client.WorldInfo World {
      get { return world_; }
      set {
        world_ = value;
      }
    }

    /// <summary>Field number for the "channel_leave" field.</summary>
    public const int ChannelLeaveFieldNumber = 3;
    private global::Blizzard.Telemetry.VoiceClient.ChannelLeave channelLeave_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::Blizzard.Telemetry.VoiceClient.ChannelLeave ChannelLeave {
      get { return channelLeave_; }
      set {
        channelLeave_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as ClientVoiceChannelLeave);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(ClientVoiceChannelLeave other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(Client, other.Client)) return false;
      if (!object.Equals(World, other.World)) return false;
      if (!object.Equals(ChannelLeave, other.ChannelLeave)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (client_ != null) hash ^= Client.GetHashCode();
      if (world_ != null) hash ^= World.GetHashCode();
      if (channelLeave_ != null) hash ^= ChannelLeave.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (client_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(Client);
      }
      if (world_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(World);
      }
      if (channelLeave_ != null) {
        output.WriteRawTag(26);
        output.WriteMessage(ChannelLeave);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (client_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(Client);
      }
      if (world_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(World);
      }
      if (channelLeave_ != null) {
        output.WriteRawTag(26);
        output.WriteMessage(ChannelLeave);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      if (client_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Client);
      }
      if (world_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(World);
      }
      if (channelLeave_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(ChannelLeave);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(ClientVoiceChannelLeave other) {
      if (other == null) {
        return;
      }
      if (other.client_ != null) {
        if (client_ == null) {
          Client = new global::Blizzard.Telemetry.Wow.Client.ClientInfo();
        }
        Client.MergeFrom(other.Client);
      }
      if (other.world_ != null) {
        if (world_ == null) {
          World = new global::Blizzard.Telemetry.Wow.Client.WorldInfo();
        }
        World.MergeFrom(other.World);
      }
      if (other.channelLeave_ != null) {
        if (channelLeave_ == null) {
          ChannelLeave = new global::Blizzard.Telemetry.VoiceClient.ChannelLeave();
        }
        ChannelLeave.MergeFrom(other.ChannelLeave);
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            if (client_ == null) {
              Client = new global::Blizzard.Telemetry.Wow.Client.ClientInfo();
            }
            input.ReadMessage(Client);
            break;
          }
          case 18: {
            if (world_ == null) {
              World = new global::Blizzard.Telemetry.Wow.Client.WorldInfo();
            }
            input.ReadMessage(World);
            break;
          }
          case 26: {
            if (channelLeave_ == null) {
              ChannelLeave = new global::Blizzard.Telemetry.VoiceClient.ChannelLeave();
            }
            input.ReadMessage(ChannelLeave);
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 10: {
            if (client_ == null) {
              Client = new global::Blizzard.Telemetry.Wow.Client.ClientInfo();
            }
            input.ReadMessage(Client);
            break;
          }
          case 18: {
            if (world_ == null) {
              World = new global::Blizzard.Telemetry.Wow.Client.WorldInfo();
            }
            input.ReadMessage(World);
            break;
          }
          case 26: {
            if (channelLeave_ == null) {
              ChannelLeave = new global::Blizzard.Telemetry.VoiceClient.ChannelLeave();
            }
            input.ReadMessage(ChannelLeave);
            break;
          }
        }
      }
    }
    #endif

  }

  #endregion

}

#endregion Designer generated code
