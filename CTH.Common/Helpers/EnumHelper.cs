using System.ComponentModel;
using System.Reflection;

namespace CTH.Common.Helpers
{
    public static class EnumHelper
    {
        public static string Description(Enum value)
        {
            var descriptionAttribute = value.GetType()
                .GetField(value.ToString())
                ?.GetCustomAttribute<DescriptionAttribute>();

            return descriptionAttribute?.Description ?? value.ToString();
        }
        public static bool TryParseFromInt<TEnum>(int value, out TEnum result)
        where TEnum : struct, Enum
        {
            if (Enum.IsDefined(typeof(TEnum), value))
            {
                result = (TEnum)(object)value;
                return true;
            }

            result = default;
            return false;
        }

        public static TEnum ParseFromInt<TEnum>(int value)
            => (TEnum)(object)value;
    }
}
