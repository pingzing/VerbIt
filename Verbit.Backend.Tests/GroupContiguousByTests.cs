using VerbIt.Backend.Extensions;

namespace Verbit.Backend.Tests
{
    [TestClass]
    public class GroupContiguousByTests
    {
        [TestMethod]
        public void ShouldWork()
        {
            var array = new int[] { 1, 2, 3, 5, 7, 8, 9, 10, 11, 12, 15 };
            var result = array.GroupContiguousBy(x => x).ToList();

            Assert.AreEqual(4, result.Count);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result[0].ToArray());
            CollectionAssert.AreEqual(new[] { 5 }, result[1].ToArray());
            CollectionAssert.AreEqual(new[] { 7, 8, 9, 10, 11, 12 }, result[2].ToArray());
            CollectionAssert.AreEqual(new[] { 15 }, result[3].ToArray());
        }
    }
}
