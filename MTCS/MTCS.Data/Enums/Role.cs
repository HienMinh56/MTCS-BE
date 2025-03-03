namespace MTCS.Data.Enums
{
    public class Role : Enumeration<Role>
    {
        public static readonly Role Customer = new(1, "Customer");
        public static readonly Role Staff = new(2, "Staff");
        public static readonly Role Admin = new(3, "Admin");
        public static readonly Role Driver = new(4, "Driver");

        private Role(int id, string name) : base(id, name) { }

        // Helper method to get all defined roles
        public static IEnumerable<Role> GetAll() => GetAll(typeof(Role));

        // Helper method to find role by ID
        public static Role? FromId(int id) => FromId(id, GetAll());

        // Helper method to find role by name
        public static Role? FromName(string name) => FromName(name, GetAll());

        public static IEnumerable<Role> GetUserRoles() =>
            new[] { Customer, Staff, Admin };

        // Check if a role has elevated permissions (admin/staff)
        public bool IsElevated => this == Admin || this == Staff;

        // Check if a role can manage users
        public bool CanManageUsers => this == Admin;

        // Flag to identify if this is a user role (vs driver)
        public bool IsUserRole => this != Driver;
    }
}
