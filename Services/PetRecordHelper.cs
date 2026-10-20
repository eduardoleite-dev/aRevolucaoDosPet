using System.Text.RegularExpressions;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

// Regras comuns aos registros de saúde ligados a um pet (vacinas, consultas, pesos).
internal static class PetRecordHelper
{
    public static void EnsureObjectId(string? id, string field)
    {
        if (!ObjectId.TryParse(id, out _)) throw new ArgumentException($"O {field} informado não é um ObjectId válido.");
    }

    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // Datas sem fuso são tratadas como UTC para não variar com a máquina do servidor.
    public static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    public static DateTime? NormalizeUtc(DateTime? value) =>
        value.HasValue ? NormalizeUtc(value.Value) : null;

    public static bool CanEdit(Pet pet, string? userId) =>
        userId is not null &&
        (pet.OwnerId == userId ||
         (ObjectId.TryParse(userId, out var objectId) && pet.CoTutorIds.Contains(objectId)));

    // Quem pode ver o perfil do pet: público, seguidores ou apenas tutores, conforme a visibilidade.
    public static async Task<bool> CanViewProfileAsync(
        Pet pet,
        IMongoCollection<PetFollower> followers,
        string? viewerUserId,
        CancellationToken cancellationToken)
    {
        if (pet.Visibility == PetProfileVisibility.PUBLIC) return true;
        if (!ObjectId.TryParse(viewerUserId, out _)) return false;
        if (CanEdit(pet, viewerUserId)) return true;

        return pet.Visibility == PetProfileVisibility.FOLLOWERS_ONLY &&
               await followers
                   .Find(follower => follower.PetId == pet.Id && follower.FollowerUserId == viewerUserId)
                   .AnyAsync(cancellationToken);
    }

    public static async Task<Pet> RequireEditablePetAsync(
        IMongoCollection<Pet> pets,
        string petId,
        string? userId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        var pet = await pets.Find(item => item.Id == petId && item.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (pet is null) throw new RecordPetNotFoundException();
        if (!isAdministrator && !CanEdit(pet, userId)) throw new RecordPermissionException();
        return pet;
    }

    // Retorna false quando o pet do registro não existe mais.
    public static async Task<bool> AuthorizeExistingAsync(
        IMongoCollection<Pet> pets,
        string petId,
        string? userId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        var pet = await pets.Find(item => item.Id == petId && item.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (pet is null) return false;
        if (!isAdministrator && !CanEdit(pet, userId)) throw new RecordPermissionException();
        return true;
    }

    public static async Task AuthorizeSearchAsync(
        IMongoCollection<Pet> pets,
        string? petId,
        string? userId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(petId))
        {
            if (!isAdministrator) throw new ArgumentException("Informe o petId.");
            return;
        }

        EnsureObjectId(petId, "petId");
        if (!isAdministrator)
        {
            await RequireEditablePetAsync(pets, petId, userId, false, cancellationToken);
        }
    }

    public static void AddText<T>(List<FilterDefinition<T>> filters, string field, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            filters.Add(Builders<T>.Filter.Regex(field, new BsonRegularExpression(Regex.Escape(value.Trim()), "i")));
        }
    }

    public static void AddObjectId<T>(List<FilterDefinition<T>> filters, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!ObjectId.TryParse(value, out var objectId)) throw new ArgumentException($"O filtro {field} precisa ser um ObjectId válido.");
        filters.Add(new BsonDocumentFilterDefinition<T>(new BsonDocument(field, objectId)));
    }

    public static void AddDateRange<T>(List<FilterDefinition<T>> filters, string field, DateTime? from, DateTime? to)
    {
        if (from.HasValue) filters.Add(Builders<T>.Filter.Gte(field, NormalizeUtc(from.Value)));
        if (to.HasValue) filters.Add(Builders<T>.Filter.Lte(field, NormalizeUtc(to.Value)));
    }
}
