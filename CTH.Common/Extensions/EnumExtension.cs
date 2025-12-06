using CTH.Common.Helpers;

namespace CTH.Common.Extensions
{
    public static class EnumExtension
    {
        public static string Description<TEnum>(this TEnum value)
            where TEnum : struct
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("Type must be Enum");
            }

            return EnumHelper.Description((Enum)(object)value);
        }

        public static int ToInt<TEnum>(this TEnum value)
                    where TEnum : struct
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("Type must be Enum");
            }

            return (int)(object)value;
        }


        //TryParseFromInt as Enum Type Extension


    }
}
