using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using VerbIt.Backend.Models;
using VerbIt.Backend.Repositories;
using VerbIt.DataModels;

namespace Verbit.Backend.Tests.Integration
{
    [TestClass]
    public class MasterListTests
    {
        private static WebApplicationFactory<Program> _factory = null!;
        private static HttpClient _httpClient = null!;

        [ClassInitialize]
        public static async Task Init(TestContext context)
        {
            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration(
                    (context, conf) =>
                    {
                        conf.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
                    }
                );
            });

            // Makes the test depend on this working, but maybe that's okay.
            // If it becomes a problem, just use the TableServiceClient directly and basically reimplement this method.
            var repository = _factory.Services.GetRequiredService<IVerbitRepository>();
            await repository.CreateAdminUser("test", "test", default);

            _httpClient = _factory.CreateClient();

            // Login, so that we can start doing authorized stuff
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", new LoginRequest("test", "test"));
            var bearerToken = await response.Content.ReadAsStringAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        [TestMethod]
        public void ShouldBootstrapAndCleanup()
        {
            // Just a little canary-in-the-coal-mine about whether or not things are running okay.
            Assert.IsTrue(true);
        }

        [TestMethod]
        public async Task CreatingAndDeletingTable_ShouldReturn200s()
        {
            const string listName = "IntegrationTestList_CreateAndDeleteTest";
            var row1 = new CreateMasterListRowRequest(
                new string[][]
                {
                    new[] { "row 1, cell 1, option 1", "row 1 cell 1, option 2" },
                    new[] { "row 1 cell 2, option 1" },
                }
            );
            var row2 = new CreateMasterListRowRequest(new string[][] { new[] { "row 2 cell 1 option 1" } });
            var createResponse = await PostNewList(listName, row1, row2);

            Assert.IsTrue(createResponse.IsSuccessStatusCode, "Failed to create master list.");

            var createdList = await createResponse.Content.ReadFromJsonAsync<MasterListRow[]>();
            var deleteResponse = await _httpClient.DeleteAsync($"/api/masterlist/{createdList![0].ListId}/delete/");
            Assert.IsTrue(deleteResponse.IsSuccessStatusCode);

            // Make sure it's actually deleted
            var getResponse = await _httpClient.GetAsync($"api/masterlist/{createdList[0].ListId}");

            Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [TestMethod]
        public async Task WhenCreatingTable_ShouldHaveGivenRows()
        {
            const string listName = "IntegrationTestList_CreateListWithRowsTest";
            var row1 = new CreateMasterListRowRequest(
                new string[][] { new[] { "One!", "One-A!" }, new[] { "Two!", "Two-A!" }, new[] { "Three!" } }
            );
            var row2 = new CreateMasterListRowRequest(
                new string[][] { new[] { "Run" }, new[] { "Ran" }, new[] { "Ran" }, new[] { "Juoksa" } }
            );
            var row3 = new CreateMasterListRowRequest(
                new string[][] { new[] { "Am", "Is", "Are", "Be" }, new[] { "Was" }, new[] { "Was" }, new[] { "Olla" } }
            );
            var row4 = new CreateMasterListRowRequest(new string[][] { new[] { "Oh no, only one cell" } });

            var createResponse = await PostNewList(listName, row1, row2, row3, row4);

            Assert.IsTrue(createResponse.IsSuccessStatusCode);

            var createdList = await createResponse.Content.ReadFromJsonAsync<MasterListRow[]>();
            var expectedWordsList = new[] { row1.Words, row2.Words, row3.Words, row4.Words };
            var actualWordsList = createdList!.Select(x => x.Words).ToArray();
            CollectionAssert.AreEqual(expectedWordsList, actualWordsList, StructuralComparisons.StructuralComparer);
        }

        [TestMethod]
        public async Task WhenDeletingRows_RemainingRowsShouldHaveCorrectedRowNumbers()
        {
            const string listName = "IntegrationTestList_RowFixupTest";
            var row1 = new CreateMasterListRowRequest(
                new string[][] { new[] { "One!", "One-A!" }, new[] { "Two!", "Two-A!" }, new[] { "Three!" } }
            );
            var row2 = new CreateMasterListRowRequest(
                new string[][] { new[] { "Run" }, new[] { "Ran" }, new[] { "Ran" }, new[] { "Juoksa" } }
            ); // Gets deleted
            var row3 = new CreateMasterListRowRequest(
                new string[][] { new[] { "Am", "Is", "Are", "Be" }, new[] { "Was" }, new[] { "Was" }, new[] { "Olla" } }
            );
            var row4 = new CreateMasterListRowRequest(new string[][] { new[] { "Oh no, only one cell" } });
            var row5 = new CreateMasterListRowRequest(new string[][] { new[] { "Oh no, only one cell" } }); // Gets deleted
            var row6 = new CreateMasterListRowRequest(new string[][] { new[] { "Oh no, only one cell" } }); // Gets deleted
            var row7 = new CreateMasterListRowRequest(new string[][] { new[] { "Oh no, only one cell" } }); // Gets deleted
            var row8 = new CreateMasterListRowRequest(new string[][] { new[] { "Oh no, only one cell" } });
            HttpResponseMessage createResponse = await PostNewList(listName, row1, row2, row3, row4, row5, row6, row7, row8);

            Assert.IsTrue(createResponse.IsSuccessStatusCode);

            var createdList = (await createResponse.Content.ReadFromJsonAsync<MasterListRow[]>())!;
            List<Guid>? oldOrder = createdList.Select(x => x.RowId).ToList();

            // Delete row numbers 2, 5, 6 and 7.
            var deleteRowsReponse = await DeleteListRows(
                createdList[0].ListId,
                oldOrder[1],
                oldOrder[4],
                oldOrder[5],
                oldOrder[6]
            );
            var newList = (await deleteRowsReponse.Content.ReadFromJsonAsync<MasterListRow[]>())!;

            Assert.IsTrue(deleteRowsReponse.IsSuccessStatusCode);
            Assert.AreEqual(4, newList.Length);
            Assert.IsTrue(newList.Single(x => x.RowId == oldOrder[0]).RowNum == 1);
            Assert.IsTrue(newList.Single(x => x.RowId == oldOrder[2]).RowNum == 2);
            Assert.IsTrue(newList.Single(x => x.RowId == oldOrder[3]).RowNum == 3);
            Assert.IsTrue(newList.Single(x => x.RowId == oldOrder[7]).RowNum == 4);
        }

        [TestMethod]
        public async Task WhenDeletingRows_ShouldDeleteEntireListIfAllRowsAreSpecified()
        {
            const string listName = "DeleteAllRowsDeletesListTooTest";
            var row1 = new CreateMasterListRowRequest(new string[][] { new[] { "Only one cell" } });
            var row2 = new CreateMasterListRowRequest(new string[][] { new[] { "Only one cell" } });
            var createResponse = await PostNewList(listName, row1, row2);
            var createdList = (await createResponse.Content.ReadFromJsonAsync<MasterListRow[]>())!;

            Assert.IsTrue(createResponse.IsSuccessStatusCode);

            var deleteRowsResponse = await DeleteListRows(createdList[0].ListId, createdList.Select(x => x.RowId).ToArray());

            Assert.IsTrue(deleteRowsResponse.IsSuccessStatusCode);

            var getResponse = await GetList(createdList[0].ListId);

            Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        private static async Task<HttpResponseMessage> GetList(Guid listId)
        {
            return await _httpClient.GetAsync($"api/masterlist/{listId}");
        }

        private static async Task<HttpResponseMessage> PostNewList(string listName, params CreateMasterListRowRequest[] rows)
        {
            return await _httpClient.PostAsJsonAsync("api/masterlist/create", new CreateMasterListRequest(listName, rows));
        }

        private static async Task<HttpResponseMessage> DeleteListRows(Guid listId, params Guid[] rowIds)
        {
            return await _httpClient.PostAsJsonAsync(
                $"api/masterlist/{listId}/deleterows",
                new DeleteMasterListRowsRequest(rowIds)
            );
        }

        [ClassCleanup]
        public static async Task Cleanup()
        {
            var settings = _factory.Services.GetRequiredService<IOptions<TableStorageSettings>>();
            var tableServiceClient = _factory.Services.GetRequiredService<TableServiceClient>();
            var tablesResponse = tableServiceClient.QueryAsync(x => x.Name.CompareTo(settings.Value.TablePrefix) >= 0);
            await foreach (var table in tablesResponse)
            {
                await tableServiceClient.DeleteTableAsync(table.Name);
            }

            _factory.Dispose();
        }
    }
}
