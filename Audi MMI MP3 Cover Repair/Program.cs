// See https://aka.ms/new-console-template for more information
using IdSharp.Tagging.ID3v2;
using IdSharp.Tagging.ID3v2.Frames;

foreach (var item in args)
{
#if DEBUG
    Console.WriteLine("参数: {0}", item);
#endif
    ID3v2Tag id3v2 = new ID3v2Tag(item);
    Console.WriteLine(string.Format("Title:     {0}", id3v2.Title));
    Console.WriteLine(string.Format("Pictures:  {0}", id3v2.PictureList.Count));
    IAttachedPicture attachedPicture = id3v2.PictureList[0];
    
    FileStream fileStream = new FileStream(attachedPicture.Description + "." + attachedPicture.PictureExtension, FileMode.OpenOrCreate, FileAccess.Write);
    fileStream.Write(attachedPicture.PictureData);
}