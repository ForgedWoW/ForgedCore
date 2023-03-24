// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: ClientNameLookup.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Blizzard.Telemetry.Wow.Client {

  /// <summary>Holder for reflection information generated from ClientNameLookup.proto</summary>
  public static partial class ClientNameLookupReflection {

    #region Descriptor
    /// <summary>File descriptor for ClientNameLookup.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static ClientNameLookupReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChZDbGllbnROYW1lTG9va3VwLnByb3RvEh1CbGl6emFyZC5UZWxlbWV0cnku",
            "V293LkNsaWVudBoadGVsZW1ldHJ5X2V4dGVuc2lvbnMucHJvdG8aIlRlbGVt",
            "ZXRyeVNoYXJlZENsaWVudEltcG9ydHMucHJvdG8iSQoOTmFtZUxvb2t1cElu",
            "Zm8SDgoGcmVhc29uGAEgASgFEhEKCW51bV9hZGRvbhgCIAEoDRIUCgxudW1f",
            "YmxpenphcmQYAyABKA0ipQEKEENsaWVudE5hbWVMb29rdXASQwoMbmFtZV9s",
            "b29rdXBzGAEgAygLMi0uQmxpenphcmQuVGVsZW1ldHJ5Lldvdy5DbGllbnQu",
            "TmFtZUxvb2t1cEluZm8SQwoRcGxheWVyX3dvcmxkX2luZm8YAiABKAsyKC5C",
            "bGl6emFyZC5UZWxlbWV0cnkuV293LkNsaWVudC5Xb3JsZEluZm86B8LMJQOg",
            "BgE="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Blizzard.Telemetry.TelemetryExtensionsReflection.Descriptor, global::Blizzard.Telemetry.Wow.Client.TelemetrySharedClientImportsReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Blizzard.Telemetry.Wow.Client.NameLookupInfo), global::Blizzard.Telemetry.Wow.Client.NameLookupInfo.Parser, new[]{ "Reason", "NumAddon", "NumBlizzard" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Blizzard.Telemetry.Wow.Client.ClientNameLookup), global::Blizzard.Telemetry.Wow.Client.ClientNameLookup.Parser, new[]{ "NameLookups", "PlayerWorldInfo" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class NameLookupInfo : pb::IMessage<NameLookupInfo>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<NameLookupInfo> _parser = new pb::MessageParser<NameLookupInfo>(() => new NameLookupInfo());
    private pb::UnknownFieldSet _unknownFields;
    private int _hasBits0;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<NameLookupInfo> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Blizzard.Telemetry.Wow.Client.ClientNameLookupReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public NameLookupInfo() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public NameLookupInfo(NameLookupInfo other) : this() {
      _hasBits0 = other._hasBits0;
      reason_ = other.reason_;
      numAddon_ = other.numAddon_;
      numBlizzard_ = other.numBlizzard_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public NameLookupInfo Clone() {
      return new NameLookupInfo(this);
    }

    /// <summary>Field number for the "reason" field.</summary>
    public const int ReasonFieldNumber = 1;
    private readonly static int ReasonDefaultValue = 0;

    private int reason_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int Reason {
      get { if ((_hasBits0 & 1) != 0) { return reason_; } else { return ReasonDefaultValue; } }
      set {
        _hasBits0 |= 1;
        reason_ = value;
      }
    }
    /// <summary>Gets whether the "reason" field is set</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool HasReason {
      get { return (_hasBits0 & 1) != 0; }
    }
    /// <summary>Clears the value of the "reason" field</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void ClearReason() {
      _hasBits0 &= ~1;
    }

    /// <summary>Field number for the "num_addon" field.</summary>
    public const int NumAddonFieldNumber = 2;
    private readonly static uint NumAddonDefaultValue = 0;

    private uint numAddon_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public uint NumAddon {
      get { if ((_hasBits0 & 2) != 0) { return numAddon_; } else { return NumAddonDefaultValue; } }
      set {
        _hasBits0 |= 2;
        numAddon_ = value;
      }
    }
    /// <summary>Gets whether the "num_addon" field is set</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool HasNumAddon {
      get { return (_hasBits0 & 2) != 0; }
    }
    /// <summary>Clears the value of the "num_addon" field</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void ClearNumAddon() {
      _hasBits0 &= ~2;
    }

    /// <summary>Field number for the "num_blizzard" field.</summary>
    public const int NumBlizzardFieldNumber = 3;
    private readonly static uint NumBlizzardDefaultValue = 0;

    private uint numBlizzard_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public uint NumBlizzard {
      get { if ((_hasBits0 & 4) != 0) { return numBlizzard_; } else { return NumBlizzardDefaultValue; } }
      set {
        _hasBits0 |= 4;
        numBlizzard_ = value;
      }
    }
    /// <summary>Gets whether the "num_blizzard" field is set</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool HasNumBlizzard {
      get { return (_hasBits0 & 4) != 0; }
    }
    /// <summary>Clears the value of the "num_blizzard" field</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void ClearNumBlizzard() {
      _hasBits0 &= ~4;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as NameLookupInfo);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(NameLookupInfo other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Reason != other.Reason) return false;
      if (NumAddon != other.NumAddon) return false;
      if (NumBlizzard != other.NumBlizzard) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (HasReason) hash ^= Reason.GetHashCode();
      if (HasNumAddon) hash ^= NumAddon.GetHashCode();
      if (HasNumBlizzard) hash ^= NumBlizzard.GetHashCode();
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
      if (HasReason) {
        output.WriteRawTag(8);
        output.WriteInt32(Reason);
      }
      if (HasNumAddon) {
        output.WriteRawTag(16);
        output.WriteUInt32(NumAddon);
      }
      if (HasNumBlizzard) {
        output.WriteRawTag(24);
        output.WriteUInt32(NumBlizzard);
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
      if (HasReason) {
        output.WriteRawTag(8);
        output.WriteInt32(Reason);
      }
      if (HasNumAddon) {
        output.WriteRawTag(16);
        output.WriteUInt32(NumAddon);
      }
      if (HasNumBlizzard) {
        output.WriteRawTag(24);
        output.WriteUInt32(NumBlizzard);
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
      if (HasReason) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(Reason);
      }
      if (HasNumAddon) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(NumAddon);
      }
      if (HasNumBlizzard) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(NumBlizzard);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(NameLookupInfo other) {
      if (other == null) {
        return;
      }
      if (other.HasReason) {
        Reason = other.Reason;
      }
      if (other.HasNumAddon) {
        NumAddon = other.NumAddon;
      }
      if (other.HasNumBlizzard) {
        NumBlizzard = other.NumBlizzard;
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
            Reason = input.ReadInt32();
            break;
          }
          case 16: {
            NumAddon = input.ReadUInt32();
            break;
          }
          case 24: {
            NumBlizzard = input.ReadUInt32();
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
            Reason = input.ReadInt32();
            break;
          }
          case 16: {
            NumAddon = input.ReadUInt32();
            break;
          }
          case 24: {
            NumBlizzard = input.ReadUInt32();
            break;
          }
        }
      }
    }
    #endif

  }

  public sealed partial class ClientNameLookup : pb::IMessage<ClientNameLookup>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<ClientNameLookup> _parser = new pb::MessageParser<ClientNameLookup>(() => new ClientNameLookup());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<ClientNameLookup> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Blizzard.Telemetry.Wow.Client.ClientNameLookupReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ClientNameLookup() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ClientNameLookup(ClientNameLookup other) : this() {
      nameLookups_ = other.nameLookups_.Clone();
      playerWorldInfo_ = other.playerWorldInfo_ != null ? other.playerWorldInfo_.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ClientNameLookup Clone() {
      return new ClientNameLookup(this);
    }

    /// <summary>Field number for the "name_lookups" field.</summary>
    public const int NameLookupsFieldNumber = 1;
    private static readonly pb::FieldCodec<global::Blizzard.Telemetry.Wow.Client.NameLookupInfo> _repeated_nameLookups_codec
        = pb::FieldCodec.ForMessage(10, global::Blizzard.Telemetry.Wow.Client.NameLookupInfo.Parser);
    private readonly pbc::RepeatedField<global::Blizzard.Telemetry.Wow.Client.NameLookupInfo> nameLookups_ = new pbc::RepeatedField<global::Blizzard.Telemetry.Wow.Client.NameLookupInfo>();
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public pbc::RepeatedField<global::Blizzard.Telemetry.Wow.Client.NameLookupInfo> NameLookups {
      get { return nameLookups_; }
    }

    /// <summary>Field number for the "player_world_info" field.</summary>
    public const int PlayerWorldInfoFieldNumber = 2;
    private global::Blizzard.Telemetry.Wow.Client.WorldInfo playerWorldInfo_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::Blizzard.Telemetry.Wow.Client.WorldInfo PlayerWorldInfo {
      get { return playerWorldInfo_; }
      set {
        playerWorldInfo_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as ClientNameLookup);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(ClientNameLookup other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if(!nameLookups_.Equals(other.nameLookups_)) return false;
      if (!object.Equals(PlayerWorldInfo, other.PlayerWorldInfo)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      hash ^= nameLookups_.GetHashCode();
      if (playerWorldInfo_ != null) hash ^= PlayerWorldInfo.GetHashCode();
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
      nameLookups_.WriteTo(output, _repeated_nameLookups_codec);
      if (playerWorldInfo_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(PlayerWorldInfo);
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
      nameLookups_.WriteTo(ref output, _repeated_nameLookups_codec);
      if (playerWorldInfo_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(PlayerWorldInfo);
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
      size += nameLookups_.CalculateSize(_repeated_nameLookups_codec);
      if (playerWorldInfo_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(PlayerWorldInfo);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(ClientNameLookup other) {
      if (other == null) {
        return;
      }
      nameLookups_.Add(other.nameLookups_);
      if (other.playerWorldInfo_ != null) {
        if (playerWorldInfo_ == null) {
          PlayerWorldInfo = new global::Blizzard.Telemetry.Wow.Client.WorldInfo();
        }
        PlayerWorldInfo.MergeFrom(other.PlayerWorldInfo);
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
            nameLookups_.AddEntriesFrom(input, _repeated_nameLookups_codec);
            break;
          }
          case 18: {
            if (playerWorldInfo_ == null) {
              PlayerWorldInfo = new global::Blizzard.Telemetry.Wow.Client.WorldInfo();
            }
            input.ReadMessage(PlayerWorldInfo);
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
            nameLookups_.AddEntriesFrom(ref input, _repeated_nameLookups_codec);
            break;
          }
          case 18: {
            if (playerWorldInfo_ == null) {
              PlayerWorldInfo = new global::Blizzard.Telemetry.Wow.Client.WorldInfo();
            }
            input.ReadMessage(PlayerWorldInfo);
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