using System;

namespace EMS.Application.Features.Attendance
{
    /// <summary>Great-circle distance check for office geofencing — pure local computation, no external service (unlike IGeocodingService's reverse-geocode lookup).</summary>
    public static class GeofenceCalculator
    {
        private const double EarthRadiusMeters = 6371000;

        public static double DistanceMeters(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            var rlat1 = ToRadians((double)lat1);
            var rlat2 = ToRadians((double)lat2);
            var deltaLat = ToRadians((double)(lat2 - lat1));
            var deltaLon = ToRadians((double)(lon2 - lon1));

            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(rlat1) * Math.Cos(rlat2) *
                    Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusMeters * c;
        }

        /// <summary>True if the punch coordinates fall within the office's configured geofence. Geofencing
        /// is considered "not configured" (always true) unless the office has Latitude, Longitude, and
        /// GeofenceRadiusMeters all set.</summary>
        public static bool IsWithinGeofence(decimal punchLatitude, decimal punchLongitude, decimal? officeLatitude, decimal? officeLongitude, int? geofenceRadiusMeters)
        {
            if (officeLatitude == null || officeLongitude == null || geofenceRadiusMeters == null)
                return true;

            return DistanceMeters(punchLatitude, punchLongitude, officeLatitude.Value, officeLongitude.Value) <= geofenceRadiusMeters.Value;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
    }
}
