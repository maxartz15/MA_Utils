//https://github.com/maxartz15/MA_Toolbox

//References:
//https://stackoverflow.com/questions/93744/most-common-c-sharp-bitwise-operations-on-enums/417217#417217
//https://stackoverflow.com/questions/16100/how-should-i-convert-a-string-to-an-enum-in-c

using System;

namespace MA_Toolbox.Utils
{
	public static class EnumUtils
	{
		public static bool Has<T>(this Enum type, T value)
		{
			try
			{
				return ((int)(object)type & (int)(object)value) == (int)(object)value;
			}
			catch
			{
				return false;
			}
		}

		public static bool Is<T>(this Enum type, T value)
		{
			try
			{
				return (int)(object)type == (int)(object)value;
			}
			catch
			{
				return false;
			}
		}

		public static T Add<T>(this Enum type, T value)
		{
			try
			{
				return (T)(object)((int)(object)type | (int)(object)value);
			}
			catch (Exception ex)
			{
				throw new ArgumentException(string.Format("Could not append value from enumerated type '{0}'.", typeof(T).Name), ex);
			}
		}

		public static T Remove<T>(this Enum type, T value)
		{
			try
			{
				return (T)(object)(((int)(object)type & ~(int)(object)value));
			}
			catch (Exception ex)
			{
				throw new ArgumentException(string.Format("Could not remove value from enumerated type '{0}'.", typeof(T).Name), ex);
			}
		}

		public static T ParseEnum<T>(string value)
		{
			return (T)Enum.Parse(typeof(T), value, true);
		}
	}
}