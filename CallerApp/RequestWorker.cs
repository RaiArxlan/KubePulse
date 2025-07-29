namespace CallerApp;

public class RequestWorker : BackgroundService
{
    private readonly IHttpClientFactory _factory;
    private int _interval = 5;
    private string _url = "http://processorapi:8080/process";
    private bool _isRunning = false;
    public RequestWorker(IHttpClientFactory factory) => _factory = factory;
    public void SetInterval(int sec) => _interval = sec;
    public void SetUrl(string url) => _url = url;
    public bool IsRunningSuccessfully() => _isRunning;
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        var client = _factory.CreateClient();
        while (!token.IsCancellationRequested)
        {
            try
            {
                await client.GetAsync(_url);
                _isRunning = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                _isRunning = false;
            }
            await Task.Delay(_interval * 1000, token);
        }
    }
}