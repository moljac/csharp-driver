﻿//
//       Copyright (C) DataStax Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;

namespace Cassandra.Serialization
{
    /// <summary>
    /// Serializer instance tied to a specific protocol version.
    /// </summary>
    internal interface ISerializer : IGenericSerializer
    {
        /// <summary>
        /// Protocol version tied to this serializer instance.
        /// </summary>
        ProtocolVersion ProtocolVersion { get; }

        object Deserialize(byte[] buffer, int offset, int length, ColumnTypeCode typeCode, IColumnInfo typeInfo);

        object DeserializeAndDecrypt(string ks, string table, string col, byte[] buffer, int offset, int length, ColumnTypeCode typeCode, IColumnInfo typeInfo);

        byte[] Serialize(object value);

        byte[] SerializeAndEncrypt(string defaultKs, RowSetMetadata metadata, int colMetadataIdx, object[] values, int valueIdx);

        /// <summary>
        /// Create a new serializer with the provided protocol version.
        /// </summary>
        ISerializer CloneWithProtocolVersion(ProtocolVersion version);

        bool IsEncryptionEnabled { get; }

        Tuple<bool, ColumnTypeCode, IColumnInfo> IsAssignableFromEncrypted(string ks, string table, string column, ColumnTypeCode columnTypeCode, IColumnInfo typeInfo, object value, out string failureMsg);
    }
}