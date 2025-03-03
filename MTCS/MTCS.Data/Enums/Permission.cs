namespace MTCS.Data.Enums
{
    public class Permission : Enumeration<Permission>
    {
        // Common permissions
        public static readonly Permission ViewProfile = new(101, "ViewProfile");
        public static readonly Permission EditProfile = new(102, "EditProfile");

        // Customer permissions
        public static readonly Permission BookRides = new(201, "BookRides");
        public static readonly Permission ViewOwnRides = new(202, "ViewOwnRides");
        public static readonly Permission CancelOwnRides = new(203, "CancelOwnRides");
        public static readonly Permission ManageContracts = new(204, "ManageContracts");

        // Staff permissions
        public static readonly Permission ViewCustomers = new(301, "ViewCustomers");
        public static readonly Permission ManageTrips = new(302, "ManageTrips");
        public static readonly Permission AssignDrivers = new(303, "AssignDrivers");
        public static readonly Permission ManageRides = new(304, "ManageRides");
        public static readonly Permission ViewOrders = new(305, "ViewOrders");

        // Admin permissions
        public static readonly Permission ManageUsers = new(401, "ManageUsers");
        public static readonly Permission ManageDrivers = new(402, "ManageDrivers");
        public static readonly Permission ViewReports = new(403, "ViewReports");
        public static readonly Permission ManageShippingPrices = new(405, "ManageShippingPrices");

        // Driver permissions
        public static readonly Permission AcceptRides = new(501, "AcceptRides");
        public static readonly Permission ViewAssignedRides = new(502, "ViewAssignedRides");
        public static readonly Permission UpdateRideStatus = new(503, "UpdateRideStatus");
        public static readonly Permission ReportIssues = new(504, "ReportIssues");
        public static readonly Permission ViewSchedule = new(505, "ViewSchedule");

        private Permission(int id, string name) : base(id, name) { }

        public static IEnumerable<Permission> GetAll() => GetAll(typeof(Permission));
    }

    public static class RolePermissions
    {
        private static readonly Dictionary<Role, HashSet<Permission>> _rolePermissions = new()
        {
            [Role.Customer] = new HashSet<Permission>
            {
                Permission.ViewProfile,
                Permission.EditProfile,
                Permission.BookRides
            },

            [Role.Staff] = new HashSet<Permission>
            {
                Permission.ViewProfile,
                Permission.EditProfile,
                Permission.ViewCustomers,
                Permission.AssignDrivers,
                Permission.ManageRides
            },

            [Role.Admin] = new HashSet<Permission>
            {
                Permission.ViewProfile,
                Permission.EditProfile,
                Permission.ViewCustomers,
                Permission.AssignDrivers,
                Permission.ManageRides,
                Permission.ManageUsers,
            },

            [Role.Driver] = new HashSet<Permission>
            {
                Permission.ViewProfile,
                Permission.EditProfile,
                Permission.AcceptRides,
                Permission.UpdateRideStatus
            }
        };

        public static bool HasPermission(this Role role, Permission permission)
        {
            return _rolePermissions.TryGetValue(role, out var permissions) &&
                   permissions.Contains(permission);
        }

        public static IEnumerable<Permission> GetPermissions(this Role role)
        {
            return _rolePermissions.TryGetValue(role, out var permissions) ?
                permissions : Enumerable.Empty<Permission>();
        }
    }
}
