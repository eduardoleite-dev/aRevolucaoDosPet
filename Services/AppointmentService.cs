using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IMongoCollection<Appointment> _appointments;
    private readonly IMongoCollection<Pet> _pets;

    public AppointmentService(IMongoDatabase database)
    {
        _appointments = database.GetCollection<Appointment>("Appointments");
        _pets = database.GetCollection<Pet>("Pets");
    }

    public async Task<AppointmentResponseDto> CreateAsync(
        CreateAppointmentRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        PetRecordHelper.EnsureObjectId(request.PetId, "petId");
        await PetRecordHelper.RequireEditablePetAsync(_pets, request.PetId, currentUserId, isAdministrator, cancellationToken);

        var now = DateTime.UtcNow;
        var appointment = new Appointment { PetId = request.PetId, CreatedAt = now };
        Apply(appointment, request, now);

        await _appointments.InsertOneAsync(appointment, cancellationToken: cancellationToken);
        return ToResponse(appointment);
    }

    public async Task<PagedResult<AppointmentResponseDto>> SearchAsync(
        AppointmentSearchFilter filter,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        await PetRecordHelper.AuthorizeSearchAsync(_pets, filter.PetId, currentUserId, isAdministrator, cancellationToken);

        var filters = new List<FilterDefinition<Appointment>>();
        var builder = Builders<Appointment>.Filter;

        PetRecordHelper.AddObjectId(filters, "_id", filter.Id);
        PetRecordHelper.AddObjectId(filters, "pet_id", filter.PetId);
        PetRecordHelper.AddText(filters, "title", filter.Title);
        PetRecordHelper.AddText(filters, "description", filter.Description);
        PetRecordHelper.AddText(filters, "veterinarian_name", filter.VeterinarianName);
        PetRecordHelper.AddText(filters, "veterinarian_crmv", filter.VeterinarianCrmv);
        PetRecordHelper.AddText(filters, "clinic_name", filter.ClinicName);
        PetRecordHelper.AddText(filters, "clinic_address", filter.ClinicAddress);
        PetRecordHelper.AddText(filters, "diagnosis", filter.Diagnosis);

        if (filter.Type.HasValue) filters.Add(builder.Eq(appointment => appointment.Type, filter.Type.Value));
        if (filter.Status.HasValue) filters.Add(builder.Eq(appointment => appointment.Status, filter.Status.Value));
        if (filter.ReminderSent.HasValue) filters.Add(builder.Eq(appointment => appointment.ReminderSent, filter.ReminderSent.Value));
        if (filter.MinDurationMinutes.HasValue) filters.Add(builder.Gte(appointment => appointment.DurationMinutes, filter.MinDurationMinutes.Value));
        if (filter.MaxDurationMinutes.HasValue) filters.Add(builder.Lte(appointment => appointment.DurationMinutes, filter.MaxDurationMinutes.Value));
        if (filter.MinPrice.HasValue) filters.Add(builder.Gte(appointment => appointment.Price, filter.MinPrice.Value));
        if (filter.MaxPrice.HasValue) filters.Add(builder.Lte(appointment => appointment.Price, filter.MaxPrice.Value));
        if (filter.HasPrescription.HasValue)
        {
            filters.Add(filter.HasPrescription.Value
                ? builder.Ne(appointment => appointment.PrescriptionUrl, null)
                : builder.Eq(appointment => appointment.PrescriptionUrl, null));
        }

        if (filter.HasAttachments.HasValue)
        {
            filters.Add(filter.HasAttachments.Value
                ? builder.SizeGt(appointment => appointment.Attachments, 0)
                : builder.Size(appointment => appointment.Attachments, 0));
        }

        PetRecordHelper.AddDateRange(filters, "scheduled_at", filter.ScheduledAtFrom, filter.ScheduledAtTo);
        PetRecordHelper.AddDateRange(filters, "created_at", filter.CreatedAtFrom, filter.CreatedAtTo);
        PetRecordHelper.AddDateRange(filters, "updated_at", filter.UpdatedAtFrom, filter.UpdatedAtTo);

        var query = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var totalCount = await _appointments.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var appointments = await _appointments.Find(query)
            .SortByDescending(appointment => appointment.ScheduledAt)
            .ThenBy(appointment => appointment.Id)
            .Skip(skip)
            .Limit(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AppointmentResponseDto>(
            appointments.Select(ToResponse).ToList(),
            totalCount,
            filter.PageNumber,
            filter.PageSize);
    }

    public async Task<AppointmentResponseDto?> GetByIdAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var appointment = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        return appointment is null ? null : ToResponse(appointment);
    }

    public async Task<AppointmentResponseDto?> UpdateAsync(
        string id,
        AppointmentWriteRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var appointment = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (appointment is null) return null;

        Apply(appointment, request, DateTime.UtcNow);
        await _appointments.ReplaceOneAsync(item => item.Id == id, appointment, cancellationToken: cancellationToken);
        return ToResponse(appointment);
    }

    public async Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var appointment = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (appointment is null) return false;

        var result = await _appointments.DeleteOneAsync(item => item.Id == id, cancellationToken);
        return result.DeletedCount == 1;
    }

    private async Task<Appointment?> FindAuthorizedAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _)) return null;

        var appointment = await _appointments.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (appointment is null) return null;

        return await PetRecordHelper.AuthorizeExistingAsync(_pets, appointment.PetId, currentUserId, isAdministrator, cancellationToken)
            ? appointment
            : null;
    }

    private static void Apply(Appointment appointment, AppointmentWriteRequest request, DateTime now)
    {
        var scheduledAt = PetRecordHelper.NormalizeUtc(request.ScheduledAt);

        // Reagendar volta o lembrete para pendente; reminder_sent só é gravado por esta regra.
        if (appointment.ScheduledAt != scheduledAt) appointment.ReminderSent = false;

        appointment.Type = request.Type;
        appointment.Title = request.Title.Trim();
        appointment.Description = PetRecordHelper.NormalizeOptional(request.Description);
        appointment.ScheduledAt = scheduledAt;
        appointment.DurationMinutes = request.DurationMinutes;
        appointment.Status = request.Status;
        appointment.VeterinarianName = PetRecordHelper.NormalizeOptional(request.VeterinarianName);
        appointment.VeterinarianCrmv = PetRecordHelper.NormalizeOptional(request.VeterinarianCrmv);
        appointment.ClinicName = PetRecordHelper.NormalizeOptional(request.ClinicName);
        appointment.ClinicAddress = PetRecordHelper.NormalizeOptional(request.ClinicAddress);
        appointment.Price = request.Price;
        appointment.Diagnosis = PetRecordHelper.NormalizeOptional(request.Diagnosis);
        appointment.PrescriptionUrl = PetRecordHelper.NormalizeOptional(request.PrescriptionUrl);
        appointment.Attachments = request.Attachments.Select(item => new AppointmentAttachment
        {
            Url = item.Url.Trim(),
            PublicId = PetRecordHelper.NormalizeOptional(item.PublicId),
            Type = item.Type.Trim(),
            Name = PetRecordHelper.NormalizeOptional(item.Name)
        }).ToList();
        appointment.UpdatedAt = now;
    }

    private static AppointmentResponseDto ToResponse(Appointment appointment) => new(
        appointment.Id,
        appointment.PetId,
        appointment.Type,
        appointment.Title,
        appointment.Description,
        appointment.ScheduledAt,
        appointment.DurationMinutes,
        appointment.Status,
        appointment.VeterinarianName,
        appointment.VeterinarianCrmv,
        appointment.ClinicName,
        appointment.ClinicAddress,
        appointment.Price,
        appointment.Diagnosis,
        appointment.PrescriptionUrl,
        appointment.Attachments.Select(item => new AppointmentAttachmentDto
        {
            Url = item.Url,
            PublicId = item.PublicId,
            Type = item.Type,
            Name = item.Name
        }).ToList(),
        appointment.ReminderSent,
        appointment.CreatedAt,
        appointment.UpdatedAt);
}
