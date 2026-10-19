using System.Text.RegularExpressions;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class PetService : IPetService
{
    private readonly IMongoCollection<Pet> _pets;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<PetFollower> _petFollowers;

    public PetService(IMongoDatabase database)
    {
        _pets = database.GetCollection<Pet>("Pets");
        _users = database.GetCollection<User>("Users");
        _petFollowers = database.GetCollection<PetFollower>("PetFollowers");
    }

    public async Task<PetResponseDto> CreateAsync(
        CreatePetRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var ownerId = isAdministrator ? request.OwnerId : currentUserId;
        EnsureObjectId(ownerId, "ownerId");
        await EnsureActiveUserAsync(ownerId!, cancellationToken);
        var coTutorIds = await ValidateCoTutorIdsAsync(request.CoTutorIds, ownerId!, cancellationToken);
        ValidateDates(request.BirthDate, request.AdoptionDate);

        var now = DateTime.UtcNow;
        var pet = new Pet
        {
            OwnerId = ownerId!,
            CoTutorIds = coTutorIds,
            Name = request.Name.Trim(),
            Username = NormalizeUsername(request.Username),
            Bio = NormalizeOptional(request.Bio),
            AvatarUrl = NormalizeOptional(request.AvatarUrl),
            CoverUrl = NormalizeOptional(request.CoverUrl),
            Species = request.Species,
            Breed = NormalizeOptional(request.Breed),
            Sex = request.Sex,
            Color = NormalizeOptional(request.Color),
            BirthDate = request.BirthDate,
            AdoptionDate = request.AdoptionDate,
            WeightKg = request.WeightKg,
            Size = request.Size,
            Microchip = NormalizeOptional(request.Microchip),
            Pedigree = NormalizeOptional(request.Pedigree),
            IsNeutered = request.IsNeutered,
            IsBreeder = request.IsBreeder,
            Allergies = NormalizeList(request.Allergies),
            ChronicConditions = NormalizeList(request.ChronicConditions),
            ContinuousMedications = MapMedications(request.ContinuousMedications),
            VetName = NormalizeOptional(request.VetName),
            VetPhone = NormalizeOptional(request.VetPhone),
            EmergencyContactName = NormalizeOptional(request.EmergencyContactName),
            EmergencyContactPhone = NormalizeOptional(request.EmergencyContactPhone),
            HealthPlan = NormalizeOptional(request.HealthPlan),
            HealthPlanNumber = NormalizeOptional(request.HealthPlanNumber),
            Status = request.Status,
            Visibility = request.Visibility,
            Location = MapLocation(request.Location),
            CreatedAt = now,
            UpdatedAt = now
        };

        try
        {
            await _pets.InsertOneAsync(pet, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DuplicatePetUsernameException();
        }

        return ToResponse(pet);
    }

    public async Task<PagedResult<PetResponseDto>> SearchAsync(
        PetSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Pet>>();
        var builder = Builders<Pet>.Filter;

        AddTextFilter(filters, "name", filter.Name);
        AddTextFilter(filters, "username", filter.Username);
        AddTextFilter(filters, "bio", filter.Bio);
        AddTextFilter(filters, "avatar_url", filter.AvatarUrl);
        AddTextFilter(filters, "cover_url", filter.CoverUrl);
        AddTextFilter(filters, "breed", filter.Breed);
        AddTextFilter(filters, "color", filter.Color);
        AddTextFilter(filters, "microchip", filter.Microchip);
        AddTextFilter(filters, "pedigree", filter.Pedigree);
        AddTextFilter(filters, "allergies", filter.Allergy);
        AddTextFilter(filters, "chronic_conditions", filter.ChronicCondition);
        AddTextFilter(filters, "continuous_medications.name", filter.MedicationName);
        AddTextFilter(filters, "vet_name", filter.VetName);
        AddTextFilter(filters, "vet_phone", filter.VetPhone);
        AddTextFilter(filters, "emergency_contact_name", filter.EmergencyContactName);
        AddTextFilter(filters, "emergency_contact_phone", filter.EmergencyContactPhone);
        AddTextFilter(filters, "health_plan", filter.HealthPlan);
        AddTextFilter(filters, "health_plan_number", filter.HealthPlanNumber);

        AddObjectIdFilter(filters, "_id", filter.Id);
        AddObjectIdFilter(filters, "owner_id", filter.OwnerId);
        AddObjectIdFilter(filters, "co_tutor_ids", filter.CoTutorId);

        if (filter.Species.HasValue) filters.Add(builder.Eq(pet => pet.Species, filter.Species.Value));
        if (filter.Sex.HasValue) filters.Add(builder.Eq(pet => pet.Sex, filter.Sex.Value));
        if (filter.Size.HasValue) filters.Add(builder.Eq(pet => pet.Size, filter.Size.Value));
        if (filter.Status.HasValue) filters.Add(builder.Eq(pet => pet.Status, filter.Status.Value));
        if (filter.Visibility.HasValue) filters.Add(builder.Eq(pet => pet.Visibility, filter.Visibility.Value));
        if (filter.IsNeutered.HasValue) filters.Add(builder.Eq(pet => pet.IsNeutered, filter.IsNeutered.Value));
        if (filter.IsBreeder.HasValue) filters.Add(builder.Eq(pet => pet.IsBreeder, filter.IsBreeder.Value));
        if (filter.HasLocation.HasValue)
        {
            filters.Add(filter.HasLocation.Value
                ? builder.Ne(pet => pet.Location, null)
                : builder.Eq(pet => pet.Location, null));
        }

        if (filter.MinWeightKg.HasValue) filters.Add(builder.Gte(pet => pet.WeightKg, filter.MinWeightKg.Value));
        if (filter.MaxWeightKg.HasValue) filters.Add(builder.Lte(pet => pet.WeightKg, filter.MaxWeightKg.Value));
        if (filter.MinFollowersCount.HasValue) filters.Add(builder.Gte(pet => pet.FollowersCount, filter.MinFollowersCount.Value));
        if (filter.MaxFollowersCount.HasValue) filters.Add(builder.Lte(pet => pet.FollowersCount, filter.MaxFollowersCount.Value));
        if (filter.MinFollowingCount.HasValue) filters.Add(builder.Gte(pet => pet.FollowingCount, filter.MinFollowingCount.Value));
        if (filter.MaxFollowingCount.HasValue) filters.Add(builder.Lte(pet => pet.FollowingCount, filter.MaxFollowingCount.Value));
        if (filter.MinPostsCount.HasValue) filters.Add(builder.Gte(pet => pet.PostsCount, filter.MinPostsCount.Value));
        if (filter.MaxPostsCount.HasValue) filters.Add(builder.Lte(pet => pet.PostsCount, filter.MaxPostsCount.Value));

        if (filter.Longitude.HasValue != filter.Latitude.HasValue)
        {
            throw new ArgumentException("Informe longitude e latitude juntas.");
        }

        if (filter.RadiusMeters.HasValue && !filter.Longitude.HasValue)
        {
            throw new ArgumentException("Informe longitude e latitude para filtrar por proximidade.");
        }

        if (filter.RadiusMeters.HasValue)
        {
            var radiusInRadians = filter.RadiusMeters.Value / 6_378_100d;
            filters.Add(new BsonDocumentFilterDefinition<Pet>(new BsonDocument("location", new BsonDocument(
                "$geoWithin",
                new BsonDocument("$centerSphere", new BsonArray
                {
                    new BsonArray { filter.Longitude!.Value, filter.Latitude!.Value },
                    radiusInRadians
                })))));
        }
        else
        {
            if (filter.Longitude.HasValue) filters.Add(builder.Eq("location.coordinates.0", filter.Longitude.Value));
            if (filter.Latitude.HasValue) filters.Add(builder.Eq("location.coordinates.1", filter.Latitude.Value));
        }

        AddDateRange(filters, "birth_date", filter.BirthDateFrom, filter.BirthDateTo);
        AddDateRange(filters, "adoption_date", filter.AdoptionDateFrom, filter.AdoptionDateTo);
        AddDateRange(filters, "created_at", filter.CreatedAtFrom, filter.CreatedAtTo);
        AddDateRange(filters, "updated_at", filter.UpdatedAtFrom, filter.UpdatedAtTo);
        AddDateRange(filters, "deleted_at", filter.DeletedAtFrom, filter.DeletedAtTo);

        if (!filter.IncludeDeleted)
        {
            filters.Add(builder.Eq(pet => pet.DeletedAt, null));
        }

        var query = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var totalCount = await _pets.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var pets = await _pets.Find(query)
            .SortByDescending(pet => pet.CreatedAt)
            .ThenBy(pet => pet.Id)
            .Skip(skip)
            .Limit(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PetResponseDto>(pets.Select(ToResponse).ToList(), totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<PetResponseDto?> GetByIdAsync(
        string id,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _)) return null;

        var pet = await _pets.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (pet is null || (pet.DeletedAt.HasValue && !isAdministrator)) return null;
        if (isAdministrator || CanEdit(pet, viewerUserId)) return ToResponse(pet);
        if (pet.Visibility == PetProfileVisibility.PUBLIC) return ToResponse(pet);

        if (pet.Visibility == PetProfileVisibility.FOLLOWERS_ONLY && ObjectId.TryParse(viewerUserId, out _))
        {
            var isFollower = await _petFollowers.Find(follower =>
                    follower.PetId == id && follower.FollowerUserId == viewerUserId)
                .AnyAsync(cancellationToken);
            if (isFollower) return ToResponse(pet);
        }

        return null;
    }

    public async Task<PetResponseDto?> UpdateAsync(
        string id,
        PetWriteRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _)) return null;

        var existingPet = await _pets.Find(pet => pet.Id == id && pet.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingPet is null) return null;
        if (!CanEdit(existingPet, currentUserId) && !isAdministrator) throw new PetPermissionException();
        if (request.CoTutorIds is not null && !isAdministrator && existingPet.OwnerId != currentUserId)
        {
            throw new PetPermissionException();
        }

        ValidateDates(request.BirthDate, request.AdoptionDate);
        var coTutorIds = request.CoTutorIds is null
            ? existingPet.CoTutorIds
            : await ValidateCoTutorIdsAsync(request.CoTutorIds, existingPet.OwnerId, cancellationToken);

        var update = Builders<Pet>.Update
            .Set(pet => pet.CoTutorIds, coTutorIds)
            .Set(pet => pet.Name, request.Name.Trim())
            .Set(pet => pet.Username, NormalizeUsername(request.Username))
            .Set(pet => pet.Bio, NormalizeOptional(request.Bio))
            .Set(pet => pet.AvatarUrl, NormalizeOptional(request.AvatarUrl))
            .Set(pet => pet.CoverUrl, NormalizeOptional(request.CoverUrl))
            .Set(pet => pet.Species, request.Species)
            .Set(pet => pet.Breed, NormalizeOptional(request.Breed))
            .Set(pet => pet.Sex, request.Sex)
            .Set(pet => pet.Color, NormalizeOptional(request.Color))
            .Set(pet => pet.BirthDate, request.BirthDate)
            .Set(pet => pet.AdoptionDate, request.AdoptionDate)
            .Set(pet => pet.WeightKg, request.WeightKg)
            .Set(pet => pet.Size, request.Size)
            .Set(pet => pet.Microchip, NormalizeOptional(request.Microchip))
            .Set(pet => pet.Pedigree, NormalizeOptional(request.Pedigree))
            .Set(pet => pet.IsNeutered, request.IsNeutered)
            .Set(pet => pet.IsBreeder, request.IsBreeder)
            .Set(pet => pet.Allergies, NormalizeList(request.Allergies))
            .Set(pet => pet.ChronicConditions, NormalizeList(request.ChronicConditions))
            .Set(pet => pet.ContinuousMedications, MapMedications(request.ContinuousMedications))
            .Set(pet => pet.VetName, NormalizeOptional(request.VetName))
            .Set(pet => pet.VetPhone, NormalizeOptional(request.VetPhone))
            .Set(pet => pet.EmergencyContactName, NormalizeOptional(request.EmergencyContactName))
            .Set(pet => pet.EmergencyContactPhone, NormalizeOptional(request.EmergencyContactPhone))
            .Set(pet => pet.HealthPlan, NormalizeOptional(request.HealthPlan))
            .Set(pet => pet.HealthPlanNumber, NormalizeOptional(request.HealthPlanNumber))
            .Set(pet => pet.Status, request.Status)
            .Set(pet => pet.Visibility, request.Visibility)
            .Set(pet => pet.Location, MapLocation(request.Location))
            .Set(pet => pet.UpdatedAt, DateTime.UtcNow);

        try
        {
            var updatedPet = await _pets.FindOneAndUpdateAsync(
                pet => pet.Id == id && pet.DeletedAt == null,
                update,
                new FindOneAndUpdateOptions<Pet> { ReturnDocument = ReturnDocument.After },
                cancellationToken);
            return updatedPet is null ? null : ToResponse(updatedPet);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DuplicatePetUsernameException();
        }
    }

    public async Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _)) return false;
        var pet = await _pets.Find(item => item.Id == id && item.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);
        if (pet is null) return false;
        if (!isAdministrator && pet.OwnerId != currentUserId) throw new PetPermissionException();

        var now = DateTime.UtcNow;
        var update = Builders<Pet>.Update
            .Set(item => item.DeletedAt, now)
            .Set(item => item.UpdatedAt, now);
        await _pets.UpdateOneAsync(item => item.Id == id && item.DeletedAt == null, update, cancellationToken: cancellationToken);
        return true;
    }

    public async Task<bool> CanEditAsync(
        string id,
        string? userId,
        bool isAdministrator,
        bool ownerOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (isAdministrator) return true;
        if (!ObjectId.TryParse(id, out _)) return false;
        var pet = await _pets.Find(item => item.Id == id && item.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);
        return pet is not null && (pet.OwnerId == userId || (!ownerOnly && CanEdit(pet, userId)));
    }

    private async Task EnsureActiveUserAsync(string userId, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(userId, out _)) throw new ArgumentException("O ownerId informado não é um ObjectId válido.");
        var exists = await _users.Find(user => user.Id == userId && user.Status == UserStatus.ACTIVE).AnyAsync(cancellationToken);
        if (!exists) throw new PetOwnerNotFoundException();
    }

    private async Task<List<ObjectId>> ValidateCoTutorIdsAsync(
        IReadOnlyCollection<string>? coTutorIds,
        string ownerId,
        CancellationToken cancellationToken)
    {
        if (coTutorIds is null || coTutorIds.Count == 0) return [];
        var ids = new List<ObjectId>();
        foreach (var id in coTutorIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!ObjectId.TryParse(id, out var objectId)) throw new ArgumentException("Um co-tutor possui um id inválido.");
            if (id == ownerId) throw new ArgumentException("O tutor principal não pode ser repetido como co-tutor.");
            ids.Add(objectId);
        }

        var stringIds = ids.Select(id => id.ToString()).ToList();
        var activeCount = await _users.CountDocumentsAsync(
            user => stringIds.Contains(user.Id) && user.Status == UserStatus.ACTIVE,
            cancellationToken: cancellationToken);
        if (activeCount != ids.Count) throw new ArgumentException("Todos os co-tutores devem ser usuários ativos existentes.");
        return ids;
    }

    private static bool CanEdit(Pet pet, string? userId) =>
        userId is not null &&
        (pet.OwnerId == userId ||
         (ObjectId.TryParse(userId, out var objectId) && pet.CoTutorIds.Contains(objectId)));

    private static void EnsureObjectId(string? id, string field)
    {
        if (!ObjectId.TryParse(id, out _)) throw new ArgumentException($"O {field} informado não é um ObjectId válido.");
    }

    private static void ValidateDates(DateTime? birthDate, DateTime? adoptionDate)
    {
        if (birthDate > DateTime.UtcNow) throw new ArgumentException("A data de nascimento não pode estar no futuro.");
        if (birthDate.HasValue && adoptionDate.HasValue && adoptionDate < birthDate)
        {
            throw new ArgumentException("A data de adoção não pode ser anterior ao nascimento.");
        }
    }

    private static UserLocation? MapLocation(UserLocationRequest? location)
    {
        if (location is null) return null;
        var coordinates = location.Coordinates;
        if (coordinates.Length != 2 || !double.IsFinite(coordinates[0]) || !double.IsFinite(coordinates[1]) ||
            coordinates[0] is < -180 or > 180 || coordinates[1] is < -90 or > 90)
        {
            throw new ArgumentException("A localização deve conter [longitude, latitude] em limites válidos.");
        }

        return new UserLocation { Coordinates = coordinates };
    }

    private static List<PetMedication> MapMedications(IEnumerable<PetMedicationDto>? medications) =>
        medications?.Select(medication => new PetMedication
        {
            Name = medication.Name.Trim(),
            Dosage = medication.Dosage.Trim(),
            Frequency = medication.Frequency.Trim(),
            StartedAt = medication.StartedAt
        }).ToList() ?? [];

    private static List<string> NormalizeList(IEnumerable<string>? values) =>
        values?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [];

    private static string? NormalizeUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;
        var normalized = username.Trim().TrimStart('@').ToLowerInvariant();
        return normalized.Length == 0 ? null : normalized;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AddTextFilter(List<FilterDefinition<Pet>> filters, string field, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            filters.Add(Builders<Pet>.Filter.Regex(field, new BsonRegularExpression(Regex.Escape(value.Trim()), "i")));
        }
    }

    private static void AddObjectIdFilter(List<FilterDefinition<Pet>> filters, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!ObjectId.TryParse(value, out var objectId)) throw new ArgumentException($"O filtro {field} precisa ser um ObjectId válido.");
        filters.Add(new BsonDocumentFilterDefinition<Pet>(new BsonDocument(field, objectId)));
    }

    private static void AddDateRange(List<FilterDefinition<Pet>> filters, string field, DateTime? from, DateTime? to)
    {
        if (from.HasValue) filters.Add(Builders<Pet>.Filter.Gte(field, from.Value));
        if (to.HasValue) filters.Add(Builders<Pet>.Filter.Lte(field, to.Value));
    }

    private static PetResponseDto ToResponse(Pet pet) => new(
        pet.Id,
        pet.OwnerId,
        pet.CoTutorIds.Select(id => id.ToString()).ToList(),
        pet.Name,
        pet.Username,
        pet.Bio,
        pet.AvatarUrl,
        pet.CoverUrl,
        pet.Species,
        pet.Breed,
        pet.Sex,
        pet.Color,
        pet.BirthDate,
        pet.AdoptionDate,
        pet.WeightKg,
        pet.Size,
        pet.Microchip,
        pet.Pedigree,
        pet.IsNeutered,
        pet.IsBreeder,
        pet.Allergies,
        pet.ChronicConditions,
        pet.ContinuousMedications.Select(medication => new PetMedicationDto
        {
            Name = medication.Name,
            Dosage = medication.Dosage,
            Frequency = medication.Frequency,
            StartedAt = medication.StartedAt
        }).ToList(),
        pet.VetName,
        pet.VetPhone,
        pet.EmergencyContactName,
        pet.EmergencyContactPhone,
        pet.HealthPlan,
        pet.HealthPlanNumber,
        pet.Status,
        pet.Visibility,
        pet.Location,
        pet.FollowersCount,
        pet.FollowingCount,
        pet.PostsCount,
        pet.CreatedAt,
        pet.UpdatedAt,
        pet.DeletedAt);
}