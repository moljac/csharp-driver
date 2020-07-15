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

using Cassandra.Serialization.Graph.Tinkerpop.Structure.IO.GraphSON;

namespace Cassandra.Serialization.Graph.GraphSON2.Dse
{
    internal class LocalDateSerializer : StringBasedSerializer
    {
        private const string Prefix = "gx";
        private const string TypeKey = "LocalDate";

        public LocalDateSerializer() : base(LocalDateSerializer.Prefix, LocalDateSerializer.TypeKey)
        {
        }

        public static string TypeName =>
            GraphSONUtil.FormatTypeName(LocalDateSerializer.Prefix, LocalDateSerializer.TypeKey);

        protected override string ToString(dynamic obj)
        {
            LocalDate date = obj;
            return date.ToString();
        }

        protected override dynamic FromString(string str)
        {
            return LocalDate.Parse(str);
        }
    }
}