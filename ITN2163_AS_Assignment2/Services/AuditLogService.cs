using System;
using System.Threading.Tasks;
using ITN2163_AS_Assignment2.Model;

namespace ITN2163_AS_Assignment2.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AuthDbContext _context;
        
        public AuditLogService(AuthDbContext context)
        {
            _context = context;
        }

        public async Task LogActivityAsync(string userId, string activity, string ipAddress)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Activity = activity,
                IPAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}
