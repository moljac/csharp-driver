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
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using OpenTelemetry.Trace;

namespace Cassandra.OpenTelemetry
{
    /// <summary>
    /// OpenTelemetry request tracker implementation that includes tracing capabilities and follow
    /// the Trace Semantic Conventions v1.27.0.
    /// https://opentelemetry.io/docs/specs/semconv/database/database-spans/
    /// https://opentelemetry.io/docs/specs/semconv/database/cassandra/
    /// </summary>
    public class OpenTelemetryRequestTracker : IRequestTracker
    {
        internal static readonly ActivitySource ActivitySource = new ActivitySource(CassandraActivitySourceHelper.ActivitySourceName, CassandraActivitySourceHelper.Version);
        private readonly CassandraInstrumentationOptions _instrumentationOptions;
        private const string OtelActivityKey = "otel_activity";
        private const string OtelStmtKey = "otel_statement_string";
        private const string SessionOperationName = "Session_Request";
        private const string NodeOperationName = "Node_Request";

        /// <summary>
        /// Request Tracker implementation that implements OpenTelemetry instrumentation.
        /// </summary>
        public OpenTelemetryRequestTracker(CassandraInstrumentationOptions instrumentationOptions)
        {
            _instrumentationOptions = instrumentationOptions;
        }

        /// <summary>
        /// Starts an <see cref="Activity"/> when request starts and includes the following Cassandra specific tags:
        /// <list type="bullet">
        /// <item>
        /// <description>db.system that has a harcoded value of `cassandra`.</description>
        /// </item>
        /// <item>
        /// <description>db.operation.name that has a harcoded value of `Session Request`.</description>
        /// </item>
        /// <item>
        /// <description>db.namespace that has the Keyspace value, if set.</description>
        /// </item>
        /// <item>
        /// <description>db.query.text that has the database query if included in <see cref="CassandraInstrumentationOptions"/>.</description>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="sessionRequest">Request contextual information.</param>
        /// <returns>Activity task.</returns>
        public virtual Task OnStartAsync(SessionRequestInfo sessionRequest)
        {
            var operationName = GetSessionOperationName(sessionRequest);

            var keyspace = GetKeyspaceFromSessionRequest(sessionRequest);
            var activity = ActivitySource.StartActivity(GetActivityName(operationName, keyspace), ActivityKind.Client);

            if (activity == null)
            {
                return Task.CompletedTask;
            }

            activity.AddTag("db.system", "cassandra");
            activity.AddTag("db.operation.name", operationName);

            if (activity.IsAllDataRequested)
            {
                if (!string.IsNullOrEmpty(keyspace))
                {
                    activity.AddTag("db.namespace", keyspace);
                }

                var queryText = GetQueryText(sessionRequest);
                if (_instrumentationOptions.IncludeDatabaseStatement && queryText != null)
                {
                    activity.AddTag("db.query.text", queryText);
                    sessionRequest.Items.TryAdd(OtelStmtKey, queryText);
                }
            }

            sessionRequest.Items.TryAdd(OtelActivityKey, activity);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Closes the <see cref="Activity"/> when the session request is successful.
        /// </summary>
        /// <param name="sessionRequest">Request contextual information.</param>
        /// <returns>Completed task.</returns>
        public virtual Task OnSuccessAsync(SessionRequestInfo sessionRequest)
        {
            sessionRequest.Items.TryRemove(OtelActivityKey, out var context);

            if (!(context is Activity activity))
            {
                return Task.CompletedTask;
            }
            activity.SetStatus(ActivityStatusCode.Ok);
            activity.Dispose();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Closes the <see cref="Activity"/> when the session request is unsuccessful.
        /// Includes an <see cref="ActivityEvent"/> containing information from the specified exception.
        /// </summary>
        /// <param name="sessionRequest">Request contextual information.</param>
        /// <param name="ex">Exception information.</param>
        /// <returns>Completed task.</returns>
        public virtual Task OnErrorAsync(SessionRequestInfo sessionRequest, Exception ex)
        {
            sessionRequest.Items.TryRemove(OtelActivityKey, out var context);

            if (!(context is Activity activity))
            {
                return Task.CompletedTask;
            }

            activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity.RecordException(ex);

            activity.Dispose();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Closes the <see cref="Activity"/> when the node request is successful.
        /// </summary>
        /// <param name="sessionRequest">Request contextual information.</param>
        /// <param name="nodeRequestInfo">Struct with host contextual information.</param>
        /// <returns>Completed task.</returns>
        public virtual Task OnNodeSuccessAsync(SessionRequestInfo sessionRequest, NodeRequestInfo nodeRequestInfo)
        {
            var activityKey = $"{OtelActivityKey}.{nodeRequestInfo.ExecutionId}";

            sessionRequest.Items.TryRemove(activityKey, out var context);

            if (!(context is Activity activity))
            {
                return Task.CompletedTask;
            }
            activity.SetStatus(ActivityStatusCode.Ok);
            activity.Dispose();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Closes the <see cref="Activity"/> when the node request level request is unsuccessful.
        /// Includes an <see cref="ActivityEvent"/> containing information from the specified exception.
        /// </summary>
        /// <param name="sessionRequest"><see cref="SessionRequestInfo"/> object with contextual information.</param>
        /// <param name="nodeRequestInfo">Struct with host contextual information.</param>
        /// <param name="ex">Exception information.</param>
        /// <returns>Completed task.</returns>
        public virtual Task OnNodeErrorAsync(SessionRequestInfo sessionRequest, NodeRequestInfo nodeRequestInfo, Exception ex)
        {
            var activityKey = $"{OtelActivityKey}.{nodeRequestInfo.ExecutionId}";
            
            sessionRequest.Items.TryRemove(activityKey, out var context);

            if (!(context is Activity activity))
            {
                return Task.CompletedTask;
            }

            activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity.RecordException(ex);

            activity.Dispose();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Closes the <see cref="Activity"/> when the node request is aborted (e.g. pending speculative execution).
        /// </summary>
        /// <param name="sessionRequest">Request contextual information.</param>
        /// <param name="nodeRequestInfo">Struct with host contextual information.</param>
        /// <returns>Completed task.</returns>
        public Task OnNodeAborted(SessionRequestInfo sessionRequest, NodeRequestInfo nodeRequestInfo)
        {
            var activityKey = $"{OtelActivityKey}.{nodeRequestInfo.ExecutionId}";

            sessionRequest.Items.TryRemove(activityKey, out var context);

            if (!(context is Activity activity))
            {
                return Task.CompletedTask;
            }

            activity.SetStatus(ActivityStatusCode.Unset);
            activity.Dispose();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Starts an <see cref="Activity"/> when node request starts and includes the following Cassandra specific tags:
        /// <list type="bullet">
        /// <item>
        /// <description>db.system that has a harcoded value of `cassandra`.</description>
        /// </item>
        /// <item>
        /// <description>db.operation.name that has a harcoded value of `Node Request`.</description>
        /// </item>
        /// <item>
        /// <description>db.namespace that has the Keyspace value, if set.</description>
        /// </item>
        /// <item>
        /// <description>db.query.text that has the database query if included in <see cref="CassandraInstrumentationOptions"/>.</description>
        /// </item>
        /// <item>
        /// <description>server.address that has the host address value.</description>
        /// </item>
        /// <item>
        /// <description>server.port that has the host port value.</description>
        /// </item>
        /// </list>
        /// </summary>
        /// <returns>Activity task.</returns>
        public virtual Task OnNodeStartAsync(SessionRequestInfo sessionRequest, NodeRequestInfo nodeRequestInfo)
        {
            sessionRequest.Items.TryGetValue(OtelActivityKey, out var sessionContext);

            if (!(sessionContext is Activity parentActivity))
            {
                return Task.CompletedTask;
            }

            var operationName = GetNodeOperationName(sessionRequest, nodeRequestInfo);
            var keyspace = GetKeyspaceFromNodeRequest(nodeRequestInfo) ?? GetKeyspaceFromSessionRequest(sessionRequest);
            var activity = ActivitySource.StartActivity(GetActivityName(operationName, keyspace), ActivityKind.Client, parentActivity.Context);

            if (activity == null)
            {
                return Task.CompletedTask;
            }

            activity.AddTag("db.system", "cassandra");
            activity.AddTag("db.operation.name", operationName);
            activity.AddTag("server.address", nodeRequestInfo.Host?.Address?.Address.ToString());
            activity.AddTag("server.port", nodeRequestInfo.Host?.Address?.Port.ToString());

            if (activity.IsAllDataRequested)
            {
                if (!string.IsNullOrEmpty(keyspace))
                {
                    activity.AddTag("db.namespace", keyspace);
                }

                if (_instrumentationOptions.IncludeDatabaseStatement)
                {
                    if (sessionRequest.Items.TryGetValue(OtelStmtKey, out var stmt))
                    {
                        activity.AddTag("db.query.text", stmt);
                    }
                }
            }

            sessionRequest.Items.TryAdd($"{OtelActivityKey}.{nodeRequestInfo.ExecutionId}", activity);

            return Task.CompletedTask;
        }

        private string GetSessionOperationName(SessionRequestInfo sessionRequest)
        {
            return $"{SessionOperationName}({sessionRequest.Statement?.GetType().Name ?? sessionRequest.PrepareRequest?.GetType().Name})";
        }

        private string GetNodeOperationName(SessionRequestInfo sessionRequest, NodeRequestInfo nodeRequest)
        {
            var nodePrepareRequestName = nodeRequest?.PrepareRequest?.GetType().Name;
            return $"{NodeOperationName}({nodePrepareRequestName ?? sessionRequest.Statement?.GetType().Name ?? sessionRequest.PrepareRequest?.GetType().Name})";
        }

        private string GetKeyspaceFromSessionRequest(SessionRequestInfo sessionRequest)
        {
            var ks = sessionRequest.Statement == null ? sessionRequest.PrepareRequest?.Keyspace : sessionRequest.Statement?.Keyspace;
            return ks ?? sessionRequest.SessionKeyspace;
        }

        private string GetKeyspaceFromNodeRequest(NodeRequestInfo nodeRequestInfo)
        {
            return nodeRequestInfo.PrepareRequest?.Keyspace;
        }

        private string GetQueryText(SessionRequestInfo sessionRequest)
        {
            return sessionRequest.Statement == null ? sessionRequest.PrepareRequest?.Query : GetQueryTextFromStatement(sessionRequest.Statement);
        }

        private string GetActivityName(string operationName, string ks)
        {
            return string.IsNullOrEmpty(ks) ? $"{operationName}" : $"{operationName} {ks}";
        }

        private string GetQueryTextFromStatement(IStatement statement)
        {
            string str;
            switch (statement)
            {
                case BatchStatement s:
                    var i = 0;
                    var sb = new StringBuilder();
                    var first = true;
                    foreach (var stmt in s.Statements)
                    {
                        if (i >= _instrumentationOptions.BatchChildStatementLimit)
                        {
                            break;
                        }
                        if (!first)
                        {
                            sb.Append($"; {stmt}");
                        }
                        else
                        {
                            sb.Append($"{stmt}");
                            first = false;
                        }
                    }

                    str = sb.ToString();
                    break;
                default:
                    str = statement.ToString();
                    break;
            }

            return str;
        }
    }
}
