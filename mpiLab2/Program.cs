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



//



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




namespace mpiLab2
{
    class Program
    {
        static void Main(string[] args)
        {
            using (new MPI.Environment(ref args))
            {
                Intracommunicator icomm = Communicator.world;

                //tags
                const int master = 0;
                const int requestURL = 1;
                const int passURL = 2;
                const int headings = 3;
                const int recivedHeading = 4;
                const int killYourself = 5;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;


                if (icomm.Rank==0) // Master
                {

                    int slavesNumber = icomm.Size-1;

                    List<string> addressList = new List<string>();
                    List<string> resultsList = new List<string>();

                    if (slavesNumber < 1)
                    {
                        Console.WriteLine("MASTER has no SLAVES to work...");
                        foreach (var item in resultsList)
                        {
                            Console.WriteLine(item);
                        }
                        return;
                    }

                    //open file and add all lines to: List<string> addressList
                    try
                    {
                        using (StreamReader streamreader = new StreamReader("domains.txt"))
                        {   
                            string line = streamreader.ReadLine();
                            while (line != null) 
                            {
                                addressList.Add(line);
                                line = streamreader.ReadLine();
                            }
                        }
                    }
                    catch
                    {
                        Console.WriteLine("domains.txt - unable to open file or file doesn't exist");
                        return;
                    }




                    // Master work loop
                    while (true)
                    {

                        int tag = icomm.Probe(Communicator.anySource, Communicator.anyTag).Tag; //get incoming tag
                        int slaveRank = icomm.Probe(Communicator.anySource, Communicator.anyTag).Source; //get icoming rank
                        switch (tag)
                        {
                            case requestURL: 
                                icomm.Receive<string>(Communicator.anySource, Communicator.anyTag); // recive request
                                Console.WriteLine("\nMASTER: recived URL request from SLAVE " + slaveRank);
                                if (addressList.Count > 0)
                                {
                                    Console.WriteLine("MASTER: is sending URL to SLAVE " + slaveRank);
                                    icomm.Send<string>(addressList[0], slaveRank, passURL);
                                    Console.WriteLine("MASTER: has just sent URL " + addressList[0] + " to SLAVE " + slaveRank);
                                    addressList.RemoveAt(0); //remove sent URL from: List<string> addressList
                                }
                                else
                                {

                                    Console.WriteLine("-----No more work... MASTER is sending KILL TAG to SLAVE " + slaveRank+"-----");
                                    icomm.Send<string>(String.Empty, slaveRank, killYourself);
                                    slavesNumber--;
                                }
                                break;
                            case headings:
                                Console.WriteLine("MASTER: is receiving output from SLAVE " + slaveRank);
                                string[] incomingArr = icomm.Receive<string[]>(Communicator.anySource, Communicator.anyTag);
                                Console.WriteLine("MASTER: has just received output from SLAVE " + slaveRank);
                                
                                //adding results to list
                                foreach (var item in incomingArr)
                                {
                                    resultsList.Add(item);
                                }
                                
                                Console.WriteLine("MASTER: is sending confirmation message to SLAVE " + slaveRank);
                                icomm.Send<string>(String.Empty, slaveRank, recivedHeading);
                                break;
                            default:
                                break;
                        }
                        if (slavesNumber < 1)
                        {
                            Console.WriteLine();
                            Thread.Sleep(2800);
                            Console.WriteLine("-------------Program output-------------");
                            Console.WriteLine();
                            foreach (var item in resultsList)
                            {
                                Console.WriteLine(item);
                            }
                            return;
                        }
                    }
                    
                }
                else // Slave work loop
                {
                    Console.WriteLine("SLAVE " + icomm.Rank + ": sends URL request to MASTER");
                    icomm.Send<string>(String.Empty, master, requestURL); 
                   

                    while (true)
                    {
                        Console.WriteLine("SLAVE " + icomm.Rank + ": is waiting for a response from MASTER");
                        int incomingTag = icomm.Probe(master, Communicator.anyTag).Tag;
                        string message = icomm.Receive<string>(master, Communicator.anyTag);
                        
                        switch (incomingTag)
                        {
                            case passURL:
                                Console.WriteLine("\nSLAVE " + icomm.Rank+ " : received URL:");
                                
                                List<string> allResults = new List<string>();
                                using (WebClient client = new WebClient())
                                {
                                    client.Encoding = System.Text.Encoding.UTF8;
                                    Console.WriteLine("SLAVE is downloading: " + message);
                                    string htmlCode = client.DownloadString(message);
                                    string pattern = "(<h1>(.*?)</h1>)|(<h2>(.*?)</h2>)";
                                    MatchCollection matches;
                                    Console.WriteLine("SLAVE " + icomm.Rank + ": is working now....");

                                    matches = Regex.Matches(htmlCode, pattern, RegexOptions.IgnoreCase);
                                    foreach (Match mt in matches)
                                    {

                                        string substring = mt.Value.Substring(4, mt.Value.Length - 9);

                                        while (substring.Contains(">") && substring.Contains("<"))
                                        {
                                            string temp = Regex.Replace(substring, "<.*?>", String.Empty);
                                            substring = temp;
                                        }
                                        allResults.Add(substring);
                                    }
                                    string[] resultArr = allResults.ToArray();
                                    
                                    Console.WriteLine("SLAVE " + icomm.Rank + ": is sending result to MASTER....");
                                    icomm.Send<string[]>(resultArr, master, headings);
                                }
                                break;
                            case recivedHeading:
                                Console.WriteLine("\nSLAVE " + icomm.Rank + ": has received delivery confirmation and is requesting another URL now...");
                                icomm.Send<string>(String.Empty, master, requestURL);
                                break;
                            case killYourself:
                                Console.WriteLine("SLAVE " + icomm.Rank + ": KILLED");
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
