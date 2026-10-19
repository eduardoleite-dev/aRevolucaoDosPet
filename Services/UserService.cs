using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace aRevolucaoDosPet.Services;

public class UserService : IUserService
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<UserSettings> _userSettings;

    public UserService(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("Users");
        _userSettings = database.GetCollection<UserSettings>("UserSettings");
    }

    public async Task<UserResponseDto?> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.Find(user => user.Email == email).AnyAsync(cancellationToken))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            AvatarUrl = NormalizeOptional(request.AvatarUrl),
            Bio = NormalizeOptional(request.Bio),
            Phone = NormalizeOptional(request.Phone),
            City = NormalizeOptional(request.City),
            State = NormalizeOptional(request.State),
            Country = request.Country.Trim().ToUpperInvariant(),
            Location = CreateLocation(request.Location),
            AccessLevel = AccessLevel.TUTOR,
            Status = UserStatus.ACTIVE,
            CreatedAt = now,
            UpdatedAt = now
        };

        try
        {
            await _users.InsertOneAsync(user, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return null;
        }

        return ToResponse(user);
    }

    public async Task<PagedResult<UserResponseDto>> SearchAsync(UserSearchFilter filter, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<User>>();
        var builder = Builders<User>.Filter;

        AddTextFilter(filters, "name", filter.Name);
        AddTextFilter(filters, "email", filter.Email);
        AddTextFilter(filters, "avatar_url", filter.AvatarUrl);
        AddTextFilter(filters, "bio", filter.Bio);
        AddTextFilter(filters, "phone", filter.Phone);
        AddTextFilter(filters, "city", filter.City);
        AddTextFilter(filters, "state", filter.State);
        AddTextFilter(filters, "country", filter.Country);
        AddTextFilter(filters, "location.type", filter.LocationType);

        if (!string.IsNullOrWhiteSpace(filter.Id))
        {
            if (!ObjectId.TryParse(filter.Id, out _))
            {
                throw new ArgumentException("O id informado não é um ObjectId válido.");
            }

            filters.Add(builder.Eq(user => user.Id, filter.Id));
        }

        if (filter.AccessLevel.HasValue)
        {
            filters.Add(builder.Eq(user => user.AccessLevel, filter.AccessLevel.Value));
        }

        if (filter.Status.HasValue)
        {
            filters.Add(builder.Eq(user => user.Status, filter.Status.Value));
        }
        else if (!filter.IncludeDeleted && filter.IsDeleted != true)
        {
            filters.Add(builder.Ne(user => user.Status, UserStatus.DELETED));
        }

        if (filter.EmailVerified.HasValue)
        {
            filters.Add(filter.EmailVerified.Value
                ? builder.Ne(user => user.EmailVerifiedAt, null)
                : builder.Eq(user => user.EmailVerifiedAt, null));
        }

        if (filter.HasLoggedIn.HasValue)
        {
            filters.Add(filter.HasLoggedIn.Value
                ? builder.Ne(user => user.LastLoginAt, null)
                : builder.Eq(user => user.LastLoginAt, null));
        }

        if (filter.HasLocation.HasValue)
        {
            filters.Add(filter.HasLocation.Value
                ? builder.Ne(user => user.Location, null)
                : builder.Eq(user => user.Location, null));
        }

        if (filter.IsDeleted.HasValue)
        {
            filters.Add(filter.IsDeleted.Value
                ? builder.Ne(user => user.DeletedAt, null)
                : builder.Eq(user => user.DeletedAt, null));
        }
        else if (!filter.IncludeDeleted && filter.Status != UserStatus.DELETED)
        {
            filters.Add(builder.Eq(user => user.DeletedAt, null));
        }

        if (filter.RadiusMeters.HasValue && (!filter.Longitude.HasValue || !filter.Latitude.HasValue))
        {
            throw new ArgumentException("Informe longitude e latitude para filtrar por proximidade.");
        }

        if (filter.RadiusMeters.HasValue)
        {
            var radiusInRadians = filter.RadiusMeters.Value / 6_378_100d;
            filters.Add(new BsonDocumentFilterDefinition<User>(new BsonDocument("location", new BsonDocument(
                "$geoWithin",
                new BsonDocument("$centerSphere", new BsonArray
                {
                    new BsonArray { filter.Longitude!.Value, filter.Latitude!.Value },
                    radiusInRadians
                })))));
        }
        else
        {
            if (filter.Longitude.HasValue)
            {
                filters.Add(builder.Eq("location.coordinates.0", filter.Longitude.Value));
            }

            if (filter.Latitude.HasValue)
            {
                filters.Add(builder.Eq("location.coordinates.1", filter.Latitude.Value));
            }
        }

        AddDateRange(filters, "email_verified_at", filter.EmailVerifiedAtFrom, filter.EmailVerifiedAtTo);
        AddDateRange(filters, "last_login_at", filter.LastLoginAtFrom, filter.LastLoginAtTo);
        AddDateRange(filters, "created_at", filter.CreatedAtFrom, filter.CreatedAtTo);
        AddDateRange(filters, "updated_at", filter.UpdatedAtFrom, filter.UpdatedAtTo);
        AddDateRange(filters, "deleted_at", filter.DeletedAtFrom, filter.DeletedAtTo);

        var query = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var totalCount = await _users.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var users = await _users.Find(query)
            .SortByDescending(user => user.CreatedAt)
            .ThenBy(user => user.Id)
            .Skip(skip)
            .Limit(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserResponseDto>(users.Select(ToResponse).ToList(), totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<UserResponseDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        var user = await _users.Find(existing => existing.Id == id).FirstOrDefaultAsync(cancellationToken);
        return user is null ? null : ToResponse(user);
    }

    public async Task<UserResponseDto?> UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var update = Builders<User>.Update
            .Set(user => user.Name, request.Name.Trim())
            .Set(user => user.Email, email)
            .Set(user => user.AvatarUrl, NormalizeOptional(request.AvatarUrl))
            .Set(user => user.Bio, NormalizeOptional(request.Bio))
            .Set(user => user.Phone, NormalizeOptional(request.Phone))
            .Set(user => user.City, NormalizeOptional(request.City))
            .Set(user => user.State, NormalizeOptional(request.State))
            .Set(user => user.Country, request.Country.Trim().ToUpperInvariant())
            .Set(user => user.Location, CreateLocation(request.Location))
            .Set(user => user.UpdatedAt, DateTime.UtcNow);

        try
        {
            var user = await _users.FindOneAndUpdateAsync(
                Builders<User>.Filter.Eq(existing => existing.Id, id) &
                Builders<User>.Filter.Ne(existing => existing.Status, UserStatus.DELETED),
                update,
                new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After },
                cancellationToken);

            return user is null ? null : ToResponse(user);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DuplicateUserEmailException();
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return false;
        }

        var existingUser = await _users.Find(user => user.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (existingUser is null)
        {
            return false;
        }

        if (existingUser.Status != UserStatus.DELETED)
        {
            var now = DateTime.UtcNow;
            var update = Builders<User>.Update
                .Set(user => user.Status, UserStatus.DELETED)
                .Set(user => user.DeletedAt, now)
                .Set(user => user.UpdatedAt, now);
            await _users.UpdateOneAsync(
                user => user.Id == id && user.Status != UserStatus.DELETED,
                update,
                cancellationToken: cancellationToken);
        }

        await _userSettings.DeleteOneAsync(settings => settings.UserId == id, cancellationToken);
        return true;
    }

    private static UserLocation? CreateLocation(UserLocationRequest? location)
    {
        if (location is null)
        {
            return null;
        }

        var coordinates = location.Coordinates;
        if (coordinates.Length != 2 ||
            !double.IsFinite(coordinates[0]) ||
            !double.IsFinite(coordinates[1]) ||
            coordinates[0] is < -180 or > 180 ||
            coordinates[1] is < -90 or > 90)
        {
            throw new ArgumentException("As coordenadas devem estar no formato [longitude, latitude] e dentro dos limites válidos.");
        }

        return new UserLocation { Coordinates = coordinates };
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AddTextFilter(List<FilterDefinition<User>> filters, string field, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            filters.Add(Builders<User>.Filter.Regex(field, new BsonRegularExpression(Regex.Escape(value.Trim()), "i")));
        }
    }

    private static void AddDateRange(
        List<FilterDefinition<User>> filters,
        string field,
        DateTime? from,
        DateTime? to)
    {
        if (from.HasValue)
        {
            filters.Add(Builders<User>.Filter.Gte(field, from.Value));
        }

        if (to.HasValue)
        {
            filters.Add(Builders<User>.Filter.Lte(field, to.Value));
        }
    }

    private static UserResponseDto ToResponse(User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.AvatarUrl,
        user.Bio,
        user.Phone,
        user.City,
        user.State,
        user.Country,
        user.Location,
        (int)user.AccessLevel,
        user.Status.ToString(),
        user.EmailVerifiedAt,
        user.LastLoginAt,
        user.CreatedAt,
        user.UpdatedAt,
        user.DeletedAt);
}