using FutureTech_StudentManagement.Models;

namespace FutureTech_StudentManagement.Services
{
    public interface ICosmosDbService
    {
        Task<IEnumerable<Student>> GetStudentsAsync(string searchTerm = null, bool includeDeleted = false);
        Task<Student> GetStudentAsync(string id);
        Task AddStudentAsync(Student student);
        Task UpdateStudentAsync(Student student);
        Task DeleteStudentAsync(string id, bool hardDelete = false);
    }
}