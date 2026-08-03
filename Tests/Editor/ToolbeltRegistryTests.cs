using NUnit.Framework;
using QFoldIT.Toolbelt.Editor.Core;

namespace QFoldIT.Toolbelt.Tests
{
    public class ToolbeltRegistryTests
    {
        [Test]
        public void Categories_AreNotEmpty()
        {
            Assert.IsNotEmpty(ToolbeltRegistry.Categories);
        }

        [Test]
        public void Categories_AllHaveNameAndDescription()
        {
            foreach (var c in ToolbeltRegistry.Categories)
            {
                Assert.IsFalse(string.IsNullOrEmpty(c.Name), "Category missing a name.");
                Assert.IsFalse(string.IsNullOrEmpty(c.Description), $"Category '{c.Name}' missing a description.");
            }
        }

        [Test]
        public void Version_IsSemVerLike()
        {
            StringAssert.IsMatch(@"^\d+\.\d+\.\d+$", ToolbeltRegistry.ToolbeltVersion);
        }
    }
}
