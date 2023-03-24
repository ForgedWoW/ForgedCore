// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: TelemetrySharedClientImports.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Blizzard.Telemetry.Wow.Client {

  /// <summary>Holder for reflection information generated from TelemetrySharedClientImports.proto</summary>
  public static partial class TelemetrySharedClientImportsReflection {

    #region Descriptor
    /// <summary>File descriptor for TelemetrySharedClientImports.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static TelemetrySharedClientImportsReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CiJUZWxlbWV0cnlTaGFyZWRDbGllbnRJbXBvcnRzLnByb3RvEh1CbGl6emFy",
            "ZC5UZWxlbWV0cnkuV293LkNsaWVudCJ6CgpDbGllbnRJbmZvEhQKDGNmZ19y",
            "ZWFsbV9pZBgBIAEoDRIVCg1jZmdfcmVnaW9uX2lkGAIgASgNEhMKC2NmZ19z",
            "aXRlX2lkGAMgASgNEhUKDXJlYWxtX2FkZHJlc3MYBCABKA0SEwoLbnVtX2Ns",
            "aWVudHMYBSABKA0iPwoJV29ybGRJbmZvEhEKCXBsYXllcl9pZBgBIAEoBBIO",
            "CgZtYXBfaWQYAiABKA0SDwoHYXJlYV9pZBgDIAEoDSJUChhUcmFkaW5nUG9z",
            "dEl0ZW1TZWxlY3Rpb24SHAoUcGVya3NfdmVuZG9yX2l0ZW1faWQYASABKA0S",
            "GgoSbnVtX3NlbGVjdGVkX3RpbWVzGAIgASgN"));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Blizzard.Telemetry.Wow.Client.ClientInfo), global::Blizzard.Telemetry.Wow.Client.ClientInfo.Parser, new[]{ "CfgRealmId", "CfgRegionId", "CfgSiteId", "RealmAddress", "NumClients" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Blizzard.Telemetry.Wow.Client.WorldInfo), global::Blizzard.Telemetry.Wow.Client.WorldInfo.Parser, new[]{ "PlayerId", "MapId", "AreaId" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Blizzard.Telemetry.Wow.Client.TradingPostItemSelection), global::Blizzard.Telemetry.Wow.Client.TradingPostItemSelection.Parser, new[]{ "PerksVendorItemId", "NumSelectedTimes" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class ClientInfo : pb::IMessage<ClientInfo>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<ClientInfo> _parser = new pb::MessageParser<ClientInfo>(() => new ClientInfo());
    private pb::UnknownFieldSet _unknownFields;
    private int _hasBits0;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<ClientInfo> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Blizzard.Telemetry.Wow.Client.TelemetrySharedClientImportsReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ClientInfo() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ClientInfo(ClientInfo other) : this() {
      _hasBits0 = other._hasBits0;
      cfgRealmId_ = other.cfgRealmId_;
      cfgRegionId_ = other.cfgRegionId_;
      cfgSiteId_ = other.cfgSiteId_;
      realmAddress_ = other.realmAddress_;
      numClients_ = other.numClients_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ClientInfo Clone() {
      return new ClientInfo(this);
    }

    /// <summary>Field number for the "cfg_realm_id" field.</summary>
    public const int CfgRealmIdFieldNumber = 1;
    private readonly static uint CfgRealmIdDefaultValue = 0;

    private uint cfgRealmId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public uint CfgRealmId {
      get { if ((_hasBits0 & 1) != 0) { return cfgRealmId_; } else { return CfgRealmIdDefaultValue; } }
      set {
        _hasBits0 |= 1;
        cfgRealmId_ = value;
      }
    }
    /// <summary>Gets whether the "cfg_realm_id" field is set</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool HasCfgRealmId {
      get { return (_hasBits0 & 1) != 0; }
    }
    /// <summary>Clears the value of the "cfg_realm_id" field</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void ClearCfgRealmId() {
      _hasBits0 &= ~1;
    }

    /// <summary>Field number for the "cfg_region_id" field.</summary>
    public const int CfgRegionIdFieldNumber = 2;
    private readonly static uint CfgRegionIdDefaultValue = 0;

    private uint cfgRegionId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public uint CfgRegionId {
      get { if ((_hasBits0 & 2) != 0) { return cfgRegionId_; } else { return CfgRegionIdDefaultValue; } }
      set {
        _hasBits0 |= 2;
        cfgRegionId_ = value;
      }
    }
    /// <summary>Gets whether the "cfg_region_id" field is set</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool HasCfgRegionId {
      get { return (_hasBits0 & 2) != 0; }
    }
    /// <summary>Clears the value of the "cfg_region_id" field</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void ClearCfgRegionId() {
      _hasBits0 &= ~2;
    }

    /// <summary>Field number for the "cfg_site_id" field.</summary>
    public const int CfgSiteIdFieldNumber = 3;
    private readonly static uint CfgSiteIdDefaultValue = 0;

    private uint cfgSiteId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public uint CfgSiteId {
      get { if ((_hasBits0 & 4) != 0) { return cfgSiteId_; } else { return CfgSiteIdDefaultValue; } }
      set {
        _hasBits0 |= 4;
        cfgSiteId_ = value;
      }
    }
    /// <summary>Gets whether the "cfg_site_id" field is set</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool HasCfgSiteId {
      get { return (_hasBits0 & 4) != 0; }
    }
    /// <summary>Clears the value of the "cfg_site_id" field</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void ClearCfgSiteId() {
      _hasBits0 &= ~4;
    }

    /// <summary>Field number for the "realm_address" field.</summary>
    public const int RealmAddressFieldNumber = 4;
    private readonly static uint RealmAddressDefaultValue = 0;

    private uint realmAddress_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public uint RealmAddress {
      get { if ((_hasBits0 & 8) != 0) { return realmAddress_; } else { return RealmAddressDefaultValue; } }
      set {
        _hasBits0 |= 8;
        realmAddress_ = value;
      }
    }
    /// <summary>Gets whether the "realm_address" field is set</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool HasRealmAddress {
      get { return (_hasBits0 & 8) != 0; }
    }
    /// <summary>Clears the value of the "realm_address" field</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void ClearRealmAddress() {
      _hasBits0 &= ~8;
    }

    /// <summary>Field number for the "num_clients" field.</summary>
    public const int NumClientsFieldNumber = 5;
    private readonly static uint NumClientsDefaultValue = 0;

    private uint numClients_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public uint NumClients {
      get { if ((_hasBits0 & 16) != 0) { return numClients_; } else { return NumClientsDefaultValue; } }
      set {
        _hasBits0 |= 16;
        numClients_ = value;
      }
    }
    /// <summary>Gets whether the "num_clients" field is set</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool HasNumClients {
      get { return (_hasBits0 & 16) != 0; }
    }
    /// <summary>Clears the value of the "num_clients" field</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void ClearNumClients() {
      _hasBits0 &= ~16;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as ClientInfo);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(ClientInfo other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (CfgRealmId != other.CfgRealmId) return false;
      if (CfgRegionId != other.CfgRegionId) return false;
      if (CfgSiteId != other.CfgSiteId) return false;
      if (RealmAddress != other.RealmAddress) return false;
      if (NumClients != other.NumClients) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (HasCfgRealmId) hash ^= CfgRealmId.GetHashCode();
      if (HasCfgRegionId) hash ^= CfgRegionId.GetHashCode();
      if (HasCfgSiteId) hash ^= CfgSiteId.GetHashCode();
      if (HasRealmAddress) hash ^= RealmAddress.GetHashCode();
      if (HasNumClients) hash ^= NumClients.GetHashCode();
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
      if (HasCfgRealmId) {
        output.WriteRawTag(8);
        output.WriteUInt32(CfgRealmId);
      }
      if (HasCfgRegionId) {
        output.WriteRawTag(16);
        output.WriteUInt32(CfgRegionId);
      }
      if (HasCfgSiteId) {
        output.WriteRawTag(24);
        output.WriteUInt32(CfgSiteId);
      }
      if (HasRealmAddress) {
        output.WriteRawTag(32);
        output.WriteUInt32(RealmAddress);
      }
      if (HasNumClients) {
        output.WriteRawTag(40);
        output.WriteUInt32(NumClients);
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
      if (HasCfgRealmId) {
        output.WriteRawTag(8);
        output.WriteUInt32(CfgRealmId);
      }
      if (HasCfgRegionId) {
        output.WriteRawTag(16);
        output.WriteUInt32(CfgRegionId);
      }
      if (HasCfgSiteId) {
        output.WriteRawTag(24);
        output.WriteUInt32(CfgSiteId);
      }
      if (HasRealmAddress) {
        output.WriteRawTag(32);
        output.WriteUInt32(RealmAddress);
      }
      if (HasNumClients) {
        output.WriteRawTag(40);
        output.WriteUInt32(NumClients);
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
      if (HasCfgRealmId) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(CfgRealmId);
      }
      if (HasCfgRegionId) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(CfgRegionId);
      }
      if (HasCfgSiteId) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(CfgSiteId);
      }
      if (HasRealmAddress) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(RealmAddress);
      }
      if (HasNumClients) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(NumClients);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(ClientInfo other) {
      if (other == null) {
        return;
      }
      if (other.HasCfgRealmId) {
        CfgRealmId = other.CfgRealmId;
      }
      if (other.HasCfgRegionId) {
        CfgRegionId = other.CfgRegionId;
      }
      if (other.HasCfgSiteId) {
        CfgSiteId = other.CfgSiteId;
      }
      if (other.HasRealmAddress) {
        RealmAddress = other.RealmAddress;
      }
      if (other.HasNumClients) {
        NumClients = other.NumClients;
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
            CfgRealmId = input.ReadUInt32();
            break;
          }
          case 16: {
            CfgRegionId = input.ReadUInt32();
            break;
          }
          case 24: {
            CfgSiteId = input.ReadUInt32();
            break;
          }
          case 32: {
            RealmAddress = input.ReadUInt32();
            break;
          }
          case 40: {
            NumClients = input.ReadUInt32();
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
            CfgRealmId = input.ReadUInt32();
            break;
          }
          case 16: {
            CfgRegionId = input.ReadUInt32();
            break;
          }
          case 24: {
            CfgSiteId = input.ReadUInt32();
            break;
          }
          case 32: {
            RealmAddress = input.ReadUInt32();
            break;
          }
          case 40: {
            NumClients = input.ReadUInt32();
            break;
          }
        }
      }
    }
    #endif

  }

  public sealed partial class WorldInfo : pb::IMessage<WorldInfo>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<WorldInfo> _parser = new pb::MessageParser<WorldInfo>(() => new WorldInfo());
    private pb::UnknownFieldSet _unknownFields;
    private int _hasBits0;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<WorldInfo> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Blizzard.Telemetry.Wow.Client.TelemetrySharedClientImportsReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public WorldInfo() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public WorldInfo(WorldInfo other) : this() {
      _hasBits0 = other._hasBits0;
      playerId_ = other.playerId_;
      mapId_ = other.mapId_;
      areaId_ = other.areaId_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public WorldInfo Clone() {
      return new WorldInfo(this);
    }

    /// <summary>Field number for the "player_id" field.</summary>
    public const int PlayerIdFieldNumber = 1;
    private readonly static ulong PlayerIdDefaultValue = 0UL;

    private ulong playerId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ulong PlayerId {
      get { if ((_hasBits0 & 1) != 0) { return playerId_; } else { return PlayerIdDefaultValue; } }
      set {
        _hasBits0 |= 1;
        playerId_ = value;
      }
    }
    /// <summary>Gets whether the "player_id" field is set</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool HasPlayerId {
      get { return (_hasBits0 & 1) != 0; }
    }
    /// <summary>Clears the value of the "player_id" field</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void ClearPlayerId() {
      _hasBits0 &= ~1;
    }

    /// <summary>Field number for the "map_id" field.</summary>
    public const int MapIdFieldNumber = 2;
    private readonly static uint MapIdDefaultValue = 0;

    private uint mapId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public uint MapId {
      get { if ((_hasBits0 & 2) != 0) { return mapId_; } else { return MapIdDefaultValue; } }
      set {
        _hasBits0 |= 2;
        mapId_ = value;
      }
    }
    /// <summary>Gets whether the "map_id" field is set</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool HasMapId {
      get { return (_hasBits0 & 2) != 0; }
    }
    /// <summary>Clears the value of the "map_id" field</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void ClearMapId() {
      _hasBits0 &= ~2;
    }

    /// <summary>Field number for the "area_id" field.</summary>
    public const int AreaIdFieldNumber = 3;
    private readonly static uint AreaIdDefaultValue = 0;

    private uint areaId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public uint AreaId {
      get { if ((_hasBits0 & 4) != 0) { return areaId_; } else { return AreaIdDefaultValue; } }
      set {
        _hasBits0 |= 4;
        areaId_ = value;
      }
    }
    /// <summary>Gets whether the "area_id" field is set</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool HasAreaId {
      get { return (_hasBits0 & 4) != 0; }
    }
    /// <summary>Clears the value of the "area_id" field</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void ClearAreaId() {
      _hasBits0 &= ~4;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as WorldInfo);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(WorldInfo other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (PlayerId != other.PlayerId) return false;
      if (MapId != other.MapId) return false;
      if (AreaId != other.AreaId) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (HasPlayerId) hash ^= PlayerId.GetHashCode();
      if (HasMapId) hash ^= MapId.GetHashCode();
      if (HasAreaId) hash ^= AreaId.GetHashCode();
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
      if (HasPlayerId) {
        output.WriteRawTag(8);
        output.WriteUInt64(PlayerId);
      }
      if (HasMapId) {
        output.WriteRawTag(16);
        output.WriteUInt32(MapId);
      }
      if (HasAreaId) {
        output.WriteRawTag(24);
        output.WriteUInt32(AreaId);
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
      if (HasPlayerId) {
        output.WriteRawTag(8);
        output.WriteUInt64(PlayerId);
      }
      if (HasMapId) {
        output.WriteRawTag(16);
        output.WriteUInt32(MapId);
      }
      if (HasAreaId) {
        output.WriteRawTag(24);
        output.WriteUInt32(AreaId);
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
      if (HasPlayerId) {
        size += 1 + pb::CodedOutputStream.ComputeUInt64Size(PlayerId);
      }
      if (HasMapId) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(MapId);
      }
      if (HasAreaId) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(AreaId);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(WorldInfo other) {
      if (other == null) {
        return;
      }
      if (other.HasPlayerId) {
        PlayerId = other.PlayerId;
      }
      if (other.HasMapId) {
        MapId = other.MapId;
      }
      if (other.HasAreaId) {
        AreaId = other.AreaId;
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
            PlayerId = input.ReadUInt64();
            break;
          }
          case 16: {
            MapId = input.ReadUInt32();
            break;
          }
          case 24: {
            AreaId = input.ReadUInt32();
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
            PlayerId = input.ReadUInt64();
            break;
          }
          case 16: {
            MapId = input.ReadUInt32();
            break;
          }
          case 24: {
            AreaId = input.ReadUInt32();
            break;
          }
        }
      }
    }
    #endif

  }

  public sealed partial class TradingPostItemSelection : pb::IMessage<TradingPostItemSelection>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<TradingPostItemSelection> _parser = new pb::MessageParser<TradingPostItemSelection>(() => new TradingPostItemSelection());
    private pb::UnknownFieldSet _unknownFields;
    private int _hasBits0;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<TradingPostItemSelection> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Blizzard.Telemetry.Wow.Client.TelemetrySharedClientImportsReflection.Descriptor.MessageTypes[2]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public TradingPostItemSelection() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public TradingPostItemSelection(TradingPostItemSelection other) : this() {
      _hasBits0 = other._hasBits0;
      perksVendorItemId_ = other.perksVendorItemId_;
      numSelectedTimes_ = other.numSelectedTimes_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public TradingPostItemSelection Clone() {
      return new TradingPostItemSelection(this);
    }

    /// <summary>Field number for the "perks_vendor_item_id" field.</summary>
    public const int PerksVendorItemIdFieldNumber = 1;
    private readonly static uint PerksVendorItemIdDefaultValue = 0;

    private uint perksVendorItemId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public uint PerksVendorItemId {
      get { if ((_hasBits0 & 1) != 0) { return perksVendorItemId_; } else { return PerksVendorItemIdDefaultValue; } }
      set {
        _hasBits0 |= 1;
        perksVendorItemId_ = value;
      }
    }
    /// <summary>Gets whether the "perks_vendor_item_id" field is set</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool HasPerksVendorItemId {
      get { return (_hasBits0 & 1) != 0; }
    }
    /// <summary>Clears the value of the "perks_vendor_item_id" field</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void ClearPerksVendorItemId() {
      _hasBits0 &= ~1;
    }

    /// <summary>Field number for the "num_selected_times" field.</summary>
    public const int NumSelectedTimesFieldNumber = 2;
    private readonly static uint NumSelectedTimesDefaultValue = 0;

    private uint numSelectedTimes_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public uint NumSelectedTimes {
      get { if ((_hasBits0 & 2) != 0) { return numSelectedTimes_; } else { return NumSelectedTimesDefaultValue; } }
      set {
        _hasBits0 |= 2;
        numSelectedTimes_ = value;
      }
    }
    /// <summary>Gets whether the "num_selected_times" field is set</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool HasNumSelectedTimes {
      get { return (_hasBits0 & 2) != 0; }
    }
    /// <summary>Clears the value of the "num_selected_times" field</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void ClearNumSelectedTimes() {
      _hasBits0 &= ~2;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as TradingPostItemSelection);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(TradingPostItemSelection other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (PerksVendorItemId != other.PerksVendorItemId) return false;
      if (NumSelectedTimes != other.NumSelectedTimes) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (HasPerksVendorItemId) hash ^= PerksVendorItemId.GetHashCode();
      if (HasNumSelectedTimes) hash ^= NumSelectedTimes.GetHashCode();
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
      if (HasPerksVendorItemId) {
        output.WriteRawTag(8);
        output.WriteUInt32(PerksVendorItemId);
      }
      if (HasNumSelectedTimes) {
        output.WriteRawTag(16);
        output.WriteUInt32(NumSelectedTimes);
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
      if (HasPerksVendorItemId) {
        output.WriteRawTag(8);
        output.WriteUInt32(PerksVendorItemId);
      }
      if (HasNumSelectedTimes) {
        output.WriteRawTag(16);
        output.WriteUInt32(NumSelectedTimes);
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
      if (HasPerksVendorItemId) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(PerksVendorItemId);
      }
      if (HasNumSelectedTimes) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(NumSelectedTimes);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(TradingPostItemSelection other) {
      if (other == null) {
        return;
      }
      if (other.HasPerksVendorItemId) {
        PerksVendorItemId = other.PerksVendorItemId;
      }
      if (other.HasNumSelectedTimes) {
        NumSelectedTimes = other.NumSelectedTimes;
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
            PerksVendorItemId = input.ReadUInt32();
            break;
          }
          case 16: {
            NumSelectedTimes = input.ReadUInt32();
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
            PerksVendorItemId = input.ReadUInt32();
            break;
          }
          case 16: {
            NumSelectedTimes = input.ReadUInt32();
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