using Library_Project.Model;
using Library_Project.Services;
using Library_Project.Services.Interfaces;
using Library_Project.Services;
using Moq;

namespace Library_Project_Tests
{
    public class LibraryServiceTests
    {
        private readonly Mock<IBookRepository> _repoMock;
        private readonly Mock<IMemberService> _memberMock;
        private readonly Mock<INotificationService> _notifMock;
        private readonly LibraryService _service;

        public LibraryServiceTests()
        {
            _repoMock = new Mock<IBookRepository>();
            _memberMock = new Mock<IMemberService>();
            _notifMock = new Mock<INotificationService>();

            _service = new LibraryService(_repoMock.Object, _memberMock.Object, _notifMock.Object);
        }

        [Fact]
        public void AddBook_ShouldAddNewBook_WhenNotExists()
        {
            _repoMock.Setup(r => r.FindBook("1984")).Returns((Book)null);

            _service.AddBook("1984", 3);

            _repoMock.Verify(r => r.SaveBook(It.Is<Book>(b => b.Title == "1984" && b.Copies == 3)), Times.Once);
        }

        [Fact]
        public void AddBook_ShouldIncreaseCopies_WhenBookExists()
        {
            var existing = new Book { Title = "1984", Copies = 2 };
            _repoMock.Setup(r => r.FindBook("1984")).Returns(existing);

            _service.AddBook("1984", 3);

            Assert.Equal(5, existing.Copies);
            _repoMock.Verify(r => r.SaveBook(existing), Times.Once);
        }

        [Theory]
        [InlineData("", 2)]
        [InlineData("Book", 0)]
        public void AddBook_ShouldThrow_WhenInvalidInput(string title, int copies)
        {
            Assert.ThrowsAny<ArgumentException>(() => _service.AddBook(title, copies));
        }

        [Fact]
        public void BorrowBook_ShouldDecreaseCopies_WhenValidMemberAndAvailable()
        {
            var book = new Book { Title = "Dune", Copies = 2 };
            _repoMock.Setup(r => r.FindBook("Dune")).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(true);

            bool result = _service.BorrowBook(1, "Dune");

            Assert.True(result);
            Assert.Equal(1, book.Copies);
            _notifMock.Verify(n => n.NotifyBorrow(1, "Dune"), Times.Once);
        }

        [Fact]
        public void BorrowBook_ShouldReturnFalse_WhenNoCopies()
        {
            var book = new Book { Title = "Dune", Copies = 0 };
            _repoMock.Setup(r => r.FindBook("Dune")).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(true);

            bool result = _service.BorrowBook(1, "Dune");

            Assert.False(result);
            _notifMock.Verify(n => n.NotifyBorrow(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void BorrowBook_ShouldThrow_WhenInvalidMember()
        {
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(false);

            Assert.Throws<InvalidOperationException>(() => _service.BorrowBook(1, "Dune"));
        }

        [Fact]
        public void ReturnBook_ShouldIncreaseCopies()
        {
            var book = new Book { Title = "Dune", Copies = 1 };
            _repoMock.Setup(r => r.FindBook("Dune")).Returns(book);

            bool result = _service.ReturnBook(1, "Dune");

            Assert.True(result);
            Assert.Equal(2, book.Copies);
            _notifMock.Verify(n => n.NotifyReturn(1, "Dune"), Times.Once);
        }

        [Fact]
        public void ReturnBook_ShouldReturnFalse_WhenBookNotFound()
        {
            _repoMock.Setup(r => r.FindBook("Unknown")).Returns((Book)null);

            bool result = _service.ReturnBook(1, "Unknown");

            Assert.False(result);
        }

        [Fact]
        public void GetAvailableBooks_ShouldReturnOnlyBooksWithCopies()
        {
            var all = new List<Book>
            {
                new Book { Title = "A", Copies = 0 },
                new Book { Title = "B", Copies = 1 },
                new Book { Title = "C", Copies = 3 }
            };
            _repoMock.Setup(r => r.GetAllBooks()).Returns(all);

            var available = _service.GetAvailableBooks();

            Assert.NotEmpty(available);
            Assert.Contains(available, b => b.Title == "B");
            Assert.Equal(2, available.Count);
        }

        [Fact]
        public void GetAvailableBooks_ShouldReturnEmpty_WhenNoBooksAvailable()
        {
            var all = new List<Book> { new Book { Title = "A", Copies = 0 } };
            _repoMock.Setup(r => r.GetAllBooks()).Returns(all);

            var result = _service.GetAvailableBooks();

            Assert.Empty(result);
        }

        [Fact]
        public void Verify_MethodsCalled_AtLeastOnce()
        {
            var book = new Book { Title = "Dune", Copies = 1 };
            _repoMock.Setup(r => r.FindBook("Dune")).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(true);

            _service.BorrowBook(1, "Dune");

            _repoMock.Verify(r => r.FindBook("Dune"), Times.AtLeastOnce);
        }

        [Fact]
        public void It_Is_PredicateExample()
        {
            var book = new Book { Title = "Dune", Copies = 2 };
            _repoMock.Setup(r => r.FindBook(It.Is<string>(s => s.StartsWith("D")))).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(true);

            var result = _service.BorrowBook(1, "Dune");

            Assert.True(result);
        }

        [Fact]
        public void It_IsAny_ShouldMatchAnyTitle()
        {
            var book = new Book { Title = "Anything", Copies = 2 };
            _repoMock.Setup(r => r.FindBook(It.IsAny<string>())).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(true);

            bool result = _service.BorrowBook(1, "RandomTitle");

            Assert.True(result);
            _notifMock.Verify(n => n.NotifyBorrow(It.IsAny<int>(), It.IsAny<string>()), Times.Once);
        }
    }


}
