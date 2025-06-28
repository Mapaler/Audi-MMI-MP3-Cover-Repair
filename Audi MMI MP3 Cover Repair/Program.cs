// See https://aka.ms/new-console-template for more information
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.CommandLine;
using System.CommandLine.Parsing;
using ATL.AudioData;
using ATL;
using System.IO;
using System.CommandLine.Binding;
using Microsoft.VisualBasic;

const int defaultLength = 480;

var filesArgument = new Argument<string[]>
    (name: "paths",
    description: $"MP3 files or directorys that need to be modified.{Environment.NewLine}需要处理的多个 MP3 文件或目录。");

var maxWidthOption = new Option<uint>(
    name: "--max-width",
    description: $"The max Width of the new JPEG file.{Environment.NewLine}新图片最大宽度。",
    getDefaultValue: () => defaultLength);
maxWidthOption.AddAlias("-W");

var maxHeightOption = new Option<uint>(
    name: "--max-height",
    description: $"The max Height of the new JPEG file. {Environment.NewLine}新图片最大高度。",
    getDefaultValue: () => defaultLength);
maxHeightOption.AddAlias("-H");

var keepRatioOption = new Option<bool>(
    name: "--keep-ratio",
    description: $"Maintain the aspect ratio of the image. {Environment.NewLine}保持图片宽高比。",
    getDefaultValue: () => true);

var qualityOption = new Option<byte>(
    name: "--jpeg-quality",
    description: $"The compressed quality of the JPEG cover picture in new audio file. {Environment.NewLine}新音频文件内 JPEG 封面图片文件的压缩质量。",
    getDefaultValue: () => 90);
qualityOption.AddAlias("-q");
qualityOption.AddValidator(result =>
{
    byte quality = result.GetValueForOption(qualityOption);
    if (quality > 100 || quality < 0)
    {
        result.ErrorMessage = $"JPEG quality can only be between 1-100.{Environment.NewLine}JPEG 质量只能位于 1-100 之间。";
    }
});

var extractOriginalPictureOption = new Option<bool>(
    name: "--extract-original-picture",
    description: $"Extract the Original Picture to file. {Environment.NewLine}提取原始的图片文件。");

var saveModifiedAudioOption = new Option<bool>(
    name: "--save-new-audio",
    description: $"The modified audio file is save to a new file without modifying the original file, and appending the postfix to the file name. {Environment.NewLine}不修改原始文件，将修改后的音频文件储存为一个新文件，文件名附加后缀。");

var extractModifiedPictureOption = new Option<bool>(
    name: "--extract-new-picture",
    description: $"Extract the picture in modified audio to file, and appending the postfix to the file name. {Environment.NewLine}提取修改后音频内的图片文件，文件名附加后缀。");

var debugPostfixOption = new Option<string>(
    name: "--postfix",
    description: $"The postfix of the new filename when same a new file. {Environment.NewLine}储存新文件时的后缀名。",
    getDefaultValue: () => "-id3");
debugPostfixOption.AddValidator(result =>
{
    if (result.GetValueForOption(saveModifiedAudioOption) && String.IsNullOrWhiteSpace(result.GetValueForOption(debugPostfixOption)))
    {
        result.ErrorMessage = $"When saving as a new file, you must have a filename postfix that is not empty.{Environment.NewLine}储存为新文件时必须拥有不为空的文件名后缀。";
    }
});

var rootCommand = new RootCommand($"Fixed the issue that the cover picture of MP3 songs on Audi car MMI could not be displayed, and all the pictures in MP3 files were reduced to no larger than {defaultLength}×{defaultLength}, and the position was adjusted to Cover-Front." + Environment.NewLine +
    $"修复奥迪汽车多媒体平台 MP3 歌曲不能显示封面的问题，将 MP3 文件的图片全部缩小到不大于 {defaultLength}×{defaultLength} ，位置调整到封面(Cover-Front)。");
rootCommand.AddOption(maxWidthOption);
rootCommand.AddOption(maxHeightOption);
rootCommand.AddOption(keepRatioOption);
rootCommand.AddOption(qualityOption);
rootCommand.AddOption(extractOriginalPictureOption);
rootCommand.AddOption(saveModifiedAudioOption);
rootCommand.AddOption(extractModifiedPictureOption);
rootCommand.AddOption(debugPostfixOption);
rootCommand.Add(filesArgument); 

rootCommand.SetHandler(ReadFile, filesArgument, new HandlerOptionsBinder(
    maxWidthOption,
    maxHeightOption,
    keepRatioOption,
    qualityOption,
    extractOriginalPictureOption,
    debugPostfixOption,
    saveModifiedAudioOption,
    extractModifiedPictureOption)
);

await rootCommand.InvokeAsync(args);

static void ReadFile(string[] paths, HandlerOptions aHandlerOptions)
{
    //获取JPEG的编码器信息
    ImageCodecInfo? jpegEncoder = ImageCodecInfo.GetImageEncoders().ToList().Find(codec =>
    {
        return codec.FormatID == ImageFormat.Jpeg.Guid;
    });
    if (jpegEncoder == null)
    {
        Console.Error.WriteLine("Error:\tNot have JPEG Image Encoder. 没有 JPEG 图片编码器。");
        return;
    }

    List<FileInfo> files = [];
    foreach (string path in paths)
    {
        if (File.Exists(path) && Path.GetExtension(path) == ".mp3")
        {
            files.Add(new FileInfo(path));
        }
        else if (Directory.Exists(path))
        {
            files.AddRange(TraversingDirectory(new DirectoryInfo(path)));
        }
    }

    uint currentIndex = 0;
    foreach (FileInfo file in files)
    {
        currentIndex++;
        Console.WriteLine("Index:\t{0}/{1}", currentIndex, files.Count);
        ConvertMetadata(file, aHandlerOptions, jpegEncoder);
    }
}
//递归遍历文件夹
static List<FileInfo> TraversingDirectory(DirectoryInfo directory)
{
    List<FileInfo> files = [];
    Console.WriteLine("Traversing 遍历:\t{0}", directory.FullName);

    try
    {
        foreach (FileInfo file in directory.GetFiles("*.mp3"))
        {
            files.Add(file);
        }
    }
    catch (UnauthorizedAccessException ex)
    {
        Console.WriteLine($"警告: 无法访问目录 {directory.FullName} 的文件: {ex.Message}");
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine($"警告: 目录不存在 {directory.FullName}: {ex.Message}");
    }

    try
    {
        foreach (DirectoryInfo subDir in directory.GetDirectories())
        {
            files.AddRange(TraversingDirectory(subDir));
        }
    }
    catch (UnauthorizedAccessException ex)
    {
        Console.WriteLine($"警告: 无法访问目录 {directory.FullName} 的子目录: {ex.Message}");
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine($"警告: 目录不存在 {directory.FullName}: {ex.Message}");
    }
    return files;
}

static void ConvertMetadata(FileInfo file, HandlerOptions aHandlerOptions, ImageCodecInfo jpegEncoder)
{
    if (!file.Exists) //如果文件不存在，则直接跳过
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine("Error:\tFile is not Exists. 文件不存在。");
        Console.Error.WriteLine("\t{0}", file);
        Console.ResetColor();
        Console.WriteLine();
        return;
    }

    uint maxWidth = aHandlerOptions.MaxWidth;
    uint maxHeight = aHandlerOptions.MaxHeight;
    bool keepRatio = aHandlerOptions.KeepRatio;
    byte jpegQuality = aHandlerOptions.JpegQuality;
    bool extractOriginalPicture = aHandlerOptions.ExtractOriginalPicture;
    string debugPostfixPicture = aHandlerOptions.DebugPostfixPicture;
    bool saveModifiedAudio = aHandlerOptions.SaveModifiedAudio;
    bool extractModifiedPicture = aHandlerOptions.ExtractModifiedPicture;
#if DEBUG
    extractOriginalPicture = true;
    saveModifiedAudio = true;
    extractModifiedPicture = true;
#endif

    //读取音乐文件
    Track theTrack = new(file.FullName);

    Console.WriteLine("Title:\t" + theTrack.Title);

    //是否有 ID3v2.3
    bool hasID3v2_3 = theTrack.MetadataFormats.Any((Format format) => format.Name == "ID3v2.3");
    if (!hasID3v2_3)
    {
        Console.WriteLine("Need change Metadata format to ID3v2.3. 需要改变元数据版本到ID3v2.3");
    }
    bool pictureChanged = false;

    if (theTrack.EmbeddedPictures.Count == 0)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine("Error:\tAudio file don't embed any Picture. 音频文件里没有内嵌图片。");
        Console.Error.WriteLine("\t{0}", file);
        Console.ResetColor();
    }
    else
    {
        PictureInfo pic = theTrack.EmbeddedPictures.First();
        Image image = Image.FromStream(new MemoryStream(pic.PictureData));
        if (extractOriginalPicture)
        {
            //保存图片原始数据
            string coverFileName = Path.Combine(file.DirectoryName, $"{Path.GetFileNameWithoutExtension(file.Name)}-{pic.PicType}.{GetExtFromMimeType(pic.MimeType)}");
            FileStream fileStream = new(coverFileName, FileMode.OpenOrCreate, FileAccess.Write);
            fileStream.Write(pic.PictureData);
            fileStream.Close();
        }

        Console.WriteLine("Picture Type:\t{0}", pic.PicType);
        Console.WriteLine("Size:\t{0}x{1}", image.Width, image.Height);

        if (pic.PicType != PictureInfo.PIC_TYPE.Front || //不是封面也要处理
            image.RawFormat.ToString() != ImageFormat.Jpeg.ToString() || //不是 Jpeg
            image.Width > maxWidth || image.Height > maxHeight) //宽高大于设定
        {

            PictureInfo newPicture;
            if (image.RawFormat.ToString() != ImageFormat.Jpeg.ToString() || image.Width > maxWidth || image.Height > maxHeight)
            {
                double scaleW = (double)maxWidth / image.Width; //宽度缩小比例
                double scaleH = (double)maxHeight / image.Height; //高度缩小比例
                if (keepRatio)
                {
                    scaleW = Math.Min(scaleW, scaleH);
                    scaleH = Math.Min(scaleW, scaleH);
                }
                int newWidth = Math.Min(image.Width, (int)Math.Round(image.Width * scaleW));
                int newHeight = Math.Min(image.Height, (int)Math.Round(image.Height * scaleH));

                Console.WriteLine("Resize Scale:\t{0}:{1}", scaleW, scaleH);
                Bitmap newImage = new(image, new Size(newWidth, newHeight));

                //创建JPEG压缩的参数
                EncoderParameters parameters = new(1);
                parameters.Param[0] = new EncoderParameter(Encoder.Quality, (long)jpegQuality);

                //在内存中压缩转存一遍
                MemoryStream jpegMemory = new();
                newImage.Save(jpegMemory, jpegEncoder, parameters);

                newPicture = PictureInfo.fromBinaryData(jpegMemory.ToArray(), PictureInfo.PIC_TYPE.Front);
                if (extractModifiedPicture)
                {
                    //保存新的封面图片
                    string coverResizeFileName = Path.Combine(file.DirectoryName, $"{Path.GetFileNameWithoutExtension(file.Name)}-{newPicture.PicType}{debugPostfixPicture}.{GetExtFromMimeType(newPicture.MimeType)}");
                    FileStream fileStream = new(coverResizeFileName, FileMode.OpenOrCreate, FileAccess.Write);
                    fileStream.Write(newPicture.PictureData);
                    fileStream.Close();
                }
            }
            else
            {
                Console.WriteLine("Not Resize, just move to the Cover-Front. 不改变图像大小，仅移动到封面");
                newPicture = PictureInfo.fromBinaryData(pic.PictureData, PictureInfo.PIC_TYPE.Front);
            }

            // 清除全部图片
            theTrack.EmbeddedPictures.Clear();
            // 重新添加封面
            theTrack.EmbeddedPictures.Add(newPicture);

            pictureChanged = true;
        }
        else
        {
            Console.WriteLine("Don't need do anything for Picture. 不需要对图片做任何处理");
        }
    }
    if (!hasID3v2_3 || pictureChanged)
    {
        Settings.ID3v2_tagSubVersion = 3;
        string newFilename;
        pictureChanged = true;
        if (saveModifiedAudio &&
            (newFilename = Path.Combine(file.DirectoryName, $"{Path.GetFileNameWithoutExtension(file.Name)}{debugPostfixPicture}{file.Extension}")) != theTrack.Path)
        {
#if DEBUG
            Console.WriteLine("储存到新文件 {0}", newFilename);
#endif
            //储存到新文件
            //newFilename = Path.Combine(file.DirectoryName, $"{Path.GetFileNameWithoutExtension(file.Name)}{debugPostfixPicture}{file.Extension}");
            theTrack.SaveTo(newFilename);
        }
        else
        {
#if DEBUG
            Console.WriteLine("保存到原始文件");
#endif
            //保存到原始文件
            theTrack.Save();
        }
    }
    Console.WriteLine();
}

static string GetExtFromMimeType(string MimeType)
{
    return MimeType
        .Split('/', StringSplitOptions.RemoveEmptyEntries)
        .Last();
}

public class HandlerOptions
{
    public uint MaxWidth { get; set; }
    public uint MaxHeight { get; set; }
    public bool KeepRatio { get; set; }
    public byte JpegQuality { get; set; }
    public bool ExtractOriginalPicture { get; set; }
    public string DebugPostfixPicture { get; set; }
    public bool SaveModifiedAudio { get; set; }
    public bool ExtractModifiedPicture { get; set; }
}

public class HandlerOptionsBinder : BinderBase<HandlerOptions>
{
    private readonly Option<uint> _maxWidth;
    private readonly Option<uint> _maxHeight;
    private readonly Option<bool> _keepRatio;
    private readonly Option<byte> _jpegQuality;
    private readonly Option<bool> _extractOriginalPicture;
    private readonly Option<string> _debugPostfixPicture;
    private readonly Option<bool> _saveModifiedAudio;
    private readonly Option<bool> _extractModifiedPicture;

    public HandlerOptionsBinder(Option<uint> maxWidth, Option<uint> maxHeight, Option<bool> keepRatio, Option<byte> jpegQuality, Option<bool> extractOriginalPicture, Option<string> debugPostfixPicture, Option<bool> saveModifiedAudio, Option<bool> extractModifiedPicture)
    {
        _maxWidth = maxWidth;
        _maxHeight = maxHeight;
        _keepRatio = keepRatio;
        _jpegQuality = jpegQuality;
        _extractOriginalPicture = extractOriginalPicture;
        _debugPostfixPicture = debugPostfixPicture;
        _saveModifiedAudio = saveModifiedAudio;
        _extractModifiedPicture = extractModifiedPicture;
    }

    protected override HandlerOptions GetBoundValue(BindingContext bindingContext) =>
        new()
        {
            MaxWidth = bindingContext.ParseResult.GetValueForOption(_maxWidth),
            MaxHeight = bindingContext.ParseResult.GetValueForOption(_maxHeight),
            KeepRatio = bindingContext.ParseResult.GetValueForOption(_keepRatio),
            JpegQuality = bindingContext.ParseResult.GetValueForOption(_jpegQuality),
            ExtractOriginalPicture = bindingContext.ParseResult.GetValueForOption(_extractOriginalPicture),
            DebugPostfixPicture = bindingContext.ParseResult.GetValueForOption(_debugPostfixPicture),
            SaveModifiedAudio = bindingContext.ParseResult.GetValueForOption(_saveModifiedAudio),
            ExtractModifiedPicture = bindingContext.ParseResult.GetValueForOption(_extractModifiedPicture),
        };
}