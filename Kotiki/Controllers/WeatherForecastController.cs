using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImageController : ControllerBase
    {
        private static ConcurrentDictionary<int, string> imageCache = new ConcurrentDictionary<int, string>();

        [HttpGet]
        public async Task<IActionResult> GetImage(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return BadRequest("URL is required.");
            }

            int statusCode = await GetUrlStatusCode(url);
            byte[] imageData;
            try
            {
                if (imageCache.ContainsKey(statusCode))
                {
                    imageData = Convert.FromBase64String(imageCache[statusCode]); // ���� ����������� ��� ��������� � ����, �������� ��� �� ����
                }
                else
                {
                    // ���� ����������� �� ��������� � ����, �������� ��� �� ������� � ��������� � ����
                    string imageUrl = await GetCatImageByStatusCode(statusCode);
                    imageData = Convert.FromBase64String(imageUrl);
                    imageCache.TryAdd(statusCode, imageUrl);
                    _ = CacheImageAsync(statusCode, imageUrl, TimeSpan.FromMinutes(30)); // ��������� ����������� ������ ��� �������� ����������� �� ���� ����� 30 ����
                }

                return File(imageData, "image/jpeg");
            }
            catch (Exception ex) 
            {
                return BadRequest(ex.Message);
            }
        }


        private async Task<int> GetUrlStatusCode(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);// ��������� GET-������ �� ���������� URL
                return (int)response.StatusCode; // ���������� ������ ��� ������
            }
        }

        private async Task<string> GetCatImageByStatusCode(int statusCode)
        {
            string apiUrl = $"https://http.cat/{statusCode}.jpg";
            // ������� HttpClient ��� ���������� HTTP-�������
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                string base64Image = Convert.ToBase64String(imageBytes);
                return base64Image;
            }
        }


        private async Task CacheImageAsync(int statusCode, string imageUrl, TimeSpan cacheDuration)
        {
            await Task.Delay(cacheDuration);// ������� �������� ����� ����� ��������� ����������� �� ����

            string removedImageUrl;
            imageCache.TryRemove(statusCode, out removedImageUrl);// ������� ����������� �� ����
        }
    }
}
