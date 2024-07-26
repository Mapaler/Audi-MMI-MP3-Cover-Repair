// See https://aka.ms/new-console-template for more information
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.CommandLine;
using ATL.AudioData;
using ATL;
using System.IO;

var filesArgument = new Argument<FileInfo[]?>
    (name: "files",
    description: "需要处理的多个 MP3 文件。\nMP3 files that need to be modified.");

var maxWidthOption = new Option<uint>(
    name: "--max-width",
    description: "新图片最大宽度。\nThe max Width of the new JPEG file.",
    getDefaultValue: () => 480);
maxWidthOption.AddAlias("-W");

var maxHeightOption = new Option<uint>(
    name: "--max-height",
    description: "新图片最大高度。\nThe max Height of the new JPEG file.",
    getDefaultValue: () => 480);
maxHeightOption.AddAlias("-H");

var keepRatioOption = new Option<bool>(
    name: "--keep-ratio",
    description: "保持图片宽高比。\nMaintain the aspect ratio of the image.",
    getDefaultValue: () => true);

var qualityOption = new Option<byte>(
    name: "--quality",
    description: "新 JPEG 文件的压缩质量。\nThe compressed quality of the new JPEG file.",
    getDefaultValue: () => 90);
qualityOption.AddAlias("-q");

var savePictureOption = new Option<bool>(
    name: "--save-picture",
    description: "储存原始图片文件。\nSave the Original Picture to file.");
savePictureOption.AddAlias("-save");

var rootCommand = new RootCommand($"修复奥迪汽车多媒体平台 MP3 歌曲不能显示封面的问题，将 MP3 文件的图片全部缩小到不大于 480x480 ，位置调整到封面(Cover-Front)。{Environment.NewLine}Fixed the issue that the cover picture of MP3 songs on Audi car MMI could not be displayed, and all the pictures in MP3 files were reduced to no larger than 480x480, and the position was adjusted to Cover-Front.");
rootCommand.AddOption(maxWidthOption);
rootCommand.AddOption(maxHeightOption);
rootCommand.AddOption(keepRatioOption);
rootCommand.AddOption(qualityOption);
rootCommand.AddOption(savePictureOption);
rootCommand.Add(filesArgument);

rootCommand.SetHandler(ReadFile,
    filesArgument, maxWidthOption, maxHeightOption, keepRatioOption, qualityOption, savePictureOption);

await rootCommand.InvokeAsync(args);

static string GetExtFromMimeType(string MimeType)
{
    return MimeType
        .Split('/', StringSplitOptions.RemoveEmptyEntries)
        .Last();
}
static void ReadFile(FileInfo[] files, uint maxWidth, uint maxHeight, bool keepRatio, byte quality, bool savePicture)
{
    //获取JPEG的编码器信息
    ImageCodecInfo? jpegEncoder = ImageCodecInfo.GetImageDecoders().ToList().Find(codec =>
    {
        return codec.FormatID == ImageFormat.Jpeg.Guid;
    });

    uint currentIndex = 0;
    foreach (FileInfo file in files)
    {
        currentIndex++;
        Console.WriteLine("Index:\t{0}/{1}", currentIndex, files.Length);
        if (!file.Exists) //如果文件不存在，则直接跳过
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("Error:\tFile is not Exists.文件不存在。");
            Console.Error.WriteLine("\t{0}", file);
            Console.ResetColor();
            Console.WriteLine();
            continue;
        }
        // Initialize with a file path
        Track theTrack = new Track(file.FullName);

        // Works the same way on any supported format (MP3, FLAC, WMA, SPC...)
        Console.WriteLine("Title:\t" + theTrack.Title);

        // Get picture list
        if (theTrack.EmbeddedPictures.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("Error:\tFile don't have any Picture.文件里没有内嵌图片。");
            Console.Error.WriteLine("\t{0}", file);
            Console.ResetColor();
            Console.WriteLine();
            continue;
        }
        else
        {
            PictureInfo pic = theTrack.EmbeddedPictures.First();
            Image image = Image.FromStream(new MemoryStream(pic.PictureData));
#if DEBUG
            savePicture = true;
#endif
            if (savePicture)
            {
                //保存图片原始数据
                string coverFileName = Path.Combine(file.DirectoryName, $"{Path.GetFileNameWithoutExtension(file.Name)}-{pic.PicType}.{GetExtFromMimeType(pic.MimeType)}");
                FileStream fileStream = new(coverFileName, FileMode.OpenOrCreate, FileAccess.Write);
                fileStream.Write(pic.PictureData);
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

                    Console.WriteLine(string.Format("Resize Scale:\t{0}:{1}", scaleW, scaleH));
                    Bitmap newImage = new Bitmap(image, new Size(newWidth, newHeight));

                    //创建JPEG压缩的参数
                    EncoderParameters parameters = new(1);
                    parameters.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);

                    //在内存中压缩转存一遍
                    MemoryStream jpegMemory = new();
                    newImage.Save(jpegMemory, jpegEncoder, parameters);

                    newPicture = PictureInfo.fromBinaryData(jpegMemory.ToArray(), PictureInfo.PIC_TYPE.Front);
#if DEBUG
                    {
                        //保存新的封面图片
                        string coverResizeFileName = Path.Combine(file.DirectoryName, $"{Path.GetFileNameWithoutExtension(file.Name)}-{newPicture.PicType}-resize.{GetExtFromMimeType(newPicture.MimeType)}");
                        FileStream fileStream = new(coverResizeFileName, FileMode.OpenOrCreate, FileAccess.Write);
                        fileStream.Write(newPicture.PictureData);
                    }
#endif
                }
                else
                {
                    Console.WriteLine(string.Format("Resize Scale:\tNot Resize, just move to the Front-Cover. 不改变图像大小，仅移动到封面"));
                    newPicture = PictureInfo.fromBinaryData(pic.PictureData, PictureInfo.PIC_TYPE.Front);
                }

                // 清除全部图片
                theTrack.EmbeddedPictures.Clear();

                // 添加封面
                theTrack.EmbeddedPictures.Add(newPicture);

#if DEBUG
                //复制一份新的mp3来修改封面
                string newFilename = Path.Combine(file.DirectoryName, $"{Path.GetFileNameWithoutExtension(file.Name)}-id3.{file.Extension}");
                file.CopyTo(newFilename, true);
                //将元数据复制到新文件
                Track theCopyTrack = new Track(newFilename);
                theCopyTrack.Remove(MetaDataIOFactory.TagType.ANY); //去除新文件里的所有内容
                theTrack.CopyMetadataTo(theCopyTrack);
                //保存新文件
                theCopyTrack.Save();
#else
                // Save modifications on the disc
                theTrack.Save();
#endif
            }
            else
            {
                Console.WriteLine(string.Format("Don't need do anything for Picture. 不需要对图片做任何处理"));
            }
            Console.WriteLine();
        }
    }
}
