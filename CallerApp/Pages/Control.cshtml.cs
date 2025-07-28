using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace CallerApp.Pages
{
    public class ControlModel : PageModel
    {
        private readonly RequestWorker _worker;
        [BindProperty]
        public int Interval { get; set; } = 5;
        public ControlModel(RequestWorker worker) => _worker = worker;
        public void OnPost()
        {
            _worker.SetInterval(Interval);
        }
    }
}
