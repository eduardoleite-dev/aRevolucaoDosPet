using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class PetFollowerService : IPetFollowerService
{
    private readonly IMongoCollection<PetFollower> _followers;
    private readonly IMongoCollection<Pet> _pets;
    private readonly IMongoCollection<User> _users;

    public PetFollowerService(IMongoDatabase database)
    {
        _followers = database.GetCollection<PetFollower>("PetFollowers");
        _pets = database.GetCollection<Pet>("Pets");
        _users = database.GetCollection<User>("Users");
    }

    public async Task<PetFollowerResponseDto> FollowUserAsync(
        string petId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        EnsureObjectId(petId, "petId");
        EnsureObjectId(userId, "userId");
        var pet = await GetFollowablePetAsync(petId, cancellationToken);
        var userExists = await _users.Find(user => user.Id == userId && user.Status == UserStatus.ACTIVE)
            .AnyAsync(cancellationToken);
        if (!userExists) throw new PetFollowerNotFoundException();
        if (pet.OwnerId == userId) throw new ArgumentException("O tutor principal não pode seguir o próprio pet.");

        var follower = new PetFollower { PetId = petId, FollowerUserId = userId };
        try
        {
            await _followers.InsertOneAsync(follower, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new PetAlreadyFollowedException();
        }

        await IncrementCountAsync(petId, followers: 1, following: 0, cancellationToken);
        return ToResponse(follower);
    }

    public async Task<bool> UnfollowUserAsync(
        string petId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(petId, out _) || !ObjectId.TryParse(userId, out _)) return false;
        var result = await _followers.DeleteOneAsync(
            follower => follower.PetId == petId && follower.FollowerUserId == userId,
            cancellationToken);
        if (result.DeletedCount == 0) return false;

        await IncrementCountAsync(petId, followers: -1, following: 0, cancellationToken);
        return true;
    }

    public async Task<PetFollowerResponseDto> FollowPetAsync(
        string petId,
        string followerPetId,
        string? requesterUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        EnsureObjectId(petId, "petId");
        EnsureObjectId(followerPetId, "followerPetId");
        if (petId == followerPetId) throw new ArgumentException("Um pet não pode seguir a si mesmo.");

        var targetPet = await GetFollowablePetAsync(petId, cancellationToken);
        var followerPet = await _pets.Find(pet => pet.Id == followerPetId && pet.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (followerPet is null) throw new PetFollowerNotFoundException();
        if (!isAdministrator && !CanEdit(followerPet, requesterUserId)) throw new PetFollowerPermissionException();

        var follower = new PetFollower { PetId = petId, FollowerPetId = followerPetId };
        try
        {
            await _followers.InsertOneAsync(follower, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new PetAlreadyFollowedException();
        }

        await IncrementCountAsync(petId, followers: 1, following: 0, cancellationToken);
        await IncrementCountAsync(followerPetId, followers: 0, following: 1, cancellationToken);
        return ToResponse(follower);
    }

    public async Task<bool> UnfollowPetAsync(
        string petId,
        string followerPetId,
        string? requesterUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(petId, out _) || !ObjectId.TryParse(followerPetId, out _)) return false;
        var followerPet = await _pets.Find(pet => pet.Id == followerPetId).FirstOrDefaultAsync(cancellationToken);
        if (followerPet is null) return false;
        if (!isAdministrator && !CanEdit(followerPet, requesterUserId)) throw new PetFollowerPermissionException();

        var result = await _followers.DeleteOneAsync(
            follower => follower.PetId == petId && follower.FollowerPetId == followerPetId,
            cancellationToken);
        if (result.DeletedCount == 0) return false;

        await IncrementCountAsync(petId, followers: -1, following: 0, cancellationToken);
        await IncrementCountAsync(followerPetId, followers: 0, following: -1, cancellationToken);
        return true;
    }

    public async Task<PagedResult<PetFollowerResponseDto>> GetFollowersAsync(
        string petId,
        PetFollowerSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        EnsureObjectId(petId, "petId");
        var filters = new List<FilterDefinition<PetFollower>>
        {
            Builders<PetFollower>.Filter.Eq(follower => follower.PetId, petId)
        };

        AddFollowerIdFilter(filters, "follower_user_id", filter.FollowerUserId);
        AddFollowerIdFilter(filters, "follower_pet_id", filter.FollowerPetId);
        var query = Builders<PetFollower>.Filter.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var totalCount = await _followers.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var followers = await _followers.Find(query)
            .SortByDescending(follower => follower.CreatedAt)
            .ThenBy(follower => follower.Id)
            .Skip(skip)
            .Limit(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PetFollowerResponseDto>(followers.Select(ToResponse).ToList(), totalCount, filter.PageNumber, filter.PageSize);
    }

    private async Task<Pet> GetFollowablePetAsync(string petId, CancellationToken cancellationToken)
    {
        var pet = await _pets.Find(item => item.Id == petId && item.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (pet is null) throw new PetNotFoundException();
        if (pet.Visibility == PetProfileVisibility.PRIVATE) throw new PetFollowerPermissionException();
        return pet;
    }

    private async Task IncrementCountAsync(
        string petId,
        int followers,
        int following,
        CancellationToken cancellationToken)
    {
        var updates = new List<UpdateDefinition<Pet>>();
        if (followers != 0) updates.Add(Builders<Pet>.Update.Inc(pet => pet.FollowersCount, followers));
        if (following != 0) updates.Add(Builders<Pet>.Update.Inc(pet => pet.FollowingCount, following));
        if (updates.Count == 0) return;

        await _pets.UpdateOneAsync(
            pet => pet.Id == petId && pet.DeletedAt == null,
            Builders<Pet>.Update.Combine(updates),
            cancellationToken: cancellationToken);
    }

    private static bool CanEdit(Pet pet, string? userId) =>
        userId is not null &&
        (pet.OwnerId == userId ||
         (ObjectId.TryParse(userId, out var objectId) && pet.CoTutorIds.Contains(objectId)));

    private static void EnsureObjectId(string? id, string field)
    {
        if (!ObjectId.TryParse(id, out _)) throw new ArgumentException($"O {field} informado não é um ObjectId válido.");
    }

    private static void AddFollowerIdFilter(List<FilterDefinition<PetFollower>> filters, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!ObjectId.TryParse(value, out var objectId)) throw new ArgumentException($"O filtro {field} precisa ser um ObjectId válido.");
        filters.Add(new BsonDocumentFilterDefinition<PetFollower>(new BsonDocument(field, objectId)));
    }

    private static PetFollowerResponseDto ToResponse(PetFollower follower) => new(
        follower.Id,
        follower.PetId,
        follower.FollowerUserId,
        follower.FollowerPetId,
        follower.CreatedAt);
}