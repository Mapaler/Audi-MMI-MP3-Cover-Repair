// See https://aka.ms/new-console-template for more information
using ATL;
using ATL.AudioData;
using Audi_MMI_MP3_Cover_Repair;
using Audi_MMI_MP3_Cover_Repair.Resources;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;

const int defaultMaxWidth = 480;
const int defaultMaxHeight = 480;
const int defaultJpegQuality = 90;
const byte JpegQualityMax = 100;
const byte JpegQualityMin = 0;

Assembly assembly = Assembly.GetExecutingAssembly();
//获取JPEG的编码器信息
ImageCodecInfo? jpegEncoder = ImageCodecInfo.GetImageEncoders().ToList().Find(codec =>
{
    return codec.FormatID == ImageFormat.Jpeg.Guid;
});

Argument<FileSystemInfo[]> pathsArgument = new("paths")
{
    Description = Resource.DES_Paths
};

Option<uint> maxWidthOption = new("--max-width", "-W")
{
    Description = Resource.DES_MaxWidth,
    DefaultValueFactory = parseResult => defaultMaxWidth
};

Option<uint> maxHeightOption = new("--max-height", "-H")
{
    Description = Resource.DES_MaxHeight,
    DefaultValueFactory = parseResult => defaultMaxHeight
};

Option<bool> keepRatioOption = new("--keep-ratio")
{
    Description = Resource.DES_KeepRatio,
    DefaultValueFactory = parseResult => true
};

Option<byte> qualityOption = new("--jpeg-quality", "-q")
{
    Description = Resource.DES_JpegQuality,
    DefaultValueFactory = parseResult => defaultJpegQuality
};
qualityOption.Validators.Add(result =>
{
    byte quality = result.GetValue(qualityOption);
    if (quality > JpegQualityMax || quality < JpegQualityMin)
    {
        result.AddError(string.Format(Resource.ERR_Range_JpegQuality, JpegQualityMin, JpegQualityMax));
    }
});

Option<bool> extractOriginalPictureOption = new("--extract-original-picture")
{
    Description = Resource.DES_ExtractOriginalPicture
};

Option<bool> saveModifiedAudioOption = new("--save-new-audio")
{
    Description = Resource.DES_SaveNewAudio
};

Option<bool> extractModifiedPictureOption = new("--extract-new-picture")
{
    Description = Resource.DES_ExtractNewPicture
};

Option<string> debugPostfixOption = new("--postfix")
{
    Description = Resource.DES_Postfix,
    DefaultValueFactory = parseResult => "-id3"
};
debugPostfixOption.Validators.Add(result =>
{
    string? postfix = result.GetValue(debugPostfixOption);
    if (result.GetValue(saveModifiedAudioOption) && string.IsNullOrWhiteSpace(postfix))
    {
        result.AddError(Resource.ERR_Empty_Postfix);
    }
});

RootCommand rootCommand = new(string.Format(Resource.DES_Soft, defaultMaxWidth, defaultMaxHeight));

rootCommand.Options.Add(maxWidthOption);
rootCommand.Options.Add(maxHeightOption);
rootCommand.Options.Add(keepRatioOption);
rootCommand.Options.Add(qualityOption);
rootCommand.Options.Add(extractOriginalPictureOption);
rootCommand.Options.Add(saveModifiedAudioOption);
rootCommand.Options.Add(extractModifiedPictureOption);
rootCommand.Options.Add(debugPostfixOption);
rootCommand.Arguments.Add(pathsArgument);

rootCommand.SetAction(parseResult =>
{
    FileSystemInfo[] paths = parseResult.GetValue(pathsArgument);
    if (paths.Length < 1)
    {
        ConsoleHelper.WriteError(Resource.ERR_Empty_Paths);
        ConsoleHelper.WriteColorLine(ConsoleColor.Yellow, Resource.HELP_ShowHelp, string.Format("{0} -h", assembly.GetName().Name, "-h"));
        return 1;
    }
    if (jpegEncoder == null)
    {
        ConsoleHelper.WriteError(Resource.ERR_NoJpegEncoder);
        return 10;
    }
    uint maxWidth = parseResult.GetValue(maxWidthOption);
    uint maxHeight = parseResult.GetValue(maxHeightOption);
    bool keepRatio = parseResult.GetValue(keepRatioOption);
    byte jpegQuality = parseResult.GetValue(qualityOption);
    bool extractOriginalPicture = parseResult.GetValue(extractOriginalPictureOption);
    string debugPostfixPicture = parseResult.GetValue(debugPostfixOption);
    bool saveModifiedAudio = parseResult.GetValue(saveModifiedAudioOption);
    bool extractModifiedPicture = parseResult.GetValue(extractModifiedPictureOption);
    ReadFiles(paths, maxWidth, maxHeight, keepRatio, jpegQuality, extractOriginalPicture, debugPostfixPicture, saveModifiedAudio, extractModifiedPicture, jpegEncoder);
    return 0;
});

ParseResult parseResult = rootCommand.Parse(args);
return parseResult.Invoke();

static void ReadFiles(FileSystemInfo[] paths,
    uint maxWidth,
    uint maxHeight,
    bool keepRatio,
    byte jpegQuality,
    bool extractOriginalPicture,
    string debugPostfixPicture,
    bool saveModifiedAudio,
    bool extractModifiedPicture,
    ImageCodecInfo jpegEncoder
    )
{
    ConsoleHelper.WriteColorLine(ConsoleColor.Cyan, Resource.INFO_ListFile);
    List<FileInfo> files = [];
    foreach (FileSystemInfo path in paths)
    {
        if (!path.Exists)
        {
            ConsoleHelper.WriteError(Resource.ERR_NotExist, Resource.WORD_Path);
            continue;
        }
        if (path.Attributes.HasFlag(FileAttributes.Directory))
        {
            files.AddRange(TraversingDirectory((DirectoryInfo)path));
        }
        else if (path.Extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase))
        {
            files.Add((FileInfo)path);
        }
    }

    int currentIndex = 0;
    int maxCount = files.Count;
    foreach (FileInfo file in files)
    {
        currentIndex++;
        
        ConsoleHelper.WriteColorLine(ConsoleColor.White, ConsoleColor.DarkCyan, Resource.PREP_Colon,
            Resource.INFO_Processing,
            string.Format(Resource.PREP_Percent, currentIndex, maxCount, (double)currentIndex / (double)maxCount));
        ConvertMetadata(file, maxWidth, maxHeight, keepRatio, jpegQuality, extractOriginalPicture, debugPostfixPicture, saveModifiedAudio, extractModifiedPicture, jpegEncoder);
    }
}
//递归遍历文件夹
static List<FileInfo> TraversingDirectory(DirectoryInfo directory)
{
    List<FileInfo> files = [];
    ConsoleHelper.WriteColorLine(ConsoleColor.Yellow, Resource.PREP_Colon, Resource.INFO_Traversing, directory.FullName);
    
    try
    {
        files.AddRange([.. directory.GetFiles("*.mp3")]);
    }
    catch (UnauthorizedAccessException ex)
    {
        ConsoleHelper.WriteWarning(Resource.ERR_NoPermission, Resource.PREP_Colon, Resource.WORD_File, directory.FullName);
        ConsoleHelper.WriteWarning(ex.Message);
    }
    catch (DirectoryNotFoundException ex)
    {
        ConsoleHelper.WriteWarning(Resource.ERR_NotExist, directory.FullName);
        ConsoleHelper.WriteWarning(ex.Message);
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
        ConsoleHelper.WriteWarning(Resource.ERR_NoPermission, Resource.PREP_Colon, Resource.WORD_SubDirectory, directory.FullName);
        ConsoleHelper.WriteWarning(ex.Message);
    }
    catch (DirectoryNotFoundException ex)
    {
        ConsoleHelper.WriteWarning(Resource.ERR_NotExist, directory.FullName);
        ConsoleHelper.WriteWarning(ex.Message);
    }
    return files;
}

static void ConvertMetadata(FileInfo file,
    uint maxWidth,
    uint maxHeight,
    bool keepRatio,
    byte jpegQuality,
    bool extractOriginalPicture,
    string debugPostfixPicture,
    bool saveModifiedAudio,
    bool extractModifiedPicture,
    ImageCodecInfo jpegEncoder)
{
#if DEBUG
    extractOriginalPicture = true;
    saveModifiedAudio = true;
    extractModifiedPicture = true;
#endif

    ConsoleHelper.WriteColorLine(ConsoleColor.White, Resource.PREP_Colon, Resource.INFO_SongFile, file.FullName);
    //读取音乐文件
    Track theTrack = new(file.FullName);

    ConsoleHelper.WriteColorLine(ConsoleColor.White, Resource.PREP_Colon, Resource.INFO_SongTitle, theTrack.Title);

    //是否有 ID3v2.3
    bool hasID3v2_3 = theTrack.MetadataFormats.Any(format => format.Name == "ID3v2.3");
    bool pictureChanged = false;

    if (theTrack.EmbeddedPictures.Count == 0)
    {
        ConsoleHelper.WriteColorLine(ConsoleColor.Yellow, Resource.ERR_NoEmbedPicture);
    }
    else
    {
        PictureInfo pic = theTrack.EmbeddedPictures.First();
        Image image = Image.FromStream(new MemoryStream(pic.PictureData));
        if (extractOriginalPicture)
        {
            //保存图片原始数据
            string coverFileName = Path.Combine(file.DirectoryName, $"{Path.GetFileNameWithoutExtension(file.Name)}-{pic.PicType}.{GetExtFromMimeType(pic.MimeType)}");
            File.WriteAllBytes(coverFileName, pic.PictureData);
        }

        ConsoleHelper.WriteColorLine(ConsoleColor.White, Resource.PREP_Colon, Resource.INFO_PictureType, pic.PicType);
        ConsoleHelper.WriteColorLine(ConsoleColor.White, Resource.PREP_Colon, Resource.INFO_PictureSize, string.Format(Resource.PREP_Size, image.Width, image.Height));

        PictureInfo? newPicture = null;
        if (pic.PicType != PictureInfo.PIC_TYPE.Front)
        {
            pictureChanged = true;
            ConsoleHelper.WriteColorLine(ConsoleColor.Cyan, Resource.INFO_MoveToFront);
            newPicture = PictureInfo.fromBinaryData(pic.PictureData, PictureInfo.PIC_TYPE.Front);
        }
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

            ConsoleHelper.WriteColorLine(ConsoleColor.White, Resource.PREP_Colon, Resource.INFO_ResizeScale, string.Format(Resource.PREP_Ratio, scaleW, scaleH));
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
                File.WriteAllBytes(coverResizeFileName, newPicture.PictureData);
            }
        }
        if (newPicture != null)
        {
            // 清除全部图片
            theTrack.EmbeddedPictures.Clear();
            // 重新添加封面
            theTrack.EmbeddedPictures.Add(newPicture);
            pictureChanged = true;
        }
        else
        {
            ConsoleHelper.WriteColorLine(ConsoleColor.Cyan, Resource.INFO_NoNeedProcessImage);
        }
    }
    if (!hasID3v2_3 || pictureChanged)
    {
        Settings.ID3v2_tagSubVersion = 3;
        string newFilename = string.Empty;
        if (saveModifiedAudio)
        { //计算新文件的路径
            newFilename = Path.Combine(file.DirectoryName, $"{Path.GetFileNameWithoutExtension(file.Name)}{debugPostfixPicture}{file.Extension}");
        }
        if (!hasID3v2_3)
        {
            ConsoleHelper.WriteColorLine(ConsoleColor.Yellow, Resource.INFO_ChangeToID3v23);
        }
        if (saveModifiedAudio &&
            newFilename != theTrack.Path) //新文件和旧文件一样时还是使用普通储存
        {
#if DEBUG
            Console.WriteLine("储存到新文件 {0}", newFilename);
#endif
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