﻿//
//      Copyright (C) DataStax Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//

using System;

namespace Cassandra
{
    public class HostTrackingInfo : IEquatable<HostTrackingInfo>
    {
        public Host Host { get; }

        public Guid ExecutionId { get; }

        public HostTrackingInfo(Host host, Guid executionId)
        {
            Host = host;
            ExecutionId = executionId;
        }

        public bool Equals(HostTrackingInfo other)
        {
            return other != null && other.ExecutionId.Equals(ExecutionId);
        }

        public static bool operator ==(HostTrackingInfo a, HostTrackingInfo b)
        {
            if (a == null)
            {
                return b == null;
            }

            return a.Equals(b);
        }

        public static bool operator !=(HostTrackingInfo a, HostTrackingInfo b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is HostTrackingInfo info))
            {
                return false;
            }
            return Equals(info);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Host != null ? Host.GetHashCode() : 0) * 397) ^ ExecutionId.GetHashCode();
            }
        }
    }
}
