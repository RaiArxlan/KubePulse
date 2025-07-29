using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CallerApp.Pages;

public class ControlModel : PageModel
{
    private readonly RequestWorker _worker;

    [BindProperty] public int Interval { get; set; } = 5;
    [BindProperty] public int BurstCount { get; set; } = 1;
    [BindProperty] public string ServiceUrl { get; set; } = "http://processorapi:8080/process";

    public bool IsRunningSuccessfully => _worker.IsRunningSuccessfully();
    public bool IsPaused => _worker.IsPaused();

    public ControlModel(RequestWorker worker)
    {
        _worker = worker;
    }

    public void OnPost()
    {
        if (Request.Form.ContainsKey("start"))
        {
            _worker.Resume();
        }
        else if (Request.Form.ContainsKey("stop"))
        {
            _worker.Pause();
        }
        else
        {
            _worker.SetInterval(Interval);
            _worker.SetBurst(BurstCount);
            _worker.SetUrl(ServiceUrl);
        }
    }
}
