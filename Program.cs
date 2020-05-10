using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

namespace Imen_Rim
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] stopWords = System.IO.File.ReadAllLines(@"C:\Users\saima\Documents\Visual Studio 2015\Projects\Imen_Rim\english.txt");

            System.IO.StreamReader file1 = new System.IO.StreamReader("C:\\Users\\saima\\Documents\\Visual Studio 2015\\Projects\\Imen_Rim\\test1.txt");
            string firstSentence = file1.ReadLine(); 

            /**Console.WriteLine("Enter First Sentence: ");
            string firstSentence = Console.ReadLine();**/
            
            string[] word1 = firstSentence.Split(new char[] { '.', '?', '!', ' ', ';', ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
            file1.Close();

            System.IO.StreamReader file2 = new StreamReader("C:\\Users\\saima\\Documents\\Visual Studio 2015\\Projects\\Imen_Rim\\test2.txt", Encoding.GetEncoding("iso-8859-1"));
            string secondSentence = file2.ReadToEnd(); 

            /**Console.WriteLine("Enter Second Sentence: ");
            string secondSentence = Console.ReadLine();**/

            string[]  word2 = secondSentence.Split(new char[] { '.', '?', '!', ' ', ';', ':', ',' }, StringSplitOptions.RemoveEmptyEntries);

            file2.Close();

            var filterdWords1 = word1.Except(stopWords);
            var filterdWords2 = word2.Except(stopWords);

            List<float> Collection_of_SimFA = new List<float>();
            List<string> wordOrder1 = new List<string>();
            List<string> wordOrder2 = new List<string>();
            List<float> SimValue = new List<float>();
            List<float> SemSim = new List<float>();
            List<string> WordOrderSet1 = new List<string>();
            List<string> WordOrderSet2 = new List<string>();

            foreach (string str1 in filterdWords1)
            {
                var result1 = filterdWords1.Select((x, i) => new { x, i })
                        .Where(x => x.x == str1)
                        .Select(x => x.i);
                foreach (string str2 in filterdWords2)
                {
                    var result2 = filterdWords2.Select((x, i) => new { x, i })
                            .Where(x => x.x == str2)
                            .Select(x => x.i);
                    SimValue = SimFA(str1, str2);

                    if (SimValue.Any(n => n >= 0) == true)
                    {
                        Collection_of_SimFA.Add(SimValue.FirstOrDefault());
                        float StoredScore = Collection_of_SimFA.Sum();
                        foreach (int i in result1)
                        {
                            foreach (int j in result2)
                            {
                                float value = StoredScore / Math.Min(i, j);
                                SemSim.Add(value);
                            }
                        }
                    }
                }
            }

            foreach(float value in SemSim)
            {
                Console.WriteLine("\n\nThe semantic similarity score between the two sentences: {0}", value);
            }

            var mc = word1.Intersect(word2);
            float Jaccard = (float)mc.Count() / (float)(word1.Count() + word2.Count() - mc.Count());

            WordOrderSet1 = WordOrderSet(word1);
            WordOrderSet2 = WordOrderSet(word2);

            foreach(string s in WordOrderSet1)
            {
                Console.WriteLine("\n\nWord order set for first sentence is: {0}", s);
            }

            foreach(string str in WordOrderSet2)
            {
                Console.WriteLine("\n\nWord order set for second sentecne is: {0}", str);
            }

            var intersect = WordOrderSet1.Intersect(WordOrderSet2);
            var union = WordOrderSet1.Union(WordOrderSet2);

            float SimWordOrderSet = (float)intersect.Count() / (float)union.Count();

            float SynSim = Jaccard + SimWordOrderSet;
            Console.WriteLine("\n\nThe syntactic similarity score between the two sentences: {0}", SynSim);

            Console.WriteLine("\n\nThe sentence similarity measure: ");
            float SenSimSet = 0;
            for (int k = 0; k <= 1; k++)
            {
                foreach(float f in SemSim)
                {
                    SenSimSet = (float)(k * f) + (float)((1 - k) * SynSim);
                }
            }
            Console.WriteLine(SenSimSet);
        }

        static List<float> SimFA(string filterdWords1, string filterdWords2)
        {
            List<string> wordList1 = new List<string>();
            List<string> wordList2 = new List<string>();
            List<float> SimFA = new List<float>();
            WebClient web = new WebClient();
            int Hw1;
            int Hw2;
            int H;

            wordList1 = SemanticAtlas(filterdWords1);
            foreach(string s1 in wordList1)
            {
                Console.WriteLine("\nWord list from online dictionary for word1: {0}:{1}", filterdWords1, s1);
            }
            wordList2 = SemanticAtlas(filterdWords2);
            foreach(string str in wordList2)
            {
                Console.WriteLine("\nWord list from online dictionary for word2: {0}:{1}", filterdWords2, str);
            }

            var wordlist = wordList1.Intersect(wordList2);

            float s = (float)(wordlist.Count()) / (float)(wordList1.Count + wordList2.Count - (wordlist.Count()));

            Hw1 = PageCount(filterdWords1);
            Console.WriteLine("\nPage Count from Digg.com for word1: {0}={1}", filterdWords1, Hw1);
            Hw2 = PageCount(filterdWords2);
            Console.WriteLine("\nPage Count from Digg.com for word2: {0}={1}", filterdWords2, Hw2);

            string wordSet = filterdWords1 + "," + " " + filterdWords2;

            H = PageCount(wordSet);
            Console.WriteLine("\nPage Count for (word1,word2): ({0},{1})={2}", filterdWords1, filterdWords2, H);

            float WebJaccard = (float)(H) / (float)(Hw1 + Hw2 - H);

            Console.WriteLine("\n\nWord pairs are: {0}, {1}", filterdWords1, filterdWords2);
            for (int i = 0; i <= 1; i++)
            {
                float Sim = i * s + (1 - i) * WebJaccard;
                Console.WriteLine("SimFA is: {0}", Sim);
                SimFA.Add(Sim);
            }

            return SimFA;
        }

        static List<string> SemanticAtlas(string word)
        {
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            List<string> wordList = new List<string>();
            WebClient web = new WebClient();

            //**Process.Start("http://dico.isc.cnrs.fr/dico/en/search?b=2&r=" + word);**//
            byte[] byteArray1 = web.DownloadData(new Uri("http://dico.isc.cnrs.fr/dico/en/search?b=2&r=" + word));
            Stream stream1 = new MemoryStream(byteArray1);
            htmlDoc.Load(stream1);
            var attr1 = htmlDoc.DocumentNode.SelectNodes("//a[@href]")
                .Where(g => g.Attributes["href"].Value.StartsWith("search?b=2&r="))
                .Select(h => h.InnerText)
                .ToList<string>();

            foreach (string str in attr1)
            {
                wordList.Add(str);
            }

            return wordList;
        }

        static int PageCount(string word)
        {
            List<string> words = new List<string>();
            HtmlDocument doc = new HtmlDocument();
            WebClient web = new WebClient();
            int Count = 0;

            //Process.Start("http://digg.com/search?q=" + word);
            string html = web.DownloadString("http://digg.com/search?q=" + word);
            string regex3 = "<span [^>]*class=(\"|')object-hed-search-results-count(\"|')>(.*?)</span>";
            MatchCollection m1 = new Regex(regex3, RegexOptions.Singleline | RegexOptions.Compiled).Matches(html);

            foreach (Match m in m1)
            {
                string list = m.Groups[0].Value;
                words.Add(list);
            }


            for (int i = 0; i < words.Count; i++)
            {
                doc.LoadHtml(words[i]);
                var itemList3 = doc.DocumentNode.SelectNodes("//span[@class='object-hed-search-results-count']")
                  .Select(p => p.InnerText)
                  .ToList();
                foreach (var str in itemList3)
                {
                    if (itemList3.Contains(str) == true)
                    {
                        var resultString = Regex.Match(str, @"\d+").Value;
                        Count = Int32.Parse(resultString);
                    }
                    else
                    {
                        Count = 0;
                    }
                }
            }
            return Count;
        }

        static List<string> WordOrderSet(string[] word)
        {
            List<string> WordOrderSet = new List<string>();

            for (int i = 0; i < word.LongLength - 1; i++)
            {
                for (int j = 1; j < word.LongLength; j++)
                {
                    string temp1 = word[i];
                    string temp2 = word[j];

                    if (temp1 != temp2)
                    {
                        WordOrderSet.Add(temp1 + "," + temp2);
                    }
                }
            }

            return WordOrderSet;
        }
    }
}
