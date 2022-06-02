namespace VerbIt.Backend.Extensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Groups the given sequence into distinct subsequences of contiguous elements.
        /// Requires that the list already be ordered in ascending order, and ordered
        /// by the property that will be checked for contiguousness.
        /// </summary>
        /// <param name="keySelector">The function to use to retrieve the property used to determine contiguousness.</param>
        /// <returns>A set of distinct sub-sequences, grouped by contiguous elements.</returns>
        /// <exception cref="ArgumentException"> Thrown if the list is not sorted.</exception>
        public static IEnumerable<IEnumerable<T>> GroupContiguousBy<T>(this IEnumerable<T> list, Func<T, int> keySelector)
        {
            if (list.Any())
            {
                int count = 1;
                int startIndex = 0;
                int startNumber = keySelector(list.First());
                int prevNum = startNumber;

                foreach (var curr in list.Skip(1))
                {
                    int currNum = keySelector(curr);
                    if (currNum < prevNum)
                    {
                        throw new ArgumentException("List is not sorted", nameof(list));
                    }
                    if (currNum - prevNum == 1)
                    {
                        count += 1;
                    }
                    else
                    {
                        yield return list.Skip(startIndex).Take(count);
                        startNumber = currNum;
                        startIndex = count + startIndex;
                        count = 1;
                    }
                    prevNum = currNum;
                }
                yield return list.Skip(startIndex).Take(count);
            }
        }
    }
}
