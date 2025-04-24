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
        NeedMaintain = 4,
        Maintaining = 5,
        NearRegistrationExpiry = 6,
        RegistrationExpired = 7
    }

    public enum DriverStatus
    {
        Inactive = 0,
        Active = 1,
        OnDuty = 2,
    }

    public enum RevenuePeriodType
    {
        Weekly,
        Monthly,
        Yearly,
        Custom
    }
}
