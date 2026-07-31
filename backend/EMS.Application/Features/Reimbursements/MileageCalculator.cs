namespace EMS.Application.Features.Reimbursements
{
    /// <summary>Computes the Amount for a mileage claim (DistanceKm * MileageRatePerKm), rounded to 2
    /// decimal places to match Amount's decimal(18,2) column — the rate is resolved by the caller from
    /// configuration and passed in here rather than read directly, keeping this a pure calculation.</summary>
    public static class MileageCalculator
    {
        public static decimal CalculateAmount(decimal distanceKm, decimal ratePerKm) =>
            decimal.Round(distanceKm * ratePerKm, 2, System.MidpointRounding.AwayFromZero);
    }
}
