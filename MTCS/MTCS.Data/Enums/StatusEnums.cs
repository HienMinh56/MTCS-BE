namespace MTCS.Data.Enums
{
    public enum UserStatus
    {
        Inactive = 0,
        Active = 1,
    }

    public enum VehicleStatus
    {
        Active = 1,
        Inactive = 2,
        OnDuty = 3,
        Onfixing = 4,
        Detained = 5
    }

    public enum DriverStatus
    {
        Inactive = 0,
        Active = 1,
        OnDuty = 2,
        Onfixing = 3,
        Detained = 4
    }

    public enum RevenuePeriodType
    {
        Weekly,
        Monthly,
        Yearly,
        Custom
    }
}
