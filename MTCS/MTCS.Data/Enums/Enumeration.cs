namespace MTCS.Data.Enums
{
    public abstract class Enumeration<TEnum> : IEquatable<TEnum> where TEnum : Enumeration<TEnum>
    {
        // Core properties all enumerations should have
        public int Id { get; }
        public string Name { get; }

        // Protected constructor to force derived classes to define instances
        protected Enumeration(int id, string name)
        {
            Id = id;
            Name = name;
        }

        protected static IEnumerable<TEnum> GetAll(Type type) =>
            typeof(TEnum).GetFields(System.Reflection.BindingFlags.Public |
                                   System.Reflection.BindingFlags.Static |
                                   System.Reflection.BindingFlags.DeclaredOnly)
                .Where(f => f.FieldType == typeof(TEnum))
                .Select(f => f.GetValue(null))
                .Cast<TEnum>();

        // Common methods to find enumeration values
        public static TEnum? FromId(int id, IEnumerable<TEnum> values) =>
            values.FirstOrDefault(v => v.Id == id);

        public static TEnum? FromName(string name, IEnumerable<TEnum> values) =>
            values.FirstOrDefault(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        // Value equality implementation
        public bool Equals(TEnum? other) => other != null && Id == other.Id;
        public override bool Equals(object? obj) => obj is TEnum other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
        public static bool operator ==(Enumeration<TEnum>? left, Enumeration<TEnum>? right) => Equals(left, right);
        public static bool operator !=(Enumeration<TEnum>? left, Enumeration<TEnum>? right) => !Equals(left, right);

        public override string ToString() => Name;
    }
}
