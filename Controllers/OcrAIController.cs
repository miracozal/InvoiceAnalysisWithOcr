using invoiceOCR.Services;
using Microsoft.AspNetCore.Mvc;

namespace invoiceOCR.Controllers
{
    public class OcrAIController : Controller
    {
        private readonly OcrAIProcess _ocrAIProcess;
        private readonly IWebHostEnvironment _env;
        public OcrAIController(IWebHostEnvironment env, OcrAIProcess ocrAIProcess)
        {
            _ocrAIProcess = ocrAIProcess;
            _env = env;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult OcrAIProcess()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> OcrAIProcess(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return View(null);

            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string filePath = Path.Combine(uploadsFolder, Guid.NewGuid() + Path.GetExtension(imageFile.FileName));
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            ViewBag.Loading = true;
            var invoice = await _ocrAIProcess.ExtractWithAI(filePath);
            ViewBag.Loading = false;

            return View(invoice);
        }
    }
}
