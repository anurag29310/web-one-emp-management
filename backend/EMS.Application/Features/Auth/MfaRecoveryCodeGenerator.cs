using System.Collections.Generic;
using System.Security.Cryptography;

namespace EMS.Application.Features.Auth
{
    internal static class MfaRecoveryCodeGenerator
    {
        // Excludes 0/O and 1/I/L so a handwritten or half-remembered code isn't ambiguous.
        private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        private const int CodeCount = 10;
        private const int SegmentLength = 5;

        public static List<string> GenerateCodes()
        {
            var codes = new List<string>(CodeCount);
            for (var i = 0; i < CodeCount; i++)
                codes.Add($"{RandomSegment()}-{RandomSegment()}");
            return codes;
        }

        private static string RandomSegment()
        {
            var chars = new char[SegmentLength];
            for (var i = 0; i < SegmentLength; i++)
                chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            return new string(chars);
        }
    }
}
