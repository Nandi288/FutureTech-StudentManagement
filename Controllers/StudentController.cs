using FutureTech_StudentManagement.Models;
using FutureTech_StudentManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FutureTech_StudentManagement.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class StudentController : Controller
    {
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<StudentController> _logger;

        public StudentController(
            ICosmosDbService cosmosDbService,
            IBlobStorageService blobStorageService,
            ILogger<StudentController> logger)
        {
            _cosmosDbService = cosmosDbService;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        // GET: Student
        public async Task<IActionResult> Index(string searchTerm, int page = 1, int pageSize = 10)
        {
            try
            {
                var students = await _cosmosDbService.GetStudentsAsync(searchTerm);

                // Pagination
                var totalItems = students.Count();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var pagedStudents = students
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Generate SAS tokens for images
                foreach (var student in pagedStudents)
                {
                    if (!string.IsNullOrEmpty(student.ProfileImageUrl))
                    {
                        var blobName = _blobStorageService.ExtractBlobNameFromUrl(student.ProfileImageUrl);
                        if (!string.IsNullOrEmpty(blobName))
                        {
                            ViewData[$"ImageUrl_{student.Id}"] = await _blobStorageService.GetFileUrlWithSasTokenAsync(blobName);
                        }
                    }
                }

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.SearchTerm = searchTerm;

                return View(pagedStudents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading students");
                TempData["Error"] = "Error loading students. Please try again.";
                return View(new List<Student>());
            }
        }

        
        public IActionResult Create()
        {
            return View();
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var student = new Student
                    {
                        FirstName = viewModel.FirstName,
                        LastName = viewModel.LastName,
                        Email = viewModel.Email,
                        MobileNumber = viewModel.MobileNumber,
                        EnrolmentStatus = viewModel.EnrolmentStatus
                    };

                    // Handle profile picture upload
                    if (viewModel.ProfilePicture != null && viewModel.ProfilePicture.Length > 0)
                    {
                        var fileName = $"{student.Id}{Path.GetExtension(viewModel.ProfilePicture.FileName)}";
                        var imageUrl = await _blobStorageService.UploadFileAsync(viewModel.ProfilePicture, fileName);
                        student.ProfileImageUrl = imageUrl;
                    }

                    await _cosmosDbService.AddStudentAsync(student);
                    TempData["Success"] = "Student added successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating student");
                    ModelState.AddModelError("", "Error saving student. Please try again.");
                }
            }

            return View(viewModel);
        }

        // GET: Student/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var student = await _cosmosDbService.GetStudentAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            var viewModel = new StudentViewModel
            {
                Id = student.Id,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.Email,
                MobileNumber = student.MobileNumber,
                EnrolmentStatus = student.EnrolmentStatus,
                ExistingProfileImageUrl = student.ProfileImageUrl
            };

            return View(viewModel);
        }

        // POST: Student/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, StudentViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var student = await _cosmosDbService.GetStudentAsync(id);
                    if (student == null)
                    {
                        return NotFound();
                    }

                    // Update student properties
                    student.FirstName = viewModel.FirstName;
                    student.LastName = viewModel.LastName;
                    student.Email = viewModel.Email;
                    student.MobileNumber = viewModel.MobileNumber;
                    student.EnrolmentStatus = viewModel.EnrolmentStatus;

                    
                    if (viewModel.ProfilePicture != null && viewModel.ProfilePicture.Length > 0)
                    {
                        
                        if (!string.IsNullOrEmpty(student.ProfileImageUrl))
                        {
                            var oldBlobName = _blobStorageService.ExtractBlobNameFromUrl(student.ProfileImageUrl);
                            if (!string.IsNullOrEmpty(oldBlobName))
                            {
                                await _blobStorageService.DeleteFileAsync(oldBlobName);
                            }
                        }

                        
                        var fileName = $"{student.Id}{Path.GetExtension(viewModel.ProfilePicture.FileName)}";
                        var imageUrl = await _blobStorageService.UploadFileAsync(viewModel.ProfilePicture, fileName);
                        student.ProfileImageUrl = imageUrl;
                    }

                    await _cosmosDbService.UpdateStudentAsync(student);
                    TempData["Success"] = "Student updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating student");
                    ModelState.AddModelError("", "Error updating student. Please try again.");
                }
            }

            return View(viewModel);
        }

        // GET: Student/Delete/5
        public async Task<IActionResult> Delete(string id, bool hardDelete = false)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var student = await _cosmosDbService.GetStudentAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            ViewBag.HardDelete = hardDelete;
            return View(student);
        }

        // POST: Student/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id, bool hardDelete = false)
        {
            try
            {
                var student = await _cosmosDbService.GetStudentAsync(id);

                if (hardDelete)
                {
                    // Delete image from blob storage
                    if (!string.IsNullOrEmpty(student?.ProfileImageUrl))
                    {
                        var blobName = _blobStorageService.ExtractBlobNameFromUrl(student.ProfileImageUrl);
                        if (!string.IsNullOrEmpty(blobName))
                        {
                            await _blobStorageService.DeleteFileAsync(blobName);
                        }
                    }

                    await _cosmosDbService.DeleteStudentAsync(id, hardDelete: true);
                    TempData["Success"] = "Student permanently deleted!";
                }
                else
                {
                    await _cosmosDbService.DeleteStudentAsync(id, hardDelete: false);
                    TempData["Success"] = "Student marked as inactive!";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student");
                TempData["Error"] = "Error deleting student. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Student/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var student = await _cosmosDbService.GetStudentAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            // Generate SAS token for image
            if (!string.IsNullOrEmpty(student.ProfileImageUrl))
            {
                var blobName = _blobStorageService.ExtractBlobNameFromUrl(student.ProfileImageUrl);
                if (!string.IsNullOrEmpty(blobName))
                {
                    ViewBag.SecureImageUrl = await _blobStorageService.GetFileUrlWithSasTokenAsync(blobName);
                }
            }

            return View(student);
        }
    }
}
