using System.Drawing;
using Tesseract;

namespace DmsProjeckt.Data
{
    public class ImageOcrUtil
    {
        public static async Task<string> ExtractFromImageAsync(Stream imageStream)
        {
            try
            {
                using var memStream = new MemoryStream();
                await imageStream.CopyToAsync(memStream);
                memStream.Position = 0;

                using var bmp = new Bitmap(memStream);
                using var engine = new TesseractEngine(@"./tessdata", "deu+eng", EngineMode.Default);
                using var pix = ConvertBitmapToPix(bmp);
                using var page = engine.Process(pix);

                return page.GetText().Trim();
            }
            catch (Exception ex)
            {
                return ""; // ou log error
            }
        }

        private static Pix ConvertBitmapToPix(Bitmap bmp)
        {
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            return Pix.LoadFromMemory(ms.ToArray());
        }
    }
}
