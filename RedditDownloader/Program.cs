using System;
using System.Diagnostics;
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
        //Amount of work that is left
        public static int WorkLeft;
        //The Directory to download the files into
        public static string DownloadDir;
        //If images flagged as "NSFW" should be allowed for the download.
        public static bool AllowNsfw;

        /// <summary>
        /// Main Program Call
        /// </summary>
        /// <param name="args">Start arguments</param>
        [STAThread]
        static void Main(string[] args)
        {
            #region UserInput

            //Check if NSFW Images are allowed.
            while (true)
            {
                Console.WriteLine("Do you want to download Images tagged NSFW Images?");
                var key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.Y:
                        AllowNsfw = true;
                        break;
                    case ConsoleKey.N:
                        break;
                    case ConsoleKey.Enter:
                        break;
                    default:
                        Console.WriteLine("Unexpected input detected please try again.");
                        continue;
                }
                break;
            }

            //Get our Subreddit
            Subreddit subreddit;
            while (true)
            {
                var reddit = new Reddit();
                Console.Clear();
                Console.WriteLine("Please enter the subs name");
                var sub = Console.ReadLine();

                try
                {
                    subreddit = reddit.GetSubreddit(sub);
                    break;
                }
                catch
                {
                    Console.WriteLine("Could not get that sub. Press any key to continue;");
                    Console.ReadKey();
                    continue;
                }
            }

            //Get the to download images
            int todownloadImages;
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Please enter how many Images of the Sub you'd like to obtain:");
                int.TryParse(Console.ReadLine(), out todownloadImages);
                if (todownloadImages == 0)
                {
                    Console.WriteLine("You need to download at least one image\n" +
                                      "Press any key to try again");
                    Console.ReadKey();
                    continue;
                }

                break;
            }

            //Select our Folder
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Please select the folder to download the images into");
                var folderBrowserDialog1 = new FolderBrowserDialog();
                // Show the FolderBrowserDialog.
                DialogResult result = folderBrowserDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    DownloadDir = folderBrowserDialog1.SelectedPath;
                    break;
                }
                Console.WriteLine("You need to select a folder to download stuff into\n" +
                                  "Press any key to try again");
                Console.ReadKey();
            }

            #endregion

            #region DownloadImages
            WorkLeft = todownloadImages;

            Console.WriteLine($"Starting download of {todownloadImages} Images from {subreddit.Name}");
            bool doingWork = true;
            //This could be considered our "uithread" to update stuff
            var thread = new Thread(() =>
            {
                Console.Clear();
                while (doingWork)
                {
                    var donework = todownloadImages - WorkLeft;
                    if (donework == 0) continue;
                    var percentile = todownloadImages / 100;
                    Console.SetCursorPosition(0, 0);
                    Console.CursorVisible = false;
                    var progressBar = GetProgress(((float)donework / (float)todownloadImages * 100), 100);
                    Console.WriteLine("Overall download progress of files\n" +
                                      $"{WorkLeft} Files left\n" +
                                      $"{progressBar}\n");
                }
            });
            thread.Start();
            //Parallel foreach through all pots
            Parallel.ForEach(subreddit.Posts.Take(todownloadImages), (post) =>
            {
                if (post.NSFW && !AllowNsfw) return;
                if (post.Thumbnail == null) return;
                if (!post.Url.ToString().Contains(".png") && !post.Url.ToString().Contains(".jpg") &&
                    !post.Url.ToString().Contains(".gif")) return;

                DownloadImage(post.Url.ToString(), out var wasEmpty);

            });

            doingWork = false;
            //Rejoin thread for UI Updates.
            thread.Join();
            #endregion

            #region AskIfUserWantsRerun
            Console.Clear();
            Console.WriteLine("Program finished download.");
            while (true)
            {
                Console.WriteLine("Do you want to run again? (y\\N)");
                var key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.Y:
                        Console.WriteLine("Rerunning Program");
                        Main(args);
                        break;
                    case ConsoleKey.N:
                        break;
                    case ConsoleKey.Enter:
                        break;
                    default:
                        Console.WriteLine("Unexpected input detected please try again.");
                        continue;
                }
                break;
            }
            

            #endregion
        }

        /// <summary>
        /// Displays Progress
        /// </summary>
        /// <param name="currentProgress"></param>
        /// <returns></returns>
        public static string GetProgress(double currentProgress, int width)
        {
            var progressBar = string.Empty;

            for (int i = 0; i < width; i++)
            {
                if (currentProgress * 100 / width > 1)
                {
                    progressBar += "█";
                    currentProgress--;
                    continue;
                }

                progressBar += "░";
            }

            progressBar += string.Empty;

            return progressBar;
        }

        /// <summary>
        /// Downloads an image via the ImageUrl and returns if it existed at all.
        /// </summary>
        /// <param name="imageUrl"></param>
        /// <param name="wasEmpty"></param>
        /// <returns></returns>
        public static void DownloadImage(string imageUrl, out bool wasEmpty)
        {
            using (var webClient = new WebClient())
            {
                try
                {
                    wasEmpty = false;
                    Guid imageNumber = Guid.NewGuid();
                    //Generate a really unique GUID found an issue where we could run in here protentioally.
                    while (true)
                    {
                        if (File.Exists(DownloadDir + "\\" + imageNumber + ".png"))
                            continue;
                        break;
                    }

                    webClient.DownloadFile(imageUrl, DownloadDir + "\\" + imageNumber + ".png");
                    FileInfo fi = new FileInfo(DownloadDir + "\\" + imageNumber + ".png");
                    var size = fi.Length;
                    if (size <= 0)
                    {
                        wasEmpty = true;
                        fi.Delete();
                    }
                    WorkLeft--;
                }
                catch (Exception)
                {
                    //Console.WriteLine(ex.Message);
                    wasEmpty = true;
                }
            }
        }
    }
}
