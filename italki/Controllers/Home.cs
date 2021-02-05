using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace italki.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Home : ControllerBase
    {
        private HttpClient Client { get; }

        private string _token =
            "1615048561gAAAAABgHCJxH16yLwFcnKmQA1BUT4YnD_-uTqIQGKi_zbnqbtiYPWCRohMKg89eKeiUhN7IUHOlljuv7yvlc3c1aRcXuxQkFnWTXnhvfOdgB4e7ISQRsy76UQQ7x7wl9x7e5e1We9Uexsg3x-Wu8vnss1he9h-FmYQgPkje38MWFCQmaFCs6-E=";



        private readonly ILogger<Home> _logger;

        public Home(ILogger<Home> logger)
        {
            _logger = logger;
            Client = new HttpClient();
            Client.DefaultRequestHeaders.Add("X-Token", _token);
            Client.DefaultRequestHeaders.Host = "api.italki.com";
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

            var chunkArray = tasks.Chunk(20).ToArray();

            foreach (var chunk in chunkArray)
            {
                await Task.WhenAll(chunk);
            }

            result = result.Where(x => x.Rating > 10).OrderByDescending(x => x.Rating).ToList();

            return result;
        }


        private async Task<List<Teacher>> GetTeachersByCountryId(string countryId)
        {
            var result = new List<Teacher>();
            for (var page = 1; ; page++)
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
                    page_size = 20,
                    page
                };


                var httpResponseMessage = await Client.PostAsJsonAsync("https://api.italki.com/api/v2/teachers", body);
                var jsonElement = JsonDocument.Parse(await httpResponseMessage.Content.ReadAsStringAsync()).RootElement;

                if (jsonElement.GetProperty("success").GetInt32() == 0) continue;

                var data = jsonElement.GetProperty("data").EnumerateArray();

                foreach (var item in data)
                {

                    var teacher = new Teacher();

                    teacher.UserId = item.GetProperty("user_info").GetProperty("user_id").GetInt32();
                    teacher.Nickname = item.GetProperty("user_info").GetProperty("nickname").GetString();
                    teacher.OriginCountryId = item.GetProperty("user_info").GetProperty("origin_country_id").GetString();
                    teacher.SessionCount = item.GetProperty("teacher_info").GetProperty("session_count").GetInt32();
                    teacher.MinPrice = item.GetProperty("course_info").GetProperty("min_price").GetInt32();

                    result.Add(teacher);
                }

                if (jsonElement.GetProperty("paging").GetProperty("has_next").GetInt32() == 0) break;
            }

            return result;
        }


        private async Task RetrieveStudentCountAsync(Teacher teacher)
        {
            var jsonElement = JsonDocument.Parse(await Client.GetStreamAsync($"https://api.italki.com/api/v2/teacher/{teacher.UserId}")).RootElement;
            teacher.StudentCount = jsonElement.GetProperty("data").GetProperty("teacher_info").GetProperty("student_count").GetInt32();
        }

    }
}
