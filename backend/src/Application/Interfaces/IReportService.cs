using Application.DTOs.Reports;

namespace Application.Interfaces;

public interface IReportService
{
    IReadOnlyList<RegisterSummaryDto> GetRegisters();

    Task<RegisterDto> BuildAsync(RegisterQuery query, CancellationToken cancellationToken);
}
