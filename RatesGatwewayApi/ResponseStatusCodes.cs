using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Collections;
using System.Collections.Generic;

namespace RatesGatwewayApi
{
    public enum ResponseStatusCodes
    {
        Success = 1000,
        NoData  = 1001,
        NoDataForPeriod = 1002,
        RequestExists = 1003,
        InvalidPeriod = 1004,
        InvalidUUID = 1005
    }

    public static class ResponseStatusMessages
    {
        public static readonly Dictionary<int, string> Messages = new Dictionary<int, string>
        {
            {(int)ResponseStatusCodes.Success, "Everythig OK."},
            {(int)ResponseStatusCodes.NoData, "No data in the DB."},
            {(int)ResponseStatusCodes.NoDataForPeriod, "No data in the DB for the period."},
            {(int)ResponseStatusCodes.RequestExists, "Request was already served."},
            {(int)ResponseStatusCodes.InvalidPeriod, "Invalid history time period."},
            {(int)ResponseStatusCodes.InvalidUUID, "Invalid Request Id (UUID) format."}
        };
    }
}