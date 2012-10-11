using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Hosting;
using System.Web.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace ScriptManagerExtensions.Tests
{
    [TestClass]
    public class ScriptResourceMappingExtensionsTests
    {
        [TestMethod]
        public void AddDefinitionIfFound_AddsDefinitionWithVerSupportIfFileFound()
        {
            // Arrange
            var map = new ScriptResourceMapping();
            var sd = new ScriptResourceDefinition
            {
                Path = "~/Scripts/",
                DebugPath = "~/Scripts/",
                CdnPath = "http://ajax.aspnetcdn.com/ajax/jQuery/jquery-{0}.min.js",
                CdnDebugPath = "http://ajax.aspnetcdn.com/ajax/jQuery/jquery-{0}.js",
                CdnSupportsSecureConnection = true
            };
            var vpp = Substitute.For<VirtualPathProvider>();
            var vd = Substitute.For<VirtualDirectory>("~/Scripts/");
            var file = Substitute.For<VirtualFile>("jquery-1.6.2.js");
            file.Name.Returns("jquery-1.6.2.js");
            vd.Files.Returns(new List<VirtualFile> { file });
            vpp.GetDirectory("~/Scripts/").Returns(vd);
            ScriptResourceMappingExtensions.VirtualPathProvider = vpp;

            // Act
            map.AddDefinitionIfFound("jquery", typeof(Page).Assembly, sd,
                @"^jquery-" + ScriptResourceMappingExtensions.VerRegexPattern + @"(?:\.min){0,1}\.js$");

            // Assert
            Assert.IsNotNull(map.GetDefinition("jquery", typeof(Page).Assembly));
        }

        [TestMethod]
        public void AddDefinitionIfFound_AddsDefinitionWithNoVerSupportIfFileFound()
        {
            // Arrange
            var map = new ScriptResourceMapping();
            var sd = new ScriptResourceDefinition
            {
                Path = "~/Scripts/"
            };
            var vpp = Substitute.For<VirtualPathProvider>();
            var vd = Substitute.For<VirtualDirectory>("~/Scripts/");
            var file = Substitute.For<VirtualFile>("foo.js");
            file.Name.Returns("foo.js");
            vd.Files.Returns(new List<VirtualFile> { file });
            vpp.GetDirectory("~/Scripts/").Returns(vd);
            ScriptResourceMappingExtensions.VirtualPathProvider = vpp;

            // Act
            map.AddDefinitionIfFound("foo", typeof(Page).Assembly, sd, @"^foo.js$");

            // Assert
            Assert.IsNotNull(map.GetDefinition("foo", typeof(Page).Assembly));
        }
    }
}