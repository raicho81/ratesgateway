using System;

namespace RatesGatwewayApi
{
    public static class TimestampConversions
    {
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static double DateTimeToUnixTime(DateTime dtDateTime)
        {
            double unixTimeStamp;
            DateTime zuluTime = dtDateTime.ToUniversalTime();
            DateTime unixEpoch = new DateTime(1970, 1, 1);
            unixTimeStamp = (zuluTime.Subtract(unixEpoch)).TotalSeconds;
            return unixTimeStamp;
        }
    }
}
