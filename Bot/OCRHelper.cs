using System;
using System.Drawing;
using System.IO;
using Tesseract;

public sealed class SimpleOcr : IDisposable
{
    private readonly TesseractEngine _engine;

    public SimpleOcr(string tessDataPath, string lang = "eng")
    {
        if (!Directory.Exists(tessDataPath))
            throw new DirectoryNotFoundException(tessDataPath);

        _engine = new TesseractEngine(tessDataPath, lang, EngineMode.LstmOnly);
        _engine.DefaultPageSegMode = PageSegMode.Auto;
    }

    public string ReadText(Bitmap bmp, Rectangle region)
    {
        var roi = Rectangle.Intersect(region, new Rectangle(Point.Empty, bmp.Size));
        if (roi.Width <= 0 || roi.Height <= 0) return string.Empty;

        // Crop + grayscale
        using var crop = new Bitmap(roi.Width, roi.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(crop))
        {
            var cm = new System.Drawing.Imaging.ColorMatrix(new float[][]
            {
            new float[] {0.299f, 0.299f, 0.299f, 0, 0},
            new float[] {0.587f, 0.587f, 0.587f, 0, 0},
            new float[] {0.114f, 0.114f, 0.114f, 0, 0},
            new float[] {0,      0,      0,      1, 0},
            new float[] {0,      0,      0,      0, 1}
            });
            using var ia = new System.Drawing.Imaging.ImageAttributes();
            ia.SetColorMatrix(cm);

            g.DrawImage(
                bmp,
                new Rectangle(0, 0, crop.Width, crop.Height),
                roi.X, roi.Y, roi.Width, roi.Height,
                GraphicsUnit.Pixel,
                ia
            );
        }

        // Convert Bitmap -> Pix
        using var ms = new MemoryStream();
        crop.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        using var pix = Pix.LoadFromMemory(ms.ToArray());

        using var page = _engine.Process(pix);
        return (page.GetText() ?? "").Trim();
    }


    public void Dispose() => _engine.Dispose();
}
