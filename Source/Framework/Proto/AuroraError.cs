// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: AuroraError.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Blizzard.Telemetry.Wow.Client {

  /// <summary>Holder for reflection information generated from AuroraError.proto</summary>
  public static partial class AuroraErrorReflection {

    #region Descriptor
    /// <summary>File descriptor for AuroraError.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static AuroraErrorReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChFBdXJvcmFFcnJvci5wcm90bxIdQmxpenphcmQuVGVsZW1ldHJ5Lldvdy5D",
            "bGllbnQaGnRlbGVtZXRyeV9leHRlbnNpb25zLnByb3RvGiJUZWxlbWV0cnlT",
            "aGFyZWRDbGllbnRJbXBvcnRzLnByb3RvImUKC0F1cm9yYUVycm9yEhIKCmVy",
            "cm9yX2NvZGUYASABKA0SOQoGY2xpZW50GAIgASgLMikuQmxpenphcmQuVGVs",
            "ZW1ldHJ5Lldvdy5DbGllbnQuQ2xpZW50SW5mbzoHwswlA6AGAQ=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Blizzard.Telemetry.TelemetryExtensionsReflection.Descriptor, global::Blizzard.Telemetry.Wow.Client.TelemetrySharedClientImportsReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Blizzard.Telemetry.Wow.Client.AuroraError), global::Blizzard.Telemetry.Wow.Client.AuroraError.Parser, new[]{ "ErrorCode", "Client" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class AuroraError : pb::IMessage<AuroraError>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<AuroraError> _parser = new pb::MessageParser<AuroraError>(() => new AuroraError());
    private pb::UnknownFieldSet _unknownFields;
    private int _hasBits0;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<AuroraError> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Blizzard.Telemetry.Wow.Client.AuroraErrorReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public AuroraError() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public AuroraError(AuroraError other) : this() {
      _hasBits0 = other._hasBits0;
      errorCode_ = other.errorCode_;
      client_ = other.client_ != null ? other.client_.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public AuroraError Clone() {
      return new AuroraError(this);
    }

    /// <summary>Field number for the "error_code" field.</summary>
    public const int ErrorCodeFieldNumber = 1;
    private readonly static uint ErrorCodeDefaultValue = 0;

    private uint errorCode_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public uint ErrorCode {
      get { if ((_hasBits0 & 1) != 0) { return errorCode_; } else { return ErrorCodeDefaultValue; } }
      set {
        _hasBits0 |= 1;
        errorCode_ = value;
      }
    }
    /// <summary>Gets whether the "error_code" field is set</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool HasErrorCode {
      get { return (_hasBits0 & 1) != 0; }
    }
    /// <summary>Clears the value of the "error_code" field</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void ClearErrorCode() {
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

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as AuroraError);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(AuroraError other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (ErrorCode != other.ErrorCode) return false;
      if (!object.Equals(Client, other.Client)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (HasErrorCode) hash ^= ErrorCode.GetHashCode();
      if (client_ != null) hash ^= Client.GetHashCode();
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
      if (HasErrorCode) {
        output.WriteRawTag(8);
        output.WriteUInt32(ErrorCode);
      }
      if (client_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(Client);
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
      if (HasErrorCode) {
        output.WriteRawTag(8);
        output.WriteUInt32(ErrorCode);
      }
      if (client_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(Client);
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
      if (HasErrorCode) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(ErrorCode);
      }
      if (client_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Client);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(AuroraError other) {
      if (other == null) {
        return;
      }
      if (other.HasErrorCode) {
        ErrorCode = other.ErrorCode;
      }
      if (other.client_ != null) {
        if (client_ == null) {
          Client = new global::Blizzard.Telemetry.Wow.Client.ClientInfo();
        }
        Client.MergeFrom(other.Client);
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
            ErrorCode = input.ReadUInt32();
            break;
          }
          case 18: {
            if (client_ == null) {
              Client = new global::Blizzard.Telemetry.Wow.Client.ClientInfo();
            }
            input.ReadMessage(Client);
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
            ErrorCode = input.ReadUInt32();
            break;
          }
          case 18: {
            if (client_ == null) {
              Client = new global::Blizzard.Telemetry.Wow.Client.ClientInfo();
            }
            input.ReadMessage(Client);
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