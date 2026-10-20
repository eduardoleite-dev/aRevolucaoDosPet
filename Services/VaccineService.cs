using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class VaccineService : IVaccineService
{
    private readonly IMongoCollection<Vaccine> _vaccines;
    private readonly IMongoCollection<Pet> _pets;

    public VaccineService(IMongoDatabase database)
    {
        _vaccines = database.GetCollection<Vaccine>("Vaccines");
        _pets = database.GetCollection<Pet>("Pets");
    }

    public async Task<VaccineResponseDto> CreateAsync(
        CreateVaccineRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        PetRecordHelper.EnsureObjectId(request.PetId, "petId");
        await PetRecordHelper.RequireEditablePetAsync(_pets, request.PetId, currentUserId, isAdministrator, cancellationToken);

        var now = DateTime.UtcNow;
        var vaccine = new Vaccine { PetId = request.PetId, CreatedAt = now };
        Apply(vaccine, request, now);

        await _vaccines.InsertOneAsync(vaccine, cancellationToken: cancellationToken);
        return ToResponse(vaccine);
    }

    public async Task<PagedResult<VaccineResponseDto>> SearchAsync(
        VaccineSearchFilter filter,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        await PetRecordHelper.AuthorizeSearchAsync(_pets, filter.PetId, currentUserId, isAdministrator, cancellationToken);

        var filters = new List<FilterDefinition<Vaccine>>();
        var builder = Builders<Vaccine>.Filter;

        PetRecordHelper.AddObjectId(filters, "_id", filter.Id);
        PetRecordHelper.AddObjectId(filters, "pet_id", filter.PetId);
        PetRecordHelper.AddText(filters, "name", filter.Name);
        PetRecordHelper.AddText(filters, "laboratory", filter.Laboratory);
        PetRecordHelper.AddText(filters, "batch_number", filter.BatchNumber);
        PetRecordHelper.AddText(filters, "veterinarian_name", filter.VeterinarianName);
        PetRecordHelper.AddText(filters, "veterinarian_crmv", filter.VeterinarianCrmv);
        PetRecordHelper.AddText(filters, "clinic_name", filter.ClinicName);

        if (filter.MinDoseNumber.HasValue) filters.Add(builder.Gte(vaccine => vaccine.DoseNumber, filter.MinDoseNumber.Value));
        if (filter.MaxDoseNumber.HasValue) filters.Add(builder.Lte(vaccine => vaccine.DoseNumber, filter.MaxDoseNumber.Value));
        if (filter.Status.HasValue) filters.Add(builder.Eq(vaccine => vaccine.Status, filter.Status.Value));
        if (filter.HasCertificate.HasValue)
        {
            filters.Add(filter.HasCertificate.Value
                ? builder.Ne(vaccine => vaccine.CertificateUrl, null)
                : builder.Eq(vaccine => vaccine.CertificateUrl, null));
        }

        PetRecordHelper.AddDateRange(filters, "applied_at", filter.AppliedAtFrom, filter.AppliedAtTo);
        PetRecordHelper.AddDateRange(filters, "next_dose_at", filter.NextDoseAtFrom, filter.NextDoseAtTo);
        PetRecordHelper.AddDateRange(filters, "created_at", filter.CreatedAtFrom, filter.CreatedAtTo);
        PetRecordHelper.AddDateRange(filters, "updated_at", filter.UpdatedAtFrom, filter.UpdatedAtTo);

        var query = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var totalCount = await _vaccines.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var vaccines = await _vaccines.Find(query)
            .SortByDescending(vaccine => vaccine.CreatedAt)
            .ThenBy(vaccine => vaccine.Id)
            .Skip(skip)
            .Limit(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<VaccineResponseDto>(
            vaccines.Select(ToResponse).ToList(),
            totalCount,
            filter.PageNumber,
            filter.PageSize);
    }

    public async Task<VaccineResponseDto?> GetByIdAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var vaccine = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        return vaccine is null ? null : ToResponse(vaccine);
    }

    public async Task<VaccineResponseDto?> UpdateAsync(
        string id,
        VaccineWriteRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var vaccine = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (vaccine is null) return null;

        Apply(vaccine, request, DateTime.UtcNow);
        await _vaccines.ReplaceOneAsync(item => item.Id == id, vaccine, cancellationToken: cancellationToken);
        return ToResponse(vaccine);
    }

    public async Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var vaccine = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (vaccine is null) return false;

        var result = await _vaccines.DeleteOneAsync(item => item.Id == id, cancellationToken);
        return result.DeletedCount == 1;
    }

    private async Task<Vaccine?> FindAuthorizedAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _)) return null;

        var vaccine = await _vaccines.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (vaccine is null) return null;

        return await PetRecordHelper.AuthorizeExistingAsync(_pets, vaccine.PetId, currentUserId, isAdministrator, cancellationToken)
            ? vaccine
            : null;
    }

    private static void Apply(Vaccine vaccine, VaccineWriteRequest request, DateTime now)
    {
        var appliedAt = PetRecordHelper.NormalizeUtc(request.AppliedAt);
        var nextDoseAt = PetRecordHelper.NormalizeUtc(request.NextDoseAt);

        // Tolerância de um dia para diferenças de fuso do cliente.
        if (appliedAt > now.AddDays(1)) throw new ArgumentException("A data de aplicação não pode estar no futuro.");
        if (appliedAt.HasValue && nextDoseAt.HasValue && nextDoseAt < appliedAt)
        {
            throw new ArgumentException("A próxima dose não pode ser anterior à data de aplicação.");
        }

        vaccine.Name = request.Name.Trim();
        vaccine.Laboratory = PetRecordHelper.NormalizeOptional(request.Laboratory);
        vaccine.BatchNumber = PetRecordHelper.NormalizeOptional(request.BatchNumber);
        vaccine.DoseNumber = request.DoseNumber;
        vaccine.AppliedAt = appliedAt;
        vaccine.NextDoseAt = nextDoseAt;
        vaccine.VeterinarianName = PetRecordHelper.NormalizeOptional(request.VeterinarianName);
        vaccine.VeterinarianCrmv = PetRecordHelper.NormalizeOptional(request.VeterinarianCrmv);
        vaccine.ClinicName = PetRecordHelper.NormalizeOptional(request.ClinicName);
        vaccine.CertificateUrl = PetRecordHelper.NormalizeOptional(request.CertificateUrl);
        vaccine.Status = request.Status;
        vaccine.UpdatedAt = now;
    }

    private static VaccineResponseDto ToResponse(Vaccine vaccine) => new(
        vaccine.Id,
        vaccine.PetId,
        vaccine.Name,
        vaccine.Laboratory,
        vaccine.BatchNumber,
        vaccine.DoseNumber,
        vaccine.AppliedAt,
        vaccine.NextDoseAt,
        vaccine.VeterinarianName,
        vaccine.VeterinarianCrmv,
        vaccine.ClinicName,
        vaccine.CertificateUrl,
        vaccine.Status,
        vaccine.CreatedAt,
        vaccine.UpdatedAt);
}
