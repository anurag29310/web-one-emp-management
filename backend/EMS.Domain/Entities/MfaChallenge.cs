using System;

namespace EMS.Domain.Entities
{
    // Short-lived, single-use record backing the mfaChallengeId a client receives from
    // POST /auth/login when a second factor is required. Persisted (not cached in-memory) so it
    // survives process boundaries and works correctly behind a load balancer.
    public class MfaChallenge
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public bool IsConsumed { get; set; }
    }
}
