using MPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
                const int headings = 3;
                const int recivedHeading = 4;
                const int killYourself = 5;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;



                if (icomm.Rank==0) //if Master
                {

                    int slavesNumber = icomm.Size-1;
                    Console.WriteLine("Sajz =" + slavesNumber);

                    List<string> addressList = new List<string>();
                    List<string> resultsList = new List<string>();
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
                    if (slavesNumber < 1)
                    {
                        Console.WriteLine("No more slaves");
                        Console.WriteLine("Wyswietlam rezultaty");
                        foreach (var item in resultsList)
                        {
                            Console.WriteLine(item);
                        }
                        return;
                    }
                    /*
                    foreach (var item in addressList) //show address lists
                    {
                        Console.WriteLine(item);
                    }*/

                    //od tad petla

                    while (true)
                    {

                        int tag = icomm.Probe(Communicator.anySource, Communicator.anyTag).Tag; //get incoming tag
                        int slaveRank = icomm.Probe(Communicator.anySource, Communicator.anyTag).Source; //get icoming rank
                        switch (tag)
                        {
                            case requestURL: //request url
                                icomm.Receive<string>(Communicator.anySource, Communicator.anyTag); // recive request
                                if (addressList.Count > 0)
                                {
                                    Console.WriteLine("Master passing url to slave" +slaveRank);
                                    icomm.Send<string>(addressList[0], slaveRank, passURL);
                                    Console.WriteLine("Master wyslal url "+ addressList[0]+" to slave number"+ slaveRank);
                                    addressList.RemoveAt(0);
                                }
                                else
                                {
                                    Console.WriteLine("url table count = " + addressList.Count);
                                    Console.WriteLine("Wysylanie kill do slava");
                                    icomm.Send<string>(String.Empty, slaveRank, killYourself);
                                    slavesNumber--;
                                    Console.WriteLine("slaves number" + slavesNumber);
                                }
                                break;
                            case headings:
                                Console.WriteLine("Master otrzymuje wynik od... "+slaveRank);
                                string[] incomingArr = icomm.Receive<string[]>(Communicator.anySource, Communicator.anyTag); // recive request
                                Console.WriteLine("Master otrzymal wynik od... " + slaveRank);
                                Console.WriteLine("------przepisywanie incominga array do list-------");
                                foreach (var item in incomingArr)
                                {
                                    Console.WriteLine("Wpisano "+item);
                                    resultsList.Add(item);
                                }
                                Console.WriteLine("Master wysyla potwierdzenie otrzymania do " + slaveRank);

                                icomm.Send<string>(String.Empty, slaveRank, recivedHeading);
                                break;
                            //case killYourself:
                              //  slavesNumber--;
                               // Console.WriteLine("slaves number" + slavesNumber);
                               // break;
                            default:
                                break;
                        }
                        if (slavesNumber < 1)
                        {
                            Console.WriteLine("No more slaves wyswietlam wyniki");
                            Thread.Sleep(4000);
                            Console.WriteLine("------------Wyniki----------");
                            foreach (var item in resultsList)
                            {
                                Console.WriteLine(item);
                            }
                            return;
                        }
                    }
                    





                }
                else //if Slave
                {
                    Console.WriteLine("Slave nr " + icomm.Rank+ " wyslal rządanie o url do Mastera");
                    icomm.Send<string>(String.Empty, master, requestURL); //requesting url address from master
                    //odtad bedzie petla

                    while (true)
                    {
                        Console.WriteLine("Slave nr " + icomm.Rank + " nasluchuje odpowiedz od Mastera");
                        int incomingTag = icomm.Probe(master, Communicator.anyTag).Tag;
                        string message = icomm.Receive<string>(master, Communicator.anyTag);
                        Console.WriteLine("Slave nr " + icomm.Rank + " przyjal odpowiedz od mastera z tagiem: "+ incomingTag);
                        
                        switch (incomingTag)
                        {
                            case passURL:
                                Console.WriteLine("Slave nr "+ icomm.Rank+ " otrzymal url:");
                                Console.WriteLine(message);
                                List<string> allResults = new List<string>();
                                using (WebClient client = new WebClient())
                                {
                                    client.Encoding = System.Text.Encoding.UTF8;
                                    Console.WriteLine(System.Environment.NewLine + "Downloading and Reading from " + message);
                                    string htmlCode = client.DownloadString(message);
                                    // SearchForHeadings(htmlCode);

                                    String[] separator = { "<h1>", "</h1>" };
                                    string pattern = "(<h1>(.*?)</h1>)|(<h2>(.*?)</h2>)";
                                    //string pattern = "(<title>(.*?)</title>)";
                                    MatchCollection matches;


                                    matches = Regex.Matches(htmlCode, pattern, RegexOptions.IgnoreCase);
                                    foreach (Match mt in matches)
                                    {
                                        //Console.WriteLine("Found '{0}' at position {1}.", mt.Value, mt.Index);
                                        allResults.Add(mt.Value.Substring(4, mt.Value.Length - 9));
                                    }
                                    string[] resultArr = allResults.ToArray();
                                    
                                    Console.WriteLine("Slave nr "+ icomm.Rank + " wysyla wynik do mastera..");
                                    icomm.Send<string[]>(resultArr, master, headings);
                                }
                                break;
                            case recivedHeading:
                                Console.WriteLine("Slave nr: "+ icomm.Rank +" otrzymal potwierdzenie i wysyla prosbe do mastera o url...");
                                icomm.Send<string>(String.Empty, master, requestURL); //requesting url address from master
                                break;
                            case killYourself:
                               // icomm.Send<string>(String.Empty, master, killYourself); //requesting url address from master
                                Console.WriteLine("killed nr:"+icomm.Rank+" slave");
                                return;
                            default:
                                break;
                        }
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