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
                    imageData = Convert.FromBase64String(imageCache[statusCode]); // Если изображение уже находится в кэше, получаем его из кэша
                }
                else
                {
                    // Если изображение не находится в кэше, получаем его из сервиса и сохраняем в кэше
                    string imageUrl = await GetCatImageByStatusCode(statusCode);
                    imageData = Convert.FromBase64String(imageUrl);
                    imageCache.TryAdd(statusCode, imageUrl);
                    _ = CacheImageAsync(statusCode, imageUrl, TimeSpan.FromMinutes(30)); // Запускаем асинхронную задачу для удаления изображения из кэша через 30 мину
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
                HttpResponseMessage response = await client.GetAsync(url);// Выполняем GET-запрос по указанному URL
                return (int)response.StatusCode; // Возвращаем статус код ответа
            }
        }

        private async Task<string> GetCatImageByStatusCode(int statusCode)
        {
            string apiUrl = $"https://http.cat/{statusCode}.jpg";
            // Создаем HttpClient для выполнения HTTP-запроса
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
            await Task.Delay(cacheDuration);// Ожидаем заданное время перед удалением изображения из кэша

            string removedImageUrl;
            imageCache.TryRemove(statusCode, out removedImageUrl);// Удаляем изображение из кэша
        }
    }
}
