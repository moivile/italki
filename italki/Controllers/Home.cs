using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace italki.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Home : ControllerBase
    {
        private HttpClient Client { get; }

        private const string TOKEN =
            "1615048561gAAAAABgHCJxH16yLwFcnKmQA1BUT4YnD_-uTqIQGKi_zbnqbtiYPWCRohMKg89eKeiUhN7IUHOlljuv7yvlc3c1aRcXuxQkFnWTXnhvfOdgB4e7ISQRsy76UQQ7x7wl9x7e5e1We9Uexsg3x-Wu8vnss1he9h-FmYQgPkje38MWFCQmaFCs6-E=";

        private const string HOST = "api.italki.com";
        private const string TEACHERS_API = "https://api.italki.com/api/v2/teachers";
        private const string TEACHER_API = "https://api.italki.com/api/v2/teacher/{0}";




        private readonly ILogger<Home> _logger;

        public Home(ILogger<Home> logger)
        {
            _logger = logger;
            Client = new HttpClient();
            Client.DefaultRequestHeaders.Add("X-Token", TOKEN);
            Client.DefaultRequestHeaders.Host = HOST;
        }

        [HttpGet]
        public async Task<List<Teacher>> GetTeachers()
        {
            var result = new List<Teacher>();

            var countryIds = new[] { "US", "GB", "CA", "AU", "ZA", "IE", "NZ" };


            foreach (var countryId in countryIds)
            {
                result.AddRange(await GetTeachersByCountryId(countryId));
            }

            result = result.Where(x => x.SessionCount > 1000).ToList();

            var tasks = result.Select(RetrieveStudentCountAsync);

            await Task.WhenAll(tasks);

            result = result.Where(x => x.Rating > 12).OrderByDescending(x => x.Rating).ToList();

            return result;
        }


        private async Task<List<Teacher>> GetTeachersByCountryId(string countryId)
        {
            var result = new List<Teacher>();
            for (var page = 1; ; page++)
            {

                var httpResponseMessage = await GetTeachersResponseAsync(page, countryId);

                result.AddRange(ReadResponse(httpResponseMessage, out bool hasNext));

                if (hasNext) break;
            }

            return result;
        }

        private async Task<HttpResponseMessage> GetTeachersResponseAsync(int page, string countryId)
        {
            var body = new
            {
                teach_language = new
                {
                    language = "english",
                    is_native = 1
                },
                teacher_info = new
                {
                    teacher_type = 1,
                    origin_country_id = new[] { countryId }
                },
                //week_time_user = new
                //{
                //    time_list = new[] { 18, 19, 20, 21 }
                //},
                page_size = 20,
                page
            };


            return await Client.PostAsJsonAsync(TEACHERS_API, body);
        }

        private List<Teacher> ReadResponse(HttpResponseMessage httpResponseMessage, out bool hasNext)
        {
            var jsonElement = JsonDocument.Parse(httpResponseMessage.Content.ReadAsStream()).RootElement;

            JsonElement.ArrayEnumerator data = jsonElement.GetProperty("data").EnumerateArray();

            var result = new List<Teacher>();

            foreach (var item in data)
            {

                var teacher = new Teacher
                {
                    UserId = item.GetProperty("user_info").GetProperty("user_id").GetInt32(),
                    Nickname = item.GetProperty("user_info").GetProperty("nickname").GetString(),
                    OriginCountryId = item.GetProperty("user_info").GetProperty("origin_country_id").GetString(),
                    SessionCount = item.GetProperty("teacher_info").GetProperty("session_count").GetInt32(),
                    MinPrice = item.GetProperty("course_info").GetProperty("min_price").GetInt32()
                };


                result.Add(teacher);
            }

            hasNext = jsonElement.GetProperty("paging").GetProperty("has_next").GetInt32() == 0;


            return result;
        }


        private async Task RetrieveStudentCountAsync(Teacher teacher)
        {
            var semaphore = new SemaphoreSlim(20);

            await semaphore.WaitAsync();
            var jsonElement = JsonDocument.Parse(await Client.GetStreamAsync(string.Format(TEACHER_API, teacher.UserId))).RootElement;
            semaphore.Release();

            teacher.StudentCount = jsonElement.GetProperty("data").GetProperty("teacher_info").GetProperty("student_count").GetInt32();
        }

    }
}
