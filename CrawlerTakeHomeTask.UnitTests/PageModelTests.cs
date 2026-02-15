namespace CrawlerTakeHomeTask.UnitTests;

using CrawlerTakeHomeTask.Models;

[TestClass]
public class PageModelTests
{
    [TestMethod]
    public void Constructor_CreatesPageModel_WithValidUri()
    {
        // Arrange
        var testUri = new Uri("http://localhost:8080/index.html");

        // Act
        var pageModel = new PageModel(testUri);

        // Assert
        Assert.IsNotNull(pageModel);
        Assert.AreEqual(testUri, pageModel.Url);
        Assert.AreEqual(0, pageModel.Depth);
        Assert.IsTrue(pageModel.IsRootPage);
    }

    [TestMethod]
    public void Constructor_ThrowsArgumentNullException_WhenUriIsNull()
    {
        // Arrange & Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => new PageModel(null!));
    }

    [TestMethod]
    public void IsValidPage_ReturnsTrue_ForHttpAbsoluteUri()
    {
        // Arrange
        var pageModel = new PageModel(new Uri("http://localhost:8080/page.html"));

        // Act
        var isValid = pageModel.IsValidPage;

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void IsValidPage_ReturnsTrue_ForHttpsAbsoluteUri()
    {
        // Arrange
        var pageModel = new PageModel(new Uri("https://example.com/page.html"));

        // Act
        var isValid = pageModel.IsValidPage;

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void IsValidPage_ReturnsTrue_ForRelativeUri()
    {
        // Arrange
        var pageModel = new PageModel(new Uri("/page.html", UriKind.Relative));

        // Act
        var isValid = pageModel.IsValidPage;

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void IsValidPage_ReturnsFalse_ForInvalidScheme()
    {
        // Arrange
        var pageModel = new PageModel(new Uri("ftp://example.com/file.txt"));

        // Act
        var isValid = pageModel.IsValidPage;

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void CreateChild_CreatesPageModel_WithIncrementedDepth()
    {
        // Arrange
        var parentUri = new Uri("http://localhost:8080/");
        var parent = new PageModel(parentUri);
        var childUri = new Uri("http://localhost:8080/child.html");

        // Act
        var child = parent.CreateChild(childUri);

        // Assert
        Assert.IsNotNull(child);
        Assert.AreEqual(childUri, child.Url);
        Assert.AreEqual(1, child.Depth);
        Assert.AreEqual(parent, child.Parent);
        Assert.IsFalse(child.IsRootPage);
    }

    [TestMethod]
    public void CreateChild_CreatesPageModel_WithLinkedRelativePath()
    {
        // Arrange
        var parentUri = new Uri("http://localhost:8080/child/index.html");
        var parent = new PageModel(parentUri);
        var childUri = new Uri("child2/index.html", UriKind.RelativeOrAbsolute);

        // Act
        var child = parent.CreateChild(childUri);

        // Assert
        Assert.AreEqual("/child/child2/index.html", child.RelativePath);
    }

    [TestMethod]
    public void Equals_ReturnsTrue_ForSameRelativePath()
    {
        // Arrange
        var page1 = new PageModel(new Uri("http://localhost:8080/index.html"));
        var page2 = new PageModel(new Uri("http://localhost:8080/index.html"));

        // Act
        var result = page1.Equals(page2);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Equals_ReturnsFalse_ForDifferentRelativePath()
    {
        // Arrange
        var page1 = new PageModel(new Uri("http://localhost:8080/index.html"));
        var page2 = new PageModel(new Uri("http://localhost:8080/other.html"));

        // Act
        var result = page1.Equals(page2);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void HasCrawled_ReturnsFalse_WhenDocumentNotLoaded()
    {
        // Arrange
        var pageModel = new PageModel(new Uri("http://localhost:8080/index.html"));

        // Act
        var hasCrawled = pageModel.HasCrawled;

        // Assert
        Assert.IsFalse(hasCrawled);
    }
}