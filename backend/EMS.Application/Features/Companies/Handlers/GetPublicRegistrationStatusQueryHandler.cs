using EMS.Application.Features.Companies.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Companies.Handlers
{
    public class GetPublicRegistrationStatusQueryHandler : IRequestHandler<GetPublicRegistrationStatusQuery, bool>
    {
        private readonly IPlatformSettingsRepository _repo;

        public GetPublicRegistrationStatusQueryHandler(IPlatformSettingsRepository repo) => _repo = repo;

        public async Task<bool> Handle(GetPublicRegistrationStatusQuery request, CancellationToken cancellationToken)
        {
            var settings = await _repo.GetAsync(cancellationToken);
            return settings.IsPublicRegistrationEnabled;
        }
    }
}
