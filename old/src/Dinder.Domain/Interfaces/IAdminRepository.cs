using Dinder.Domain.Entities;
using Dinder.Domain.Enums;

namespace Dinder.Domain.Interfaces;

public interface IAdminRepository
{
    // User search
    Task<List<(User User, DateTime? LastLogin, int ReportCount)>> SearchUsersAsync(
        string query, int skip, int take, CancellationToken cancellationToken = default);
    Task<(User User, DateTime? LastLogin, int ReportCount)?> GetUserDetailsAsync(Guid userId, CancellationToken cancellationToken = default);

    // Audit log (append-only)
    void AddAuditLog(AdminAuditLog entry);

    // Prompt catalog
    Task<List<PromptCatalog>> GetPromptCatalogAsync(CancellationToken cancellationToken = default);
    Task<List<PromptCatalog>> GetEnabledPromptCatalogAsync(CancellationToken cancellationToken = default);
    Task<PromptCatalog?> GetPromptCatalogByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void AddPrompt(PromptCatalog prompt);
    void UpdatePrompt(PromptCatalog prompt);

    // Icebreaker library
    Task<List<IcebreakerLibrary>> GetIcebreakerLibraryAsync(CancellationToken cancellationToken = default);
    Task<List<IcebreakerLibrary>> GetEnabledIcebreakerLibraryAsync(CancellationToken cancellationToken = default);
    Task<IcebreakerLibrary?> GetIcebreakerLibraryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void AddIcebreaker(IcebreakerLibrary icebreaker);
    void UpdateIcebreaker(IcebreakerLibrary icebreaker);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
