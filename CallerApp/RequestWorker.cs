using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
namespace CallerApp
{
    public class RequestWorker : BackgroundService
    {
        private readonly IHttpClientFactory _factory;
        private int _interval = 5;
        public RequestWorker(IHttpClientFactory factory) => _factory = factory;
        public void SetInterval(int sec) => _interval = sec;
        protected override async Task ExecuteAsync(CancellationToken token)
        {
            var client = _factory.CreateClient();
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await client.GetAsync("http://processorapi/process");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred: {ex.Message}");
                }
                await Task.Delay(_interval * 1000, token);
            }
        }
    }
}
