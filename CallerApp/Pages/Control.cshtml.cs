using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace CallerApp.Pages
{
    public class ControlModel : PageModel
    {
        private readonly RequestWorker _worker;

        [BindProperty]
        public int Interval { get; set; } = 5;

        [BindProperty]
        public string ServiceUrl { get; set; } = "http://localhost:7081/process";

        [BindProperty]
        public bool IsRunningSuccessfully { get; private set; } = false;

        public ControlModel(RequestWorker worker) => _worker = worker;
        public void OnPost()
        {
            _worker.SetInterval(Interval);
            _worker.SetUrl(ServiceUrl);
            IsRunningSuccessfully = _worker.IsRunningSuccessfully();
        }
    }
}
