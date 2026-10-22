using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Models;
using MongoDB.Bson;

namespace aRevolucaoDosPet.Services;

internal static class GeoJsonMapper
{
    public static UserLocation MapPoint(UserLocationRequest? point, string field, bool required = false)
    {
        if (point is null)
        {
            if (required) throw new ArgumentException($"{field} é obrigatório.");
            return null!;
        }

        if (!string.Equals(point.Type, "Point", StringComparison.OrdinalIgnoreCase) || point.Coordinates.Length != 2 ||
            !double.IsFinite(point.Coordinates[0]) || !double.IsFinite(point.Coordinates[1]) ||
            point.Coordinates[0] is < -180 or > 180 || point.Coordinates[1] is < -90 or > 90)
        {
            throw new ArgumentException($"{field} deve ser GeoJSON Point com coordenadas [longitude, latitude].");
        }

        return new UserLocation { Type = "Point", Coordinates = point.Coordinates };
    }

    public static UserLocationRequest? ToPointDto(UserLocation? point) => point is null
        ? null
        : new UserLocationRequest { Type = point.Type, Coordinates = point.Coordinates };

    public static WalkRoute? MapRoute(List<double[]>? coordinates)
    {
        if (coordinates is null) return null;
        if (coordinates.Count is < 2 or > 100_000 || coordinates.Any(point => point.Length != 2 ||
                !double.IsFinite(point[0]) || !double.IsFinite(point[1]) ||
                point[0] is < -180 or > 180 || point[1] is < -90 or > 90))
        {
            throw new ArgumentException("route deve conter de 2 a 100000 coordenadas [longitude, latitude].");
        }

        return new WalkRoute { Type = "LineString", Coordinates = coordinates };
    }

    public static WalkRouteDto? ToRouteDto(WalkRoute? route) => route is null
        ? null
        : new WalkRouteDto(route.Type, route.Coordinates);

    public static BsonDocument? ToBsonData(Dictionary<string, System.Text.Json.JsonElement>? data) =>
        data is null ? null : BsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(data));

    public static Dictionary<string, System.Text.Json.JsonElement>? ToDataDto(BsonDocument? data) =>
        data is null
            ? null
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(data.ToJson());
}
