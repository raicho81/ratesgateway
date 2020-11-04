using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StatisticsCollector
{
    public enum ResponseStatusCodes
    {
        Success = 1000,
        InvalidRequestId = 1001,
    }

    public static class ResponseStatusMessages
    {
        public static readonly Dictionary<int, string> Messages = new Dictionary<int, string>
        {
            {(int)ResponseStatusCodes.Success, "Everythig OK."},
            {(int)ResponseStatusCodes.InvalidRequestId, "Invalid request id (uuid)."},
        };
    }
}
