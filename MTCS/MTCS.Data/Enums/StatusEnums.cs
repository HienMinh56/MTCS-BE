namespace MTCS.Data.Enums
{
    public enum UserStatus
    {
        Active = 1,
        Inactive = 2,
        Deleted = 3
    }

    public enum DriverStatus
    {
        Active = 1,
        Inactive = 2,
        OnDuty = 3
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
}
