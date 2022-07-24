using System.ComponentModel;
using System.Globalization;

namespace VerbIt.Client.Converters
{
    // Used for MasterLists to display the lists of acceptable words in a cell.
    // A Pipe (|) value is used as a separator between valid values.
    // Gets registed in Program.cs.
    public class StringListToStringConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
            sourceType == typeof(string) || sourceType == typeof(List<string>);

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
            destinationType == typeof(List<string>) || destinationType == typeof(string);

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string stringVal)
            {
                var retVal = StringToList(stringVal);
                return retVal;
            }
            if (value is List<string> listVal)
            {
                var retVal = ListToString(listVal);
                return retVal;
            }
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        public override object? ConvertTo(
            ITypeDescriptorContext? context,
            CultureInfo? culture,
            object? value,
            Type destinationType
        )
        {
            if (value == null)
            {
                return null;
            }

            if (value is string stringVal)
            {
                var retVal = StringToList(stringVal);
                return retVal;
            }
            if (value is List<string> listVal)
            {
                var retVal = ListToString(listVal);
                return retVal;
            }
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        private List<string> StringToList(string value) => value.Split("|", StringSplitOptions.TrimEntries).ToList();

        private string ListToString(List<string> value) => string.Join(" | ", value);
    }
}
