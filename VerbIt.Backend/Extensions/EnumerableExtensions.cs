namespace VerbIt.Backend.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> GroupContiguousBy<T>(this IEnumerable<T> list, Func<T, int> keySelector)
        {
            if (list.Any())
            {
                int count = 1;
                int startNumber = keySelector(list.First());
                int last = startNumber;

                foreach (var curr in list.Skip(1))
                {
                    int i = keySelector(curr);
                    if (i < last)
                    {
                        throw new ArgumentException("Lit is not sorted", nameof(list));
                    }
                    if (i - last == 1)
                    {
                        count += 1;
                    }
                    else
                    {
                        // May need a -1 for startNumber
                        yield return list.Skip(startNumber).Take(count);
                        startNumber = i;
                        count = 1;
                    }
                    last = i;
                }
                // May need a -1 for startNumber
                yield return list.Skip(startNumber).Take(count);
            }
        }
    }
}
