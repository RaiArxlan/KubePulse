namespace CallerApp;

public class RequestWorker : BackgroundService
{
    private readonly IHttpClientFactory _factory;
    private int _interval = 5;
    private int _burstCount = 1;
    private string _url = "http://processorapi:8080/process";
    private bool _isRunning = false;
    private volatile bool _paused = false;

    public RequestWorker(IHttpClientFactory factory) => _factory = factory;

    public void SetInterval(int sec) => _interval = Math.Max(1, sec);
    public void SetBurst(int count) => _burstCount = Math.Max(1, count);
    public void SetUrl(string url) => _url = url?.Trim();
    public void Pause() => _paused = true;
    public void Resume() => _paused = false;
    public bool IsRunningSuccessfully() => _isRunning && !_paused;
    public bool IsPaused() => _paused;

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        var client = _factory.CreateClient();

        while (!token.IsCancellationRequested)
        {
            if (_paused)
            {
                await Task.Delay(1000, token);
                continue;
            }

            var tasks = new List<Task>();
            for (int i = 0; i < _burstCount; i++)
                tasks.Add(SendRequest(client));

            try
            {
                await Task.WhenAll(tasks);
                _isRunning = true;
            }
            catch
            {
                _isRunning = false;
            }

            await Task.Delay(_interval * 1000, token);
        }
    }

    private async Task SendRequest(HttpClient client)
    {
        try
        {
            var response = await client.GetAsync(_url);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred during burst request: {ex.Message}");
            throw;
        }
    }
}
