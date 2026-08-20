
namespace GrammarNazi.Core.Extensions;

public static class BuiltInTypesExtensions
{
    extension(IEnumerable<string> list)
    {
        public string Join(string separator = ",") => string.Join(separator, list);
    }

    extension(int val)
    {
        public bool IsAssignableToEnum<T>()
            where T : Enum
        {
            foreach (var item in Enum.GetValues(typeof(T)))
            {
                if (Convert.ToInt32(item) == val)
                {
                    return true;
                }
            }

            return false;
        }
    }

    extension(string str)
    {
        public IEnumerable<string> SplitInParts(int partLength)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            if (partLength <= 0)
            {
                throw new ArgumentException("Part length has to be positive.", nameof(partLength));
            }

            for (int i = 0; i < str.Length; i += partLength)
            {
                yield return str.Substring(i, Math.Min(partLength, str.Length - i));
            }
        }

        public bool ContainsAny(params ReadOnlySpan<string> values)
        {
            foreach (var val in values)
            {
                if (str.Contains(val))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsAny(StringComparison comparisonType, params ReadOnlySpan<string> values)
        {
            foreach (var val in values)
            {
                if (str.Contains(val, comparisonType))
                {
                    return true;
                }
            }

            return false;
        }
    }

    extension(Exception exception)
    {
        public IEnumerable<Exception> GetInnerExceptions()
        {
            var currentException = exception.InnerException;

            while (currentException != null)
            {
                yield return currentException;

                currentException = currentException.InnerException;
            }
        }
    }
}
