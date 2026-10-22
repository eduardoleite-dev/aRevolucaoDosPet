using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class PregnancyService : IPregnancyService
{
    private readonly IMongoCollection<Pregnancy> _pregnancies;
    private readonly IMongoCollection<Pet> _pets;

    public PregnancyService(IMongoDatabase database)
    {
        _pregnancies = database.GetCollection<Pregnancy>("Pregnancies");
        _pets = database.GetCollection<Pet>("Pets");
    }

    public async Task<PregnancyResponseDto> CreateAsync(
        CreatePregnancyRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        PetRecordHelper.EnsureObjectId(request.PetId, "petId");
        await RequireFemalePetAsync(request.PetId, currentUserId, isAdministrator, cancellationToken);

        var now = DateTime.UtcNow;
        var pregnancy = new Pregnancy { PetId = request.PetId, CreatedAt = now };
        await ApplyAsync(pregnancy, request, now, cancellationToken);
        await _pregnancies.InsertOneAsync(pregnancy, cancellationToken: cancellationToken);
        return ToResponse(pregnancy);
    }

    public async Task<PagedResult<PregnancyResponseDto>> SearchAsync(
        PregnancySearchFilter filter,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        await PetRecordHelper.AuthorizeSearchAsync(_pets, filter.PetId, currentUserId, isAdministrator, cancellationToken);
        var filters = new List<FilterDefinition<Pregnancy>>();
        var builder = Builders<Pregnancy>.Filter;

        PetRecordHelper.AddObjectId(filters, "_id", filter.Id);
        PetRecordHelper.AddObjectId(filters, "pet_id", filter.PetId);
        PetRecordHelper.AddObjectId(filters, "father_pet_id", filter.FatherPetId);
        PetRecordHelper.AddText(filters, "father_name", filter.FatherName);
        PetRecordHelper.AddText(filters, "notes", filter.Notes);

        if (filter.Status.HasValue) filters.Add(builder.Eq(pregnancy => pregnancy.Status, filter.Status.Value));
        if (filter.MinPuppiesCount.HasValue) filters.Add(builder.Gte(pregnancy => pregnancy.PuppiesCount, filter.MinPuppiesCount.Value));
        if (filter.MaxPuppiesCount.HasValue) filters.Add(builder.Lte(pregnancy => pregnancy.PuppiesCount, filter.MaxPuppiesCount.Value));
        if (filter.HasUltrasound.HasValue)
        {
            filters.Add(filter.HasUltrasound.Value
                ? builder.Ne(pregnancy => pregnancy.UltrasoundUrl, null)
                : builder.Eq(pregnancy => pregnancy.UltrasoundUrl, null));
        }

        PetRecordHelper.AddDateRange(filters, "started_at", filter.StartedAtFrom, filter.StartedAtTo);
        PetRecordHelper.AddDateRange(filters, "expected_birth_at", filter.ExpectedBirthAtFrom, filter.ExpectedBirthAtTo);
        PetRecordHelper.AddDateRange(filters, "actual_birth_at", filter.ActualBirthAtFrom, filter.ActualBirthAtTo);
        PetRecordHelper.AddDateRange(filters, "created_at", filter.CreatedAtFrom, filter.CreatedAtTo);
        PetRecordHelper.AddDateRange(filters, "updated_at", filter.UpdatedAtFrom, filter.UpdatedAtTo);

        var query = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var totalCount = await _pregnancies.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var pregnancies = await _pregnancies.Find(query)
            .SortByDescending(pregnancy => pregnancy.StartedAt)
            .ThenBy(pregnancy => pregnancy.Id)
            .Skip(skip)
            .Limit(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PregnancyResponseDto>(
            pregnancies.Select(ToResponse).ToList(),
            totalCount,
            filter.PageNumber,
            filter.PageSize);
    }

    public async Task<PregnancyResponseDto?> GetByIdAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var pregnancy = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        return pregnancy is null ? null : ToResponse(pregnancy);
    }

    public async Task<PregnancyResponseDto?> UpdateAsync(
        string id,
        PregnancyWriteRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var pregnancy = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (pregnancy is null) return null;
        await RequireFemalePetAsync(pregnancy.PetId, currentUserId, isAdministrator, cancellationToken);

        await ApplyAsync(pregnancy, request, DateTime.UtcNow, cancellationToken);
        await _pregnancies.ReplaceOneAsync(item => item.Id == id, pregnancy, cancellationToken: cancellationToken);
        return ToResponse(pregnancy);
    }

    public async Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var pregnancy = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (pregnancy is null) return false;

        var result = await _pregnancies.DeleteOneAsync(item => item.Id == id, cancellationToken);
        return result.DeletedCount == 1;
    }

    private async Task<Pet> RequireFemalePetAsync(
        string petId,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        var pet = await _pets.Find(item => item.Id == petId && item.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (pet is null) throw new PregnancyPetNotFoundException();
        if (!isAdministrator && !PetRecordHelper.CanEdit(pet, currentUserId)) throw new RecordPermissionException();
        if (pet.Sex != Sex.FEMALE) throw new PregnancyPetSexException();
        return pet;
    }

    private async Task<Pet?> FindFatherAsync(string? fatherPetId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fatherPetId)) return null;
        PetRecordHelper.EnsureObjectId(fatherPetId, "fatherPetId");
        var father = await _pets.Find(item => item.Id == fatherPetId && item.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (father is null) throw new PregnancyPetNotFoundException();
        if (father.Sex != Sex.MALE) throw new ArgumentException("O pet cadastrado como pai deve ser macho.");
        return father;
    }

    private async Task<Pregnancy?> FindAuthorizedAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        var pregnancy = await _pregnancies.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (pregnancy is null) return null;

        return await PetRecordHelper.AuthorizeExistingAsync(_pets, pregnancy.PetId, currentUserId, isAdministrator, cancellationToken)
            ? pregnancy
            : null;
    }

    private async Task ApplyAsync(
        Pregnancy pregnancy,
        PregnancyWriteRequest request,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var fatherPetId = PetRecordHelper.NormalizeOptional(request.FatherPetId);
        var fatherName = PetRecordHelper.NormalizeOptional(request.FatherName);
        if (fatherPetId is not null && fatherName is not null)
        {
            throw new ArgumentException("Informe fatherPetId ou fatherName, não ambos.");
        }

        if (fatherPetId is not null)
        {
            var father = await FindFatherAsync(fatherPetId, cancellationToken);
            if (father?.Id == pregnancy.PetId) throw new ArgumentException("O pet não pode ser o próprio pai.");
        }

        var startedAt = PetRecordHelper.NormalizeUtc(request.StartedAt);
        var expectedBirthAt = PetRecordHelper.NormalizeUtc(request.ExpectedBirthAt);
        var actualBirthAt = PetRecordHelper.NormalizeUtc(request.ActualBirthAt);
        if (startedAt > now.AddDays(1)) throw new ArgumentException("startedAt não pode estar no futuro.");
        if (expectedBirthAt.HasValue && expectedBirthAt < startedAt)
        {
            throw new ArgumentException("expectedBirthAt não pode ser anterior a startedAt.");
        }
        if (actualBirthAt.HasValue && actualBirthAt < startedAt)
        {
            throw new ArgumentException("actualBirthAt não pode ser anterior a startedAt.");
        }
        if (actualBirthAt > now.AddDays(1)) throw new ArgumentException("actualBirthAt não pode estar no futuro.");
        if (request.Status == PregnancyStatus.COMPLETED && !actualBirthAt.HasValue)
        {
            throw new ArgumentException("Gestação concluída exige actualBirthAt.");
        }
        if (request.Status == PregnancyStatus.ONGOING && actualBirthAt.HasValue)
        {
            throw new ArgumentException("Gestação em andamento não pode ter actualBirthAt.");
        }

        pregnancy.FatherPetId = fatherPetId;
        pregnancy.FatherName = fatherName;
        pregnancy.StartedAt = startedAt;
        pregnancy.ExpectedBirthAt = expectedBirthAt;
        pregnancy.ActualBirthAt = actualBirthAt;
        pregnancy.Status = request.Status;
        pregnancy.UltrasoundUrl = PetRecordHelper.NormalizeOptional(request.UltrasoundUrl);
        pregnancy.PuppiesCount = request.PuppiesCount;
        pregnancy.Notes = PetRecordHelper.NormalizeOptional(request.Notes);
        pregnancy.UpdatedAt = now;
    }

    private static PregnancyResponseDto ToResponse(Pregnancy pregnancy) => new(
        pregnancy.Id,
        pregnancy.PetId,
        pregnancy.FatherPetId,
        pregnancy.FatherName,
        pregnancy.StartedAt,
        pregnancy.ExpectedBirthAt,
        pregnancy.ActualBirthAt,
        pregnancy.Status,
        pregnancy.UltrasoundUrl,
        pregnancy.PuppiesCount,
        pregnancy.Notes,
        pregnancy.CreatedAt,
        pregnancy.UpdatedAt);
}
