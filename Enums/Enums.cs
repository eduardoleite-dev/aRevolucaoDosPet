using System.ComponentModel;

namespace aRevolucaoDosPet.Enums;

public enum AccessLevel
{
    ADMINISTRADOR = 0,
    TUTOR = 1,
    VETERINARIO = 2,
    ONG = 3,
    LOJA = 4
}

public enum UserStatus
{
    ACTIVE,
    SUSPENDED,
    DELETED
}

public enum Species
{
    DOG,
    CAT,
    OTHER
}

public enum Sex
{
    MALE,
    FEMALE,
    UNKNOWN
}

public enum PetProfileVisibility
{
    PUBLIC,
    FOLLOWERS_ONLY,
    PRIVATE
}

public enum PetStatus
{
    ALIVE,
    LOST,
    DECEASED,
    FOR_ADOPTION
}

public enum PetSize
{
    SMALL,
    MEDIUM,
    LARGE,
    GIANT
}

public enum PostType
{
    PHOTO,
    VIDEO,
    CAROUSEL,
    TEXT,
    AUDIO
}

public enum PostVisibility
{
    PUBLIC,
    FOLLOWERS_ONLY,
    PRIVATE
}

public enum ReactionType
{
    LIKE,
    LOVE,
    HAHA,
    WOW,
    [Description("latir - exclusivo para cães")]
    BARK,
    [Description("miar - exclusivo para gatos")]
    MEOW
}

public enum VaccineStatus
{
    PENDING,
    APPLIED,
    OVERDUE
}

public enum AppointmentType
{
    CONSULTATION,
    VACCINATION,
    EXAM,
    SURGERY,
    GROOMING,
    EMERGENCY,
    RETURN
}

public enum AppointmentStatus
{
    SCHEDULED,
    COMPLETED,
    CANCELED,
    MISSED
}

public enum ExpenseCategory
{
    FOOD,
    VET,
    MEDICINE,
    HYGIENE,
    TOYS,
    ACCESSORIES,
    INSURANCE,
    TRAINING,
    DAYCARE,
    WALKER,
    OTHER
}

public enum DiaryMood
{
    HAPPY,
    CALM,
    ANXIOUS,
    AGGRESSIVE,
    APATHETIC,
    PLAYFUL,
    SICK
}

public enum DiaryLevel
{
    LOW,
    NORMAL,
    HIGH
}

public enum StoolType
{
    NORMAL,
    SOFT,
    HARD,
    DIARRHEA,
    BLOOD
}

public enum UrineType
{
    NORMAL,
    LITTLE,
    MUCH,
    BLOOD
}

public enum HeatCyclePhase
{
    PROESTRUS,
    ESTRUS,
    DIESTRUS,
    ANESTRUS
}

public enum ExpenseRecurrenceFrequency
{
    DAILY,
    WEEKLY,
    MONTHLY,
    YEARLY
}

public enum PregnancyStatus
{
    ONGOING,
    COMPLETED,
    LOST
}

public enum WalkStatus
{
    IN_PROGRESS,
    COMPLETED,
    CANCELED
}

public enum WalkInteractionType
{
    PLAYED,
    SNIFFED,
    BARKED,
    IGNORED,
    AGGRESSIVE
}

public enum PlaceType
{
    PARK,
    SQUARE,
    PET_SHOP,
    VET,
    CAFE,
    DAYCARE,
    GROOMING
}

public enum NotificationType
{
    VACCINE_DUE,
    APPOINTMENT_REMINDER,
    HEAT_CYCLE_PREDICTION,
    NEW_FOLLOWER,
    NEW_LIKE,
    NEW_COMMENT,
    PET_NEARBY,
    EXPENSE_ALERT,
    LOST_PET_ALERT
}