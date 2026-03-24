#nullable disable
using FutureTech_StudentManagement.Models;
using Microsoft.Azure.Cosmos;

namespace FutureTech_StudentManagement.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private readonly Container _container;

        public CosmosDbService(CosmosClient cosmosClient, string databaseName, string containerName)
        {
            _container = cosmosClient.GetContainer(databaseName, containerName);
        }

        public async Task<IEnumerable<Student>> GetStudentsAsync(string searchTerm = null, bool includeDeleted = false)
        {
            var queryBuilder = new List<string>();

            if (!includeDeleted)
            {
                queryBuilder.Add("SELECT * FROM c WHERE c.isDeleted = false");
            }
            else
            {
                queryBuilder.Add("SELECT * FROM c");
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchTermLower = searchTerm.ToLower();
                queryBuilder[0] = $"SELECT * FROM c WHERE (CONTAINS(LOWER(c.firstName), '{searchTermLower}') OR CONTAINS(LOWER(c.lastName), '{searchTermLower}') OR CONTAINS(c.id, '{searchTerm}'))";

                if (!includeDeleted)
                {
                    queryBuilder[0] += " AND c.isDeleted = false";
                }
            }

            var query = queryBuilder[0];
            var queryDefinition = new QueryDefinition(query);
            var results = new List<Student>();

            using (FeedIterator<Student> feedIterator = _container.GetItemQueryIterator<Student>(queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    var response = await feedIterator.ReadNextAsync();
                    results.AddRange(response);
                }
            }

            return results.OrderByDescending(s => s.CreatedAt);
        }

        public async Task<Student> GetStudentAsync(string id)
        {
            try
            {
                var response = await _container.ReadItemAsync<Student>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task AddStudentAsync(Student student)
        {
            student.Id = Guid.NewGuid().ToString();
            student.CreatedAt = DateTime.UtcNow;
            student.IsDeleted = false;
            await _container.CreateItemAsync(student, new PartitionKey(student.Id));
        }

        public async Task UpdateStudentAsync(Student student)
        {
            student.UpdatedAt = DateTime.UtcNow;
            await _container.UpsertItemAsync(student, new PartitionKey(student.Id));
        }

        public async Task DeleteStudentAsync(string id, bool hardDelete = false)
        {
            if (hardDelete)
            {
                await _container.DeleteItemAsync<Student>(id, new PartitionKey(id));
            }
            else
            {
                var student = await GetStudentAsync(id);
                if (student != null)
                {
                    student.IsDeleted = true;
                    student.UpdatedAt = DateTime.UtcNow;
                    await UpdateStudentAsync(student);
                }
            }
        }
    }
}