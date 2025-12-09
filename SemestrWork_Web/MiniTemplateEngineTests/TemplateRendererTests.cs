using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniTemplateEngine;
using System.Collections.Generic;
using MiniHttpServer.Shared;
using System.Drawing;

namespace MiniTemplateEngine.Tests
{
    [TestClass]
    public class TemplateRendererTests
    {
        private HtmlTemplateRenderer _renderer;

        [TestInitialize]
        public void Setup()
        {
            _renderer = new HtmlTemplateRenderer();
        }

        [TestMethod]
        public void RenderFromString_SimpleInterpolation_ReplacesValue()
        {
            var template = "Hello, ${Name}!";
            var model = new { Name = "John" };
         
            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("Hello, John!", result);
        }

        [TestMethod]
        public void RenderFromString_NestedProperty_ReplacesValue()
        {
            var template = "User: ${User.Name}";
            var model = new { User = new { Name = "Alice" } };
        
            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("User: Alice", result);
        }

        [TestMethod]
        public void RenderFromString_IfConditionTrue_ReturnsIfContent()
        {
            var template = "$if(IsVisible)Visible Content$endif";
            var model = new { IsVisible = true };
         
            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("Visible Content", result);
        }

        [TestMethod]
        public void RenderFromString_IfConditionFalse_ReturnsEmptyString()
        {
            var template = "$if(IsVisible)Visible Content$endif";
            var model = new { IsVisible = false };
        
            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void RenderFromString_ForeachLoop_IteratesCollection()
        {
            var template = "$foreach(point in Items)<div>${point.X}</div>$endfor";
            var ap = new Point(1, 2);
            var xp = new Point(3, 4);
            var model = new { Items = new List<Point> { ap, xp } };

            var result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("<div>1</div>\n<div>3</div>\n", result);
        }

        [TestMethod]
        public void RenderFromString_NestedConditions_ProcessesCorrectly()
        {
            var template = @"$if(Outer)
                                Outer
                                $if(Inner)Inner$endif
                            $endif";
            var model = new { Outer = true, Inner = true };

            var result = _renderer.RenderFromString(template, model);

            Assert.IsTrue(result.Contains("Outer") && result.Contains("Inner"));
        }

        [TestMethod]
        public void RenderFromString_NullModel_ReturnsOriginalTemplate()
        {
            var template = "Hello, ${Name}!";

            var result = _renderer.RenderFromString(template, null);

            Assert.AreEqual("Hello, ${Name}!", result);
        }
    }
}