using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Library;
using SMIS.Infrastructure.Data;

namespace SMIS.Web.Controllers;

[Authorize(Roles = "Administrator,Librarian")]
public class LibraryController : BaseController
{
    private new readonly ApplicationDbContext _db;

    public LibraryController(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    // ── DASHBOARD ────────────────────────────────────────────────

    // GET: /Library
    public async Task<IActionResult> Index()
    {
        var totalBooks = await _db.Books.CountAsync();
        var totalCopies = await _db.Books.SumAsync(b => b.TotalCopies);
        var availableCopies = await _db.Books.SumAsync(b => b.AvailableCopies);
        var issuedBooks = await _db.BookIssues
            .CountAsync(i => i.Status == BookIssueStatus.Issued);
        var overdueBooks = await _db.BookIssues
            .CountAsync(i => i.Status == BookIssueStatus.Issued &&
                             i.DueDate < DateTime.Today);
        var recentIssues = await _db.BookIssues
            .Include(i => i.Book)
            .Include(i => i.Student)
            .Include(i => i.StaffPayroll)
            .OrderByDescending(i => i.IssueDate)
            .Take(10)
            .ToListAsync();

        ViewBag.TotalBooks = totalBooks;
        ViewBag.TotalCopies = totalCopies;
        ViewBag.AvailableCopies = availableCopies;
        ViewBag.IssuedBooks = issuedBooks;
        ViewBag.OverdueBooks = overdueBooks;
        ViewBag.RecentIssues = recentIssues;

        return View();
    }

    // ── BOOK CATEGORIES ──────────────────────────────────────────

    // GET: /Library/Categories
    public async Task<IActionResult> Categories()
    {
        var categories = await _db.BookCategories
            .Include(c => c.Books)
            .OrderBy(c => c.OrderIndex)
            .ToListAsync();
        return View(categories);
    }

    // POST: /Library/CreateCategory
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string name,
        string? description)
    {
        var category = new BookCategory
        {
            Name = name,
            Description = description,
            OrderIndex = await _db.BookCategories.CountAsync() + 1,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };
        _db.BookCategories.Add(category);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Category '{name}' created successfully!";
        return RedirectToAction(nameof(Categories));
    }

    // POST: /Library/SeedDefaultCategories
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedDefaultCategories()
    {
        if (await _db.BookCategories.AnyAsync())
        {
            TempData["Error"] = "Categories already exist!";
            return RedirectToAction(nameof(Categories));
        }

        var categories = new List<BookCategory>
        {
            new BookCategory { Name = "Fiction", OrderIndex = 1,
                Description = "Novels and story books",
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new BookCategory { Name = "Non-Fiction", OrderIndex = 2,
                Description = "Factual books",
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new BookCategory { Name = "Mathematics", OrderIndex = 3,
                Description = "Mathematics textbooks and references",
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new BookCategory { Name = "Science", OrderIndex = 4,
                Description = "Science textbooks and references",
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new BookCategory { Name = "English", OrderIndex = 5,
                Description = "English language books",
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new BookCategory { Name = "Kiswahili", OrderIndex = 6,
                Description = "Kiswahili language books",
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new BookCategory { Name = "Social Studies", OrderIndex = 7,
                Description = "Social studies and history books",
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new BookCategory { Name = "Reference", OrderIndex = 8,
                Description = "Dictionaries, encyclopedias and atlases",
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new BookCategory { Name = "Religious Education", OrderIndex = 9,
                Description = "Religious education books",
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new BookCategory { Name = "Co-Curricular", OrderIndex = 10,
                Description = "Sports, arts and co-curricular books",
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
        };

        _db.BookCategories.AddRange(categories);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Default library categories created!";
        return RedirectToAction(nameof(Categories));
    }

    // ── BOOKS ────────────────────────────────────────────────────

    // GET: /Library/Books
    public async Task<IActionResult> Books(string? search, int? categoryId)
    {
        var query = _db.Books
            .Include(b => b.Category)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(b =>
                b.Title.Contains(search) ||
                (b.Author != null && b.Author.Contains(search)) ||
                (b.ISBN != null && b.ISBN.Contains(search)));

        if (categoryId.HasValue)
            query = query.Where(b => b.BookCategoryId == categoryId);

        var books = await query.OrderBy(b => b.Title).ToListAsync();

        ViewBag.Categories = await _db.BookCategories
            .OrderBy(c => c.OrderIndex).ToListAsync();
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;

        return View(books);
    }

    // GET: /Library/AddBook
    public async Task<IActionResult> AddBook()
    {
        ViewBag.Categories = await _db.BookCategories
            .OrderBy(c => c.OrderIndex).ToListAsync();
        return View();
    }

    // POST: /Library/AddBook
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBook(Book model)
    {
        model.AvailableCopies = model.TotalCopies;
        model.CreatedBy = User.Identity?.Name ?? "system";
        model.CreatedAt = DateTime.UtcNow;

        _db.Books.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Book '{model.Title}' added successfully!";
        return RedirectToAction(nameof(Books));
    }

    // GET: /Library/EditBook/5
    public async Task<IActionResult> EditBook(int id)
    {
        var book = await _db.Books.FindAsync(id);
        if (book == null) return NotFound();
        ViewBag.Categories = await _db.BookCategories
            .OrderBy(c => c.OrderIndex).ToListAsync();
        return View(book);
    }

    // POST: /Library/EditBook/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBook(int id, Book model)
    {
        var book = await _db.Books.FindAsync(id);
        if (book == null) return NotFound();

        book.Title = model.Title;
        book.Author = model.Author;
        book.ISBN = model.ISBN;
        book.Publisher = model.Publisher;
        book.PublicationYear = model.PublicationYear;
        book.Edition = model.Edition;
        book.BookCategoryId = model.BookCategoryId;
        book.TotalCopies = model.TotalCopies;
        book.ShelfLocation = model.ShelfLocation;
        book.Description = model.Description;
        book.Condition = model.Condition;
        book.UpdatedBy = User.Identity?.Name ?? "system";
        book.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Book '{book.Title}' updated successfully!";
        return RedirectToAction(nameof(Books));
    }

    // ── BOOK ISSUING ─────────────────────────────────────────────

    // GET: /Library/Issues
    public async Task<IActionResult> Issues(string? status)
    {
        var query = _db.BookIssues
            .Include(i => i.Book)
            .Include(i => i.Student)
            .Include(i => i.StaffPayroll)
            .AsQueryable();

        if (status == "overdue")
            query = query.Where(i => i.Status == BookIssueStatus.Issued &&
                                     i.DueDate < DateTime.Today);
        else if (status == "issued")
            query = query.Where(i => i.Status == BookIssueStatus.Issued);
        else if (status == "returned")
            query = query.Where(i => i.Status == BookIssueStatus.Returned);

        var issues = await query
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();

        // Update overdue status
        foreach (var issue in issues.Where(i =>
            i.Status == BookIssueStatus.Issued &&
            i.DueDate < DateTime.Today))
        {
            issue.Status = BookIssueStatus.Overdue;
        }
        await _db.SaveChangesAsync();

        ViewBag.Status = status;
        ViewBag.Books = await _db.Books
            .Where(b => b.AvailableCopies > 0)
            .OrderBy(b => b.Title).ToListAsync();
        ViewBag.Students = await _db.Students
            .Where(s => s.Status == Domain.Enums.StudentStatus.Active)
            .OrderBy(s => s.LastName).ToListAsync();
ViewBag.Status = status;
ViewBag.Books = await _db.Books
    .Where(b => b.AvailableCopies > 0)
    .OrderBy(b => b.Title).ToListAsync();
ViewBag.Students = await _db.Students
    .Where(s => s.Status == Domain.Enums.StudentStatus.Active)
    .OrderBy(s => s.LastName).ToListAsync();
ViewBag.StaffList = await _db.StaffPayrolls
    .Where(s => s.IsActive)
    .OrderBy(s => s.FullName).ToListAsync();
        return View(issues);
    }

    // POST: /Library/IssueBook
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> IssueBook(int bookId, int? studentId,
    int? staffPayrollId, string? visitorName, string? visitorPosition,
    string? visitorIdNumber, BorrowerType borrowerType, DateTime dueDate)
{
    var book = await _db.Books.FindAsync(bookId);
    if (book == null || book.AvailableCopies <= 0)
    {
        TempData["Error"] = "Book not available!";
        return RedirectToAction(nameof(Issues));
    }

    // Validate the right fields were supplied for the chosen borrower type
    if (borrowerType == BorrowerType.Student && !studentId.HasValue)
    {
        TempData["Error"] = "Please select a student.";
        return RedirectToAction(nameof(Issues));
    }
    if (borrowerType == BorrowerType.Staff && !staffPayrollId.HasValue)
    {
        TempData["Error"] = "Please select a staff member.";
        return RedirectToAction(nameof(Issues));
    }
    if (borrowerType == BorrowerType.Visitor && string.IsNullOrWhiteSpace(visitorName))
    {
        TempData["Error"] = "Please enter the visitor's name.";
        return RedirectToAction(nameof(Issues));
    }

    var issue = new BookIssue
    {
        BookId = bookId,
        BorrowerType = borrowerType,
        StudentId = borrowerType == BorrowerType.Student ? studentId : null,
        StaffPayrollId = borrowerType == BorrowerType.Staff ? staffPayrollId : null,
        VisitorName = borrowerType == BorrowerType.Visitor ? visitorName : null,
        VisitorPosition = borrowerType == BorrowerType.Visitor ? visitorPosition : null,
        VisitorIdNumber = borrowerType == BorrowerType.Visitor ? visitorIdNumber : null,
        IssueDate = DateTime.Today,
        DueDate = dueDate,
        Status = BookIssueStatus.Issued,
        CreatedBy = User.Identity?.Name ?? "system",
        CreatedAt = DateTime.UtcNow
    };

    book.AvailableCopies--;
    _db.BookIssues.Add(issue);
    await _db.SaveChangesAsync();

    TempData["Success"] = $"Book '{book.Title}' issued successfully!";
    return RedirectToAction(nameof(Issues));
}

    // POST: /Library/ReturnBook/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnBook(int id, string? notes)
    {
        var issue = await _db.BookIssues
            .Include(i => i.Book)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (issue == null)
        {
            TempData["Error"] = "Issue record not found!";
            return RedirectToAction(nameof(Issues));
        }

        issue.ReturnDate = DateTime.Today;
        issue.Status = BookIssueStatus.Returned;
        issue.Notes = notes;

        // Calculate fine if overdue (KES 5 per day)
        if (DateTime.Today > issue.DueDate)
        {
            var overdueDays = (DateTime.Today - issue.DueDate).Days;
            issue.FineAmount = overdueDays * 5;
        }

        // Return copy to available
        issue.Book.AvailableCopies++;
        issue.UpdatedBy = User.Identity?.Name ?? "system";
        issue.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var fineMsg = issue.FineAmount > 0
            ? $" Fine: KES {issue.FineAmount:N0}" : "";
        TempData["Success"] =
            $"Book returned successfully!{fineMsg}";
        return RedirectToAction(nameof(Issues));
    }
}