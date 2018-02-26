using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOG.Pathway.Client;
using System.Net;
using System.IO;

namespace BOG.Pathway.Client.Test.Load
{
    static class Program
    {
        static string userAccessToken = "User";
        static string adminAccessToken = "Admin";
        static string superAccessToken = "Super";
        static Pathway pathwayQ1 = new Pathway()
        {
            ID = "Q1",
            ReadToken = "Q1R",
            WriteToken = "Q1W",
            maxPayloads = 200,
            maxReferences = 10
        };
        static Pathway pathwayA1 = new Pathway()
        {
            ID = "A1",
            ReadToken = "A1R",
            WriteToken = "A1W",
            maxPayloads = 20,
            maxReferences = 10
        };

        static string[] uris = new string[] { "http://localhost:5670" };
        
        static void PromptToContinue()
        {
            Console.WriteLine("Press Enter to continue");
            Console.ReadLine();
        }

        static void Main(string[] args)

        {
            const string password = "TheSillyMaidensAllInTheColumn";
            const string salt = "Th77c324$";

            Console.WriteLine("Initialize");
            try
            {
                // Payloads and references are sent as Base64 with no protection, when no password nor salt is supplied.
                // Client client = new Client(uris, superAccessToken, adminAccessToken, userAccessToken);

                // Encrypted payloads and references are used when password and salt are supplied.
                Client client = new Client(uris, superAccessToken, adminAccessToken, userAccessToken, password, salt);

                Console.WriteLine("ServerReset()");
                client.ServerReset();
                Console.WriteLine("Add Pathway Q1");
                client.AddPathway(pathwayQ1);
                Console.WriteLine("Add Pathway A1");
                client.AddPathway(pathwayA1);

                Console.WriteLine("Create Pathway Q1");
                client.CreatePathway("Q1");

                Dictionary<string, string> References = new Dictionary<string, string>();
                References.Add("Test-01", "The quick brown fox jumped over the lazy dog's back");
                References.Add("Stats-127.0.0.1", "{ \"Some numbers\": 2047 }");
                StringBuilder dataSet = new StringBuilder();
                dataSet.AppendLine("Column1\tColumn2\tColumn3");
                Random r = new Random(DateTime.Now.Millisecond);
                for (int index = 1; index <= 12000; index++)
                {
                    dataSet.AppendLine(string.Format("{0}\t{1}\t{2}", index, r.Next(310, 799), r.Next(81500, 740000)));
                }
                References.Add("DataSet1", dataSet.ToString());
                Console.WriteLine($"Pathway Q1: Set references");
                foreach (var key in References.Keys)
                {
                    Console.WriteLine($"Pathway Q1: Add reference named {key}");
                    client.SetReference("Q1", key, References[key]);
                }
                Console.WriteLine("Pathway Q1: List References");
                Console.WriteLine(client.ListReferences("Q1"));
                Console.WriteLine("Pathway Q1: Get References");
                foreach (var key in References.Keys)
                {
                    Console.WriteLine(client.GetReference("Q1", key));
                }
                PromptToContinue();

                Console.WriteLine("Create Pathway A1");
                client.CreatePathway("A1");
                Console.WriteLine("Pathway A1: Set reference State :: fox");
                client.SetReference("A1", "Test01", "The quick red fox jumped over the lazy dog's back");
                Console.WriteLine("Pathway A1: Get Reference Test01");
                Console.WriteLine(client.GetReference("A1", "Test01"));

                Console.WriteLine(client.ListReferences("A1"));
                PromptToContinue();

                References.Add("StaticPayload", "The quick brown fox jumped over the lazy dogs");

                Console.WriteLine("Pathway Q1: Write 200 payloads");
                int len = References["DataSet1"].Length;
                Parallel.For(0, 200, index =>
                {
                    client.WritePayload("Q1", References["DataSet1"]);
                });
                //for (int index = 0; index < 200; index++)
                //{
                //    Console.Write($"... Write {index}: ");
                //    var resultCode = client.WritePayload("Q1", References["DataSet1"]);
                //    Console.WriteLine($"{resultCode}: {len}");
                //}
                PromptToContinue();
                Console.WriteLine("Clients");
                Console.WriteLine(client.ServerClients());
                Console.WriteLine(client.ServerSummary());
                Console.WriteLine("Pathway Q1: Read payloads");
                string[] readBacks = new string[200];

                //for (int index = 0; index < 200; index++)
                //{
                //    readBacks[index] = client.ReadPayload("Q1");
                //}
                Parallel.For(0, 200, index =>
                {
                    readBacks[index] = client.ReadPayload("Q1");
                });

                Console.WriteLine("Pathway Q1: Validate payloads");
                for (int index = 0; index < 200; index++)
                {
                    if (string.Compare(readBacks[index], References["DataSet1"], false) != 0)
                    {
                        using (StreamWriter sw = new StreamWriter("F:\\readback.txt"))
                        {
                            sw.Write(readBacks[index]);
                        }
                        using (StreamWriter sw = new StreamWriter("F:\\dataset1.txt"))
                        {
                            sw.Write(References["DataSet1"]);
                        }
                        throw new Exception($"Payload read value does not match write value @ {index}");
                    }
                }

                Console.WriteLine(client.ServerClients());
                PromptToContinue();

                Console.WriteLine(client.ServerSummary());
                PromptToContinue();

                Console.WriteLine("Delete Pathway Q1");
                client.DeletePathway("Q1");
                Console.WriteLine(client.ServerSummary());
                PromptToContinue();
                Console.WriteLine("Delete Pathway A1");
                client.DeletePathway("A1");
                Console.WriteLine(client.ServerSummary());
                PromptToContinue();

                Console.WriteLine("ServerReset()");
                client.ServerReset();
                Console.WriteLine(client.ServerClients());
                client.ServerReset();
                //client.ServerShutdown();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
            Console.WriteLine("Test is complete)");
            PromptToContinue();
        }
    }
}