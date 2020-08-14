using Audit.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Dapper;
using System.Data;
using Audit.WebApi;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// Custom Audit Data Provider
    /// </summary>
    public class RAuditDataProvider : AuditDataProvider, IDisposable
    {
        /// <summary>
        /// Insert
        /// </summary>
        /// <param name="auditEvent"></param>
        /// <returns></returns>
        public override object InsertEvent(AuditEvent auditEvent)
        {
            if (_connection == null)
                return null;
            AuditEventWebApi apiEvent = (AuditEventWebApi)auditEvent;
            Guid Id = new Guid(apiEvent.Action.TraceId);
            string sql = $"INSERT INTO AuditLogs (Id, EventType, StartDate, EndDate, Duration, UserName, IpAddress, ResponseStatusCode, RequestUrl, JsonData) VALUES (@Id, @EventType, @StartDate, @EndDate, @Duration, @UserName, @IpAddress, @ResponseStatusCode, @RequestUrl, @JsonData);";
            _connection.Execute(sql, new { Id, apiEvent.EventType, apiEvent.StartDate, apiEvent.EndDate, apiEvent.Duration, apiEvent.Action.UserName, apiEvent.Action.IpAddress, apiEvent.Action.ResponseStatusCode, apiEvent.Action.RequestUrl, JsonData = apiEvent.ToJson() });
            return Id;
        }

        /// <summary>
        /// Insert Async
        /// </summary>
        /// <param name="auditEvent"></param>
        /// <returns></returns>
        public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            if (_connection == null)
                return null;
            AuditEventWebApi apiEvent = (AuditEventWebApi)auditEvent;
            Guid Id = new Guid(apiEvent.Action.TraceId);
            string sql = $"INSERT INTO AuditLogs (Id, EventType, StartDate, EndDate, Duration, UserName, IpAddress, ResponseStatusCode, RequestUrl, JsonData) VALUES (@Id, @EventType, @StartDate, @EndDate, @Duration, @UserName, @IpAddress, @ResponseStatusCode, @RequestUrl, @JsonData);";
            await _connection.ExecuteAsync(sql, new { Id, apiEvent.EventType, apiEvent.StartDate, apiEvent.EndDate, apiEvent.Duration, apiEvent.Action.UserName, apiEvent.Action.IpAddress, apiEvent.Action.ResponseStatusCode, apiEvent.Action.RequestUrl, JsonData = apiEvent.ToJson() });
            return Id;
        }

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="auditEvent"></param>
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            if (_connection == null)
                return;
            Guid Id = new Guid(eventId.ToString());
            IDbConnection dapper = _connection;
            dapper.Execute($"UPDATE AuditLogs SET JsonData = @JsonData, EndDate = @EndDate WHERE Id = @Id", new { JsonData = auditEvent.ToJson(), auditEvent.EndDate, Id });
        }

        /// <summary>
        /// Update Async
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="auditEvent"></param>
        /// <returns></returns>
        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent)
        {
            if (_connection == null)
                return;
            Guid Id = new Guid(eventId.ToString());
            IDbConnection dapper = _connection;
            await dapper.ExecuteAsync($"UPDATE AuditLogs SET JsonData = @JsonData, EndDate = @EndDate WHERE Id = @Id", new { JsonData = auditEvent.ToJson(), auditEvent.EndDate, Id });
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventId"></param>
        /// <returns></returns>
        public override T GetEvent<T>(object eventId)
        {
            if (_connection == null)
                return null;
            Guid id = new Guid(eventId.ToString());
            IDbConnection dapper = _connection;
            return JsonConvert.DeserializeObject<T>(dapper.QueryFirstOrDefault<string>($"SELECT JsonData FROM AuditLogs WHERE Id = '{id}'"));
        }
       
  

        

        /// <summary>
        /// Get Async
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventId"></param>
        /// <returns></returns>
        public override async Task<T> GetEventAsync<T>(object eventId)
        {
            if (_connection == null)
                return null;
            Guid id = new Guid(eventId.ToString());
            IDbConnection dapper = _connection;
            return JsonConvert.DeserializeObject<T>((await dapper.QueryFirstOrDefaultAsync<string>($"SELECT JsonData FROM AuditLogs WHERE Id = '{id}'")));
        }

        /// <summary>
        /// is disposed
        /// </summary>
        private bool _disposed = false;
        
        /// <summary>
        /// dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                if(_connection != null)
                    _connection.Dispose();
                _connection = null;
            }

            _disposed = true;
        }


        /// <summary>
        /// Connection
        /// </summary>
        private SqlConnection _connection;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public RAuditDataProvider(string connectionString)
        {
            try
            {
                _connection = new SqlConnection(connectionString);
                _connection.Open();
            }
            catch //this happens when database does not exits, it is only the first run of the app
            {
                _connection = null;
            }
        }
    }
}
