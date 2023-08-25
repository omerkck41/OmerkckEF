using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.Versioning;

namespace OmerkckEF.Biscom.ToolKit
{
	public static class Extensions
	{
		/// <summary>
		/// create a fast and practical dictionary (Extensions)
		/// ex; int bDate = 1989; var param = bDate.CreateParameters("@bdate");
		/// </summary>
		/// <param name="t"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static Dictionary<string, object> CreateParameters(this object t, string name)
		{
			return new Dictionary<string, object>()
			{
			  {
				name,
				t
			  }
			};
		}

		public static bool In<T>(this T obj, params T[] args)
		{
			return args?.Contains(obj) ?? false;
		}
		public static string? MyToString(this object change)
		{
			return change == null || change == DBNull.Value
				? string.Empty
				: change.ToString();
		}
		public static int? MyToInt(this object value)
		{
            if (value is null)
                return 0;

            if (value is int intValue)
                return intValue;

            if (value is string strValue)
            {
                if (int.TryParse(strValue, out int parsedInt))
                    return parsedInt;
            }
            else if (value is IConvertible convertibleValue)
            {
                try
                {
                    if (convertibleValue is long longValue)
                        return checked((int)longValue);
                    else if (convertibleValue is short shortValue)
                        return checked((int)shortValue);
                    else if (convertibleValue is byte byteValue)
                        return checked((int)byteValue);
                    else if (convertibleValue is decimal decimalValue)
                        return checked((int)decimalValue);
                    else if (convertibleValue is double doubleValue)
                        return checked((int)doubleValue);
                    else if (convertibleValue is float floatValue)
                        return checked((int)floatValue);
                    else
                        return Convert.ToInt32(convertibleValue);
                }
                catch (OverflowException)
                {
                    // Handle overflow errors if necessary
                }
                catch (Exception)
                {
                    // Handle conversion errors if necessary
                }
            }

            return 0;
        }
		public static decimal? ToDecimal(this object convertit)
		{
			if (convertit is decimal decimalValue)
				return decimalValue;
			else if (convertit is string strValue && decimal.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedDecimal))
				return parsedDecimal;

			return null;
		}
		public static string ToDateTime(this DateTime date)
		{
			return date.ToString("yyyy-MM-dd") ?? "";
		}
		public static DateTime? ToDate(this object date)
		{
			if (date is DateTime dateTimeValue)
				return dateTimeValue;
			else if (date is string strValue && DateTime.TryParse(strValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDateTime))
				return parsedDateTime;

			return null;
		}

		/// <summary>
		/// Saves objects to database if the object is Null.
		/// </summary>
		public static object DbNullIfNull(this object obj)
		{
			return obj != null ? obj : DBNull.Value;
		}

		/// <summary>
		/// Converting object to byte. (From Database)
		/// </summary>
		public static byte[]? MyToByte(this object change)
		{
			if (change == DBNull.Value || change == null) return null;

			return (byte[])(change);
		}

		/// <summary>
		/// Converting image to byte. (To Database)
		/// </summary>
		[SupportedOSPlatform("windows")]
		public static byte[]? ImageToByte(this Image img)
		{
			if (img == null) return null;

			using MemoryStream ms = new MemoryStream();
			img.Save(ms, ImageFormat.Jpeg);
			
			return ms.ToArray();
		}
	}
}
