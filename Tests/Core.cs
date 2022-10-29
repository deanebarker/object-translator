using DeaneBarker.ObjectTranslator;

namespace Tests
{
    [TestClass]
    public class Core
    {
        [TestMethod]
        public void SimpleProperty()
        {
            var spec = "id";
            dynamic target = ObjectTranslator.Translate(spec, taylor);

            Assert.AreEqual("ts", target.id);
        }

        [TestMethod]
        public void RenamedProperty()
        {
            var spec = "identifer: id";
            dynamic target = ObjectTranslator.Translate(spec, taylor);

            Assert.AreEqual("ts", target.identifer);
        }

        [TestMethod]
        public void ResolvedProperty()
        {
            var spec = "name: name.first";
            dynamic target = ObjectTranslator.Translate(spec, taylor);

            Assert.AreEqual("Taylor", target.name);
        }

        [TestMethod]
        public void SimpleCollection()
        {
            var spec = "pets";
            dynamic target = ObjectTranslator.Translate(spec, taylor);

            Assert.AreEqual("Olivia", target.pets[0]);

            // I want to test more here; but I don't know what type
        }

        [TestMethod]
        public void FluidExpression()
        {
            var spec = "year: dob | format_date:'yyyy'";
            dynamic target = ObjectTranslator.Translate(spec, taylor);

            Assert.AreEqual("1989", target.year);

            // I want to test more here; but I don't know what type
        }

        [TestMethod]
        public void ObjectCollection()
        {
            var line1 = "categories";
            var line2 = "  id";

            dynamic target = ObjectTranslator.Translate(CombineLines(line1, line2), taylor);

            Assert.AreEqual(2, target.categories[1].id);
        }

        [TestMethod]
        public void SimpleCollectionToObjectCollection()
        {
            var line1 = "pets";
            var line2 = "  name: _";
            var line3 = "  yell_name: _ | append:'!'";

            dynamic target = ObjectTranslator.Translate(CombineLines(line1, line2, line3), taylor);

            Assert.AreEqual("Benjamin", target.pets[1].name);
            Assert.AreEqual("Meredith!", target.pets[2].yell_name);
        }

        [TestMethod]
        public void ObjectCollectionWithNoSubGenerators()
        {
            var spec = "categories";

            dynamic target = ObjectTranslator.Translate(spec, taylor);

            // You really shouldn't do this -- you'll only get what you want from this if the object has an acceptable ToString() method
            Assert.IsTrue(target.categories[0].Contains("Queen"));
        }

        private string CombineLines(params string[] lines)
        {
            return string.Join(Environment.NewLine, lines);
        }

        private object taylor = new
        {
            id = "ts",
            name = new
            {
                first = "Taylor",
                last = "Swift"
            },
            age = 22,
            favorite_numbers = new[] { 7, 77, 777 },
            dob = new DateTime(1989, 12, 13),
            url = "/slay-queen",
            categories = new List<object>()
        {
            new { id = 1, name = "Queen", evidence = "None needed" },
            new { id = 2, name = "Genius", evidence = "None needed" }
        },
            pets = new List<string>()
        {
            "Olivia",
            "Benjamin",
            "Meredith"
        },
            best_songs = new[] { "False God", "All Too Well" },
            useless_men = new[] { "Harry", "Jake", "Joe" }
        };
    }
}