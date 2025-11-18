using Library_Project.Model;
using Library_Project.Services;
using Library_Project.Services.Interfaces;
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

        /// <summary>
        /// Verifies that adding a new book when it does not exist results in SaveBook being called with a non-null Book having the specified title and copies.
        /// </summary>
        [Fact]
        public void AddBook_ShouldAddNewBook_WhenNotExists()
        {
            _repoMock.Setup(r => r.FindBook("1984")).Returns((Book?)null);

            _service.AddBook("1984", 3);

            _repoMock.Verify(r => r.SaveBook(It.Is<Book>(b => b.Title == "1984" && b.Copies == 3)), Times.Once);
        }

        /// <summary>
        /// Verifies that adding copies to an existing book increases its Copies and SaveBook is called with the existing instance.
        /// </summary>
        [Fact]
        public void AddBook_ShouldIncreaseCopies_WhenBookExists()
        {
            var existing = new Book { Title = "1984", Copies = 2 };
            _repoMock.Setup(r => r.FindBook("1984")).Returns(existing);

            _service.AddBook("1984", 3);

            Assert.Equal(5, existing.Copies);
            _repoMock.Verify(r => r.SaveBook(existing), Times.Once);
        }

        /// <summary>
        /// Ensures AddBook throws an ArgumentException for invalid title or non-positive copies using parameterized inputs.
        /// </summary>
        [Theory]
        [InlineData("", 2)]
        [InlineData("Book", 0)]
        public void AddBook_ShouldThrow_WhenInvalidInput(string title, int copies)
        {
            Assert.ThrowsAny<ArgumentException>(() => _service.AddBook(title, copies));
        }

        /// <summary>
        /// When a valid member borrows an available book, BorrowBook returns true, decreases the copy count and triggers a borrow notification.
        /// </summary>
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

        /// <summary>
        /// When a book has no copies left, BorrowBook returns false and no notification is sent.
        /// </summary>
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

        /// <summary>
        /// If a member is invalid, BorrowBook throws an InvalidOperationException.
        /// </summary>
        [Fact]
        public void BorrowBook_ShouldThrow_WhenInvalidMember()
        {
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(false);

            Assert.Throws<InvalidOperationException>(() => _service.BorrowBook(1, "Dune"));
        }

        /// <summary>
        /// Returning a book increases the copy count, returns true and sends a return notification.
        /// </summary>
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

        /// <summary>
        /// Returning a non-existing book results in false and no notifications.
        /// </summary>
        [Fact]
        public void ReturnBook_ShouldReturnFalse_WhenBookNotFound()
        {
            _repoMock.Setup(r => r.FindBook("Unknown")).Returns((Book?)null);

            bool result = _service.ReturnBook(1, "Unknown");

            Assert.False(result);
        }

        /// <summary>
        /// GetAvailableBooks returns only books with Copies &gt; 0 and the result contains expected titles.
        /// </summary>
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

        /// <summary>
        /// When no books have copies > 0, GetAvailableBooks returns an empty collection.
        /// </summary>
        [Fact]
        public void GetAvailableBooks_ShouldReturnEmpty_WhenNoBooksAvailable()
        {
            var all = new List<Book> { new Book { Title = "A", Copies = 0 } };
            _repoMock.Setup(r => r.GetAllBooks()).Returns(all);

            var result = _service.GetAvailableBooks();

            Assert.Empty(result);
        }

        /// <summary>
        /// Verifies that FindBook is called at least once during a successful borrow operation.
        /// </summary>
        [Fact]
        public void Verify_MethodsCalled_AtLeastOnce()
        {
            var book = new Book { Title = "Dune", Copies = 1 };
            _repoMock.Setup(r => r.FindBook("Dune")).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(true);

            _service.BorrowBook(1, "Dune");

            _repoMock.Verify(r => r.FindBook("Dune"), Times.AtLeastOnce);
        }

        /// <summary>
        /// Demonstrates predicate matching with It.Is to match titles by predicate and allow borrowing.
        /// </summary>
        [Fact]
        public void It_Is_PredicateExample()
        {
            var book = new Book { Title = "Dune", Copies = 2 };
            _repoMock.Setup(r => r.FindBook(It.Is<string>(s => s.StartsWith("D")))).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(true);

            var result = _service.BorrowBook(1, "Dune");

            Assert.True(result);
        }

        /// <summary>
        /// Demonstrates wildcard argument matching with It.IsAny so any title can be borrowed when a book is returned by the repository.
        /// </summary>
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

        /// <summary>
        /// Ensures that the saved Book argument is not null when adding a new book (captures the argument with a callback).
        /// </summary>
        [Fact]
        public void AddBook_SavesNonNullBookArgument()
        {
            Book? saved = null;
            _repoMock.Setup(r => r.FindBook("NewTitle")).Returns((Book?)null);
            _repoMock.Setup(r => r.SaveBook(It.IsAny<Book>())).Callback<Book>(b => saved = b);

            _service.AddBook("NewTitle", 1);

            Assert.NotNull(saved);
            Assert.Equal("NewTitle", saved!.Title);
            Assert.Equal(1, saved.Copies);
        }

        /// <summary>
        /// Verifies that BorrowBook called SaveBook exactly twice when borrowing twice and enough copies exist.
        /// </summary>
        [Fact]
        public void BorrowBook_SaveBook_Called_Exactly_Twice_When_BorrowedTwice()
        {
            var book = new Book { Title = "Series", Copies = 3 };
            _repoMock.Setup(r => r.FindBook("Series")).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(It.IsAny<int>())).Returns(true);

            _service.BorrowBook(1, "Series");
            _service.BorrowBook(2, "Series");

            _repoMock.Verify(r => r.SaveBook(It.Is<Book>(b => b.Title == "Series")), Times.Exactly(2));
        }

        /// <summary>
        /// Uses Times.AtMost to ensure notifications are not sent more than expected for a single borrow.
        /// </summary>
        [Fact]
        public void BorrowBook_NotifyBorrow_Called_AtMostOnce()
        {
            var book = new Book { Title = "Short", Copies = 1 };
            _repoMock.Setup(r => r.FindBook("Short")).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(true);

            var ok = _service.BorrowBook(1, "Short");

            Assert.True(ok);
            _notifMock.Verify(n => n.NotifyBorrow(It.IsAny<int>(), It.IsAny<string>()), Times.AtMost(1));
        }

        /// <summary>
        /// Demonstrates Assert.NotEqual: after a successful borrow the copy count differs from the original value.
        /// </summary>
        [Fact]
        public void BorrowBook_DecreasesCopies_NotEqualToOriginal()
        {
            var book = new Book { Title = "Change", Copies = 2 };
            _repoMock.Setup(r => r.FindBook("Change")).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(true);

            _service.BorrowBook(1, "Change");

            Assert.NotEqual(2, book.Copies);
        }
    }


}
