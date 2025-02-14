using System.Threading.Tasks;

namespace ITN2163_AS_Assignment2.Services
{
    public interface IAuditLogService
    {
        Task LogActivityAsync(string userId, string activity, string ipAddress);
    }
}
