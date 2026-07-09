using System;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class EmbeddedAssemblyLoaderTests {
        [Test]
        public void Resources_Image_Loads_CloseButton_WithoutPreserializedDependency() {
            var bitmap = Resources_Image.imgCloseButton_Cold;
            Assert.IsNotNull(bitmap);
            Assert.Greater(bitmap.Width, 0);
            Assert.Greater(bitmap.Height, 0);
        }

        [Test]
        public void VistaMenuRenderer_Bootstrap_AllowsResourceBitmapAccess() {
            DropDownMenuBase.InitializeMenuRenderer();
            Assert.IsNotNull(DropDownMenuBase.CurrentRenderer);
        }
    }
}
