// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: WorldSceneLoadFailed.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Blizzard.Telemetry.Wow.Client {

  /// <summary>Holder for reflection information generated from WorldSceneLoadFailed.proto</summary>
  public static partial class WorldSceneLoadFailedReflection {

    #region Descriptor
    /// <summary>File descriptor for WorldSceneLoadFailed.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static WorldSceneLoadFailedReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChpXb3JsZFNjZW5lTG9hZEZhaWxlZC5wcm90bxIdQmxpenphcmQuVGVsZW1l",
            "dHJ5Lldvdy5DbGllbnQaGnRlbGVtZXRyeV9leHRlbnNpb25zLnByb3RvGiJU",
            "ZWxlbWV0cnlTaGFyZWRDbGllbnRJbXBvcnRzLnByb3RvIqkBChRXb3JsZFNj",
            "ZW5lTG9hZEZhaWxlZBIUCgxmaWxlX2RhdGFfaWQYASABKAUSOQoGY2xpZW50",
            "GAIgASgLMikuQmxpenphcmQuVGVsZW1ldHJ5Lldvdy5DbGllbnQuQ2xpZW50",
            "SW5mbxI3CgV3b3JsZBgDIAEoCzIoLkJsaXp6YXJkLlRlbGVtZXRyeS5Xb3cu",
            "Q2xpZW50LldvcmxkSW5mbzoHwswlA6AGAQ=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Blizzard.Telemetry.TelemetryExtensionsReflection.Descriptor, global::Blizzard.Telemetry.Wow.Client.TelemetrySharedClientImportsReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Blizzard.Telemetry.Wow.Client.WorldSceneLoadFailed), global::Blizzard.Telemetry.Wow.Client.WorldSceneLoadFailed.Parser, new[]{ "FileDataId", "Client", "World" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class WorldSceneLoadFailed : pb::IMessage<WorldSceneLoadFailed>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<WorldSceneLoadFailed> _parser = new pb::MessageParser<WorldSceneLoadFailed>(() => new WorldSceneLoadFailed());
    private pb::UnknownFieldSet _unknownFields;
    private int _hasBits0;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<WorldSceneLoadFailed> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Blizzard.Telemetry.Wow.Client.WorldSceneLoadFailedReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public WorldSceneLoadFailed() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public WorldSceneLoadFailed(WorldSceneLoadFailed other) : this() {
      _hasBits0 = other._hasBits0;
      fileDataId_ = other.fileDataId_;
      client_ = other.client_ != null ? other.client_.Clone() : null;
      world_ = other.world_ != null ? other.world_.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public WorldSceneLoadFailed Clone() {
      return new WorldSceneLoadFailed(this);
    }

    /// <summary>Field number for the "file_data_id" field.</summary>
    public const int FileDataIdFieldNumber = 1;
    private readonly static int FileDataIdDefaultValue = 0;

    private int fileDataId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int FileDataId {
      get { if ((_hasBits0 & 1) != 0) { return fileDataId_; } else { return FileDataIdDefaultValue; } }
      set {
        _hasBits0 |= 1;
        fileDataId_ = value;
      }
    }
    /// <summary>Gets whether the "file_data_id" field is set</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool HasFileDataId {
      get { return (_hasBits0 & 1) != 0; }
    }
    /// <summary>Clears the value of the "file_data_id" field</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void ClearFileDataId() {
      _hasBits0 &= ~1;
    }

    /// <summary>Field number for the "client" field.</summary>
    public const int ClientFieldNumber = 2;
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
    public const int WorldFieldNumber = 3;
    private global::Blizzard.Telemetry.Wow.Client.WorldInfo world_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::Blizzard.Telemetry.Wow.Client.WorldInfo World {
      get { return world_; }
      set {
        world_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as WorldSceneLoadFailed);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(WorldSceneLoadFailed other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (FileDataId != other.FileDataId) return false;
      if (!object.Equals(Client, other.Client)) return false;
      if (!object.Equals(World, other.World)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (HasFileDataId) hash ^= FileDataId.GetHashCode();
      if (client_ != null) hash ^= Client.GetHashCode();
      if (world_ != null) hash ^= World.GetHashCode();
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
      if (HasFileDataId) {
        output.WriteRawTag(8);
        output.WriteInt32(FileDataId);
      }
      if (client_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(Client);
      }
      if (world_ != null) {
        output.WriteRawTag(26);
        output.WriteMessage(World);
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
      if (HasFileDataId) {
        output.WriteRawTag(8);
        output.WriteInt32(FileDataId);
      }
      if (client_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(Client);
      }
      if (world_ != null) {
        output.WriteRawTag(26);
        output.WriteMessage(World);
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
      if (HasFileDataId) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(FileDataId);
      }
      if (client_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Client);
      }
      if (world_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(World);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(WorldSceneLoadFailed other) {
      if (other == null) {
        return;
      }
      if (other.HasFileDataId) {
        FileDataId = other.FileDataId;
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
          case 8: {
            FileDataId = input.ReadInt32();
            break;
          }
          case 18: {
            if (client_ == null) {
              Client = new global::Blizzard.Telemetry.Wow.Client.ClientInfo();
            }
            input.ReadMessage(Client);
            break;
          }
          case 26: {
            if (world_ == null) {
              World = new global::Blizzard.Telemetry.Wow.Client.WorldInfo();
            }
            input.ReadMessage(World);
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
          case 8: {
            FileDataId = input.ReadInt32();
            break;
          }
          case 18: {
            if (client_ == null) {
              Client = new global::Blizzard.Telemetry.Wow.Client.ClientInfo();
            }
            input.ReadMessage(Client);
            break;
          }
          case 26: {
            if (world_ == null) {
              World = new global::Blizzard.Telemetry.Wow.Client.WorldInfo();
            }
            input.ReadMessage(World);
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