using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ImageMagick;
using OpenCvSharp;
using Microsoft.AspNetCore.Http;
using System.Runtime.InteropServices.ComTypes;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR.Models.LocalV3;
using Sdcb.PaddleOCR;
using System.Threading.Tasks;
using BotSharp.Abstraction.Knowledges;
using static System.Net.WebRequestMethods;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using Docnet;
using Docnet.Core.Models;
using Docnet.Core;
using Docnet.Core.Converters;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using BotSharp.Plugin.PaddleSharp.Settings;

namespace BotSharp.Plugin.PaddleSharp.Providers;

public class Pdf2TextConverter : IPdf2TextConverter
{
    // private readonly IServiceProvider _service;
    
    private Dictionary<int, string> _mappings = new Dictionary<int, string>();
    /*
    // private FullOcrModel _model;
    private string? _tempFolderPath = Path.GetTempPath();
    private FullOcrModel _model = LocalFullModels.EnglishV3;
    private MagickReadSettings _magicReadSettings;
    private int _consumerCount;
    private int _boundedCapacity;
    private double _acceptScore;
    */
    private FullOcrModel _model = LocalFullModels.EnglishV3;
    private PaddleSharpSettings _paddleSharpSettings;
    public Pdf2TextConverter(PaddleSharpSettings paddleSharpSettings)
    {
        _paddleSharpSettings = paddleSharpSettings;
    }

    public async Task<string> ConvertPdfToText(IFormFile formFile, int? startPageNum, int? endPageNum)
    {
        string pdfContent;
        if (_paddleSharpSettings.paddleModel)
        {
            await ConvertPdfToLocalImagesAsync(formFile, startPageNum, endPageNum);
            pdfContent = await LocalImageToTextsAsync();
        }
        else
        {
            pdfContent = await OpenPdfDocumentAsync(formFile, startPageNum, endPageNum);
        }
        return pdfContent;
    }

    public async Task<string> OpenPdfDocumentAsync(IFormFile formFile, int? startPageNum, int? endPageNum)
    {
        if (formFile.Length <= 0)
        {
            return await Task.FromResult(string.Empty);
        }

        var filePath = Path.GetTempFileName();

        using (var stream = System.IO.File.Create(filePath))
        {
            await formFile.CopyToAsync(stream);
        }

        var document = PdfDocument.Open(filePath);
        var content = "";
        foreach (Page page in document.GetPages())
        {
            if (startPageNum.HasValue && page.Number < startPageNum.Value)
            {
                continue;
            }

            if (endPageNum.HasValue && page.Number > endPageNum.Value)
            {
                continue;
            }

            content += page.Text;
        }

        return content;
    }

    public async Task<string> LocalImageToTextsAsync()
    {
        string loadPath;
        string contents = "";
        if (!Directory.Exists(_paddleSharpSettings.tempFolderPath))
        {
            throw new Exception("No local temporary files found! Please convert PDF to local images first by \"ConvertPdfToLocalImages\".");
        }

        // var converter = _service.GetRequiredService<IPaddleOcrConverter>();

        QueuedPaddleOcrAll all = new(() => new PaddleOcrAll(_model, PaddleDevice.Mkldnn())
        {
            AllowRotateDetection = true,
            Enable180Classification = false,
        }, consumerCount: _paddleSharpSettings.consumerCount, boundedCapacity: _paddleSharpSettings.boundedCapacity);
        
        
        foreach (var item in _mappings.OrderBy(x => x.Key))
        {
            loadPath = Path.Combine(_paddleSharpSettings.tempFolderPath, item.Value);
            // var pdfContent = converter.ConvertImageToText(loadPath);
            // contents += pdfContent;
            
            using (Mat src = Cv2.ImRead(loadPath))
            {
                PaddleOcrResult result = await all.Run(src);

                foreach (PaddleOcrResultRegion region in result.Regions)
                {
                    if (region.Score > _paddleSharpSettings.acceptScore)
                    {
                        contents += region.Text + " ";
                    }
                }
            }
            
            // Delete related Temp files after converting image to texts
            // DeleteTempFile(loadPath);
        }
        // await Console.Out.WriteLineAsync("Finished!");
        // all.Dispose();
        return contents;
    }

    private static void AddBytes(Bitmap bmp, byte[] rawBytes)
    {
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

        var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
        var pNative = bmpData.Scan0;

        Marshal.Copy(rawBytes, 0, pNative, rawBytes.Length);
        bmp.UnlockBits(bmpData);
    }

    public void DocnetConverter(string filePath, int width = 1080, int height = 1920)
    {
        var pageSettings = new PageDimensions(width, height);

        // using (var docReader = DocLib.Instance.GetDocReader("C:\\Users\\104199\\Postman\\files\\WM2077CW.pdf", new PageDimensions(1080, 1920)))
        using (var docReader = DocLib.Instance.GetDocReader(filePath, pageSettings))
        {
            using (var pageReader = docReader.GetPageReader(17))
            {
                var rawBytes = pageReader.GetImage();
                var pageWidth = pageReader.GetPageWidth();
                var pageHeight = pageReader.GetPageHeight();
                var characters = pageReader.GetCharacters();

                using (var bmp = new Bitmap(pageWidth, pageHeight, PixelFormat.Format32bppArgb))
                {
                    AddBytes(bmp, rawBytes);

                    using (var imageStream = new MemoryStream())
                    {
                        //saving and exporting
                        bmp.Save(imageStream, ImageFormat.Png);
                        System.IO.File.WriteAllBytes(filePath, imageStream.ToArray());
                    };
                }
            }
        };
    }

    public async Task ConvertPdfToLocalImagesAsync(IFormFile formFile, int? startPageNum, int? endPageNum)
    {
        string rootFileName;

        var filePath = Path.GetTempFileName();

        using (var stream = System.IO.File.Create(filePath))
        {
            await formFile.CopyToAsync(stream);
        }

        using var images = new MagickImageCollection();
        // _magicReadSettings.Density = new Density((double)300);
        /*
        using var images = new MagickImageCollection();
        MagickNET.SetGhostscriptDirectory("C:\\Users\\104199\\Downloads\\ghostpcl-10.01.2-win64\\ghostpcl-10.01.2-win64");

        images.Read("C:\\Users\\104199\\Postman\\files\\page12.pdf", new MagickReadSettings
        {
            Density = new Density(300, 300)
        });
        */
        images.Read(filePath, new MagickReadSettings
        {
            Density = new Density(300, 300)
        });

        if (images.Count == 0)
        {
            throw new Exception("PDF loading failed. Please check if the PDF format is correct!");
        }

        startPageNum = startPageNum.HasValue ? startPageNum : 1;
        endPageNum = endPageNum.HasValue ? endPageNum : images.Count;

        for (int page = (int)startPageNum; page <= (int)endPageNum; page++)
        {
            string tempFileName = Path.GetRandomFileName();
            tempFileName = Path.ChangeExtension(tempFileName, "png");
            rootFileName = Path.Combine(_paddleSharpSettings.tempFolderPath, tempFileName);

            // image.Format = MagickFormat.Jpg; Set to "Jpg" format
            images[page].Write(rootFileName);
            _mappings[page] = rootFileName;
        }
    }

    public void DeleteTempFile(string filePath)
    {
        System.IO.File.Delete(filePath);
    }
}
