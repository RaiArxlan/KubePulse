using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CallerApp.Pages;

public class ControlModel : PageModel
{
    private readonly RequestWorker _worker;

    [BindProperty] public int Interval { get; set; }
    [BindProperty] public int BurstCount { get; set; }
    [BindProperty] public string ServiceUrl { get; set; }

    public bool IsRunningSuccessfully => _worker.IsRunningSuccessfully();
    public bool IsPaused => _worker.IsPaused();

    public ControlModel(RequestWorker worker)
    {
        _worker = worker;
        ServiceUrl = _worker.GetUrl();   
        BurstCount = _worker.GetBurst();   
        Interval = _worker.GetInterval();   
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
