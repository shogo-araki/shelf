using QRCoder;
using SkiaSharp;

namespace shelf_project.Services
{
    public interface IQRCodeService
    {
        byte[] GenerateQRCodeImage(string url);
        string SaveQRCodeImage(string qrCode, string url);
    }

    public class QRCodeService : IQRCodeService
    {
        private readonly IWebHostEnvironment _environment;

        public QRCodeService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public byte[] GenerateQRCodeImage(string url)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            
            var matrix = qrCodeData.ModuleMatrix;
            var moduleCount = matrix.Count;
            var pixelSize = 10;
            var imageSize = moduleCount * pixelSize;
            
            using var surface = SKSurface.Create(new SKImageInfo(imageSize, imageSize));
            var canvas = surface.Canvas;
            
            canvas.Clear(SKColors.White);
            
            using var paint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = false
            };
            
            for (int row = 0; row < moduleCount; row++)
            {
                for (int col = 0; col < moduleCount; col++)
                {
                    if (matrix[row][col])
                    {
                        var rect = new SKRect(
                            col * pixelSize,
                            row * pixelSize,
                            (col + 1) * pixelSize,
                            (row + 1) * pixelSize
                        );
                        canvas.DrawRect(rect, paint);
                    }
                }
            }
            
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        public string SaveQRCodeImage(string qrCode, string url)
        {
            var imageBytes = GenerateQRCodeImage(url);
            
            var uploadsPath = Path.Combine(_environment.WebRootPath, "qrcodes");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var fileName = $"{qrCode}.png";
            var filePath = Path.Combine(uploadsPath, fileName);
            
            File.WriteAllBytes(filePath, imageBytes);
            
            return $"/qrcodes/{fileName}";
        }
    }
}