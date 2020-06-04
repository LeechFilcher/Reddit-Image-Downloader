using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RedditSharp;
using RedditSharp.Things;

namespace RedditDownloader
{
    class Program
    {
        public static int WorkLeft;
        public static string DownloadDir;
        public static bool AllowNsfw;
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Allow Nsfw Images? (y/N)");
            if (Console.ReadKey().Key == ConsoleKey.Y)
            {
                AllowNsfw = true;
            }


            #region getSub
            getSub:
            var reddit = new Reddit();
            Console.Clear();
            Console.WriteLine("Please enter the subs name");
            var sub = Console.ReadLine();
            Subreddit subreddit;
            try
            {
                subreddit = reddit.GetSubreddit(sub);
            }
            catch
            {
                Console.WriteLine("Could not get that sub. Press any key to continue;");
                Console.ReadKey();
                goto getSub;
            }
            #endregion
            
            #region getImageCount
            getImageCount:
            Console.Clear();
            Console.WriteLine("Please enter how many Images of the Sub you'd like to obtain:");
            int.TryParse(Console.ReadLine(), out var todownloadImages);
            if (todownloadImages == 0)
            {
                Console.WriteLine("You need to download at least one image\n" +
                                  "Press any key to try again");
                Console.ReadKey();
                goto getImageCount;
            }
            #endregion

            #region SelectFolder
            selectFolder:
            Console.Clear();
            Console.WriteLine("Please select the folder to download the images into");
            var folderBrowserDialog1 = new FolderBrowserDialog();
            // Show the FolderBrowserDialog.
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if( result == DialogResult.OK )
            {
                DownloadDir = folderBrowserDialog1.SelectedPath;
            }
            else
            {
                Console.WriteLine("You need to select a folder to download stuff into\n" +
                                  "Press any key to try again");
                Console.ReadKey();
                goto selectFolder;
            }
            #endregion

            #region DownloadImages
            WorkLeft = todownloadImages;
            Console.WriteLine($"Starting download of {todownloadImages} Images from {subreddit.Name}");
            var thread = new Thread(() =>
            {
                foreach (var post in subreddit.Posts.Take(todownloadImages))
                {
                    if (post.NSFW && !AllowNsfw) continue;
                    if (post.Thumbnail == null) continue;
                    
                    var prm = new Program();
                    Task t = new Task(() => { 
                        prm.DownloadImageTask(post.Url.AbsoluteUri, out var wasEmpty); 
                        });
                    t.Start();
                }
            });
            thread.Start();

            do
            {
                Console.Clear();
                Console.WriteLine("Downloads left: " + WorkLeft);
                Thread.Sleep(200);
            } while (WorkLeft > 0 && thread.IsAlive);
            #endregion

            Console.Clear();
            Console.WriteLine("Downloads left 0");
            Console.WriteLine("Program is finished, do you want to run again?\n (y\\N)");
            if (Console.ReadKey().Key == ConsoleKey.Y)
            {
                goto getSub;
            }
        }

        #region ImageDownloadTask
        public Task DownloadImageTask(string imageUrl, out bool wasEmpty)
        {
            
            using (var webClient = new WebClient())
            {
                try
                {
                    wasEmpty = false;
                    var imageNumber = Guid.NewGuid();
                    webClient.DownloadFileAsync(new Uri(imageUrl), DownloadDir + "\\" + imageNumber + ".png");
                    FileInfo fi = new FileInfo(DownloadDir + "\\" + imageNumber + ".png");
                    var size = fi.Length;
                    if (size <= 0)
                    {
                        wasEmpty = true;
                        fi.Delete();
                    }
                    WorkLeft--;
                    return Task.FromResult(0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    wasEmpty = true;
                    return Task.FromResult(1);
                }
            }
        }
        #endregion
    }
}
