using MPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//--------------------------------
/*W dowolnym języku programowania, używając dowolnej implementacji MPI napisz program realizujący poniższą funkcjonalność: Należy utworzyć plik
domains.txt, umieścić w pliku kilka adresów do stron internetowych, np:
https://wsb.zylowski.net/
https://wsb.zylowski.net/faq/
https://www.telegraph.co.uk/technology/6125914/How-20-popular-websites-looked-when-they-launched.html


Należy napisać program, który otworzy ten plik i dla każdej domeny:
Odwiedzi stronę internetową i pobierze do pamięci jej zawartość
Przeanalizuje treść strony i wypisze na ekran wszystkie nagłówki pierwszego i drugiego poziomu. (Zwartość tagów HTML <h1>, <h2> ) Np dla strony:
https://www.telegraph.co.uk/technology/6125914/How-20-popular-websites-looked-when-they-launched.html
Są to:
How 20 popular websites looked when they launched
From Google to youtube, from craigslist to flickr - how some of today's biggest sites looked back in the early days of their
existence.
Related Articles
Technology
Odwiedzanie i analiza strona ma zostać zrównoleglona. Np to master może odczytywać plik i delegować strony do otwarcia poszczególnym procesom.
Można też wykorzystać zrównolegloną pętlę.*/
//---------------------------------

namespace mpiLab2
{
    class Program
    {
        static void Main(string[] args)
        {
            using (new MPI.Environment(ref args))
            {
                Intracommunicator icomm = Communicator.world;
                const int master = 0;
                const int requestURL = 1;
                const int passURL = 2;




                if (icomm.Rank==0) //if Master
                {
                    List<string> addressList = new List<string>();
                    try // try to open file
                    {
                        using (StreamReader streamreader = new StreamReader("domains.txt"))
                        {
                            string line = streamreader.ReadLine();
                            while (line != null) // read lines until null line
                            {
                                addressList.Add(line); //save lines to List<string>
                                line = streamreader.ReadLine();
                            }
                        }
                    }
                    catch
                    {
                        Console.WriteLine("domains.txt - unable to open file or file doesn't exist");
                        return;
                    }

                    /*
                    foreach (var item in addressList) //show address lists
                    {
                        Console.WriteLine(item);
                    }*/

                    int tag =  icomm.Probe(Communicator.anySource, Communicator.anyTag).Tag; //get incoming tag
                    int slaveRank = icomm.Probe(Communicator.anySource, Communicator.anyTag).Source; //get icoming rank
                    icomm.Receive<string>(Communicator.anySource, Communicator.anyTag); // recive request


                    switch (tag)
                    {
                        case requestURL: //request url
                            if (addressList.Count>0)
                            {
                                Console.WriteLine("Master passing url to slave");
                                icomm.Send<string>(addressList[0], slaveRank, passURL);
                                addressList.RemoveAt(0);
                            }
                            break;
                        default:
                            break;
                    }





                }
                else //if Slave
                {
                    icomm.Send<string>(String.Empty, master, requestURL); //requesting url address from master

                    int incomingTag = icomm.Probe(master, Communicator.anyTag).Tag;
                    string message = icomm.Receive<string>(master, Communicator.anyTag);

                    switch (incomingTag)
                    {
                        case passURL:
                            Console.WriteLine("otrzymalem url:");
                            Console.WriteLine(message);



                            break;
                        default:
                            break;
                    }



                }
                

            }
        }
    }
}

/*        static string htmlCode;
        static String[] separator = { "<h1>", "</h1>" };
        static string pattern = "(<h1>(.*?)</h1>)|(<h2>(.*?)</h2>)";
        static MatchCollection matches;
        static List<string> allResults = new List<string>();

        //adresy
        static string[] htmls = {
            "https://wsb.zylowski.net/",
            "https://wsb.zylowski.net/faq/",
            "https://www.telegraph.co.uk/technology/6125914/How-20-popular-websites-looked-when-they-launched.html"
        };

        static void SearchForHeadings(string code)
        {
            matches = Regex.Matches(code, pattern, RegexOptions.IgnoreCase);
            foreach (Match mt in matches)
            {
                Console.WriteLine("Found '{0}' at position {1}.", mt.Value, mt.Index);
                allResults.Add(mt.Value.Substring(4, mt.Value.Length - 9));
            }
        }

        static void Main(string[] args)
        {
            using (WebClient client = new WebClient())
            {
                foreach (string html in htmls)
                {
                    Console.WriteLine(Environment.NewLine+"Downloading and Reading from " + html);
                    htmlCode = client.DownloadString(html);
                    SearchForHeadings(htmlCode);
                }
            }
            Console.ReadKey();
            Console.WriteLine(Environment.NewLine);
            foreach (string result in allResults)
                Console.WriteLine(result);
            Console.ReadKey();
        }
*/