using invoiceOCR.Services;
using Microsoft.AspNetCore.Mvc;

namespace invoiceOCR.Controllers
{
    public class OCRController : Controller
    {
        private readonly OCRProcess _ocrService; 
        private readonly IWebHostEnvironment _env;

        public OCRController(IWebHostEnvironment env, OCRProcess ocrProcess)
        {
            _ocrService = ocrProcess;
            _env = env;

        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult OCRProcess()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> OCRProcess(IFormFile imageFile)
        {
            ViewBag.Loading = true;

            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("imageFile", "Lütfen bir resim dosyası seçiniz.");
                return View();
            }

            var uploadPath = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, Path.GetFileName(imageFile.FileName));

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
            // OCR işlemi
            var invoice = await Services.OCRProcess.ExtractInvoiceData(filePath);

            ViewBag.Loading = false;
            return View(invoice);
        }
    }
}
