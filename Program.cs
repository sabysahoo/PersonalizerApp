using Microsoft.Azure.CognitiveServices.Personalizer;
using Microsoft.Azure.CognitiveServices.Personalizer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Dynamic.Core;
using System.Linq;
using System.Dynamic;
using FastMember;

namespace PersonalizerExample
{
    class Program
    {
        // The key specific to your personalizer service instance being derived from MyDocuments folder."
        private static string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static string fullName = System.IO.Path.Combine(documentsPath, "PersonalizeDocs/Secrets.txt");
        private static string key1 = File.ReadLines(fullName).First();
        private static string[] token = key1.Split(':');
        private static string ApiKey = token[1];

        // The endpoint specific to your personalizer service instance; e.g. https://westus2.api.cognitive.microsoft.com/
        private const string ServiceEndpoint = "https://westus2.api.cognitive.microsoft.com/";

        static void Main(string[] args)
        {
            int iteration = 1;
            bool runLoop = true;

            string csvPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string csvFile = System.IO.Path.Combine(csvPath, "PersonalizeDocs/Volkswagen-Models.csv");


            List<List<string>> csvFileRead = readCSV(csvFile);
            //printCSV(csvFileRead);

            // Get the actions list to choose from personalizer with their features.
            IList<RankableAction> actions = GetActions(csvFileRead);
            //foreach (RankableAction s in actions){ Console.WriteLine(s.Id);
                //foreach (object i in s.Features) Console.WriteLine(i);} 

            // Initialize Personalizer client.
            PersonalizerClient client = InitializePersonalizerClient(ServiceEndpoint);

            do
            {
                Console.WriteLine("\nIteration: " + iteration++);

                // Get context information from the user.
                string typeOfCarFeature = GetUsersCarChoice();
                string personalityFeature = GetUsersCarFeatures();

                // Create current context from user specified data.
                string feat1 = csvFileRead[0][1];
                string feat2 = csvFileRead[0][2];

                //Console.WriteLine(feat1 + "  " + feat2);

                IList<object> currentContext = new List<object>() {
                    new { feat1 = typeOfCarFeature },
                    new { feat2 = personalityFeature }
                }; //to print
                //foreach (object k in currentContext){Console.WriteLine(k);}

                // Exclude an action for personalizer ranking. This action will be held at its current position.
                IList<string> excludeActions = new List<string> { "juice" };

                // Generate an ID to associate with the request.
                string eventId = Guid.NewGuid().ToString();

                // Rank the actions
                var request = new RankRequest(actions, currentContext, excludeActions, eventId);

                RankResponse response = client.Rank(request);

                Console.WriteLine("\nPersonalizer service thinks you would like to have: " + response.RewardActionId + ". Is this correct? (y/n)");

                float reward = 0.0f;
                string answer = GetKey();

                if (answer == "Y")
                {
                    reward = 1;
                    Console.WriteLine("\nGreat! Enjoy your car.");
                }
                else if (answer == "N")
                {
                    reward = 0;
                    Console.WriteLine("\nYou didn't like the recommended car.");
                }
                else
                {
                    Console.WriteLine("\nEntered choice is invalid. Service assumes that you didn't like the recommended car.");
                }

                Console.WriteLine("\nPersonalizer service ranked the actions with the probabilities as below:");
                foreach (var rankedResponse in response.Ranking)
                {
                    Console.WriteLine(rankedResponse.Id + " " + rankedResponse.Probability);
                }

                // Send the reward for the action based on user response.
                client.Reward(response.EventId, new RewardRequest(reward));

                Console.WriteLine("\nPress q to break, any other key to continue:");
                runLoop = !(GetKey() == "Q");

            } while (runLoop);
        }

        static List<List<string>> readCSV(string fileName)
        {
            var reader = new StreamReader(File.OpenRead(fileName));
            List<List<string>> results = new List<List<string>>();
            int[] retArr = new int[2];
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (!String.IsNullOrWhiteSpace(line))
                {
                    List<string> values = line.Split(',').ToList();
                    results.Add(values);
                }
            }
            return results;
        }

        static int[] listDimens(List<List<string>> dataArray)
        {
            int[] resultArray = new int[2];
            int ySize = dataArray.Count;
            int xSize = dataArray[0].Count;
            resultArray[0] = xSize;
            resultArray[1] = ySize;
            return resultArray;
        }

        static void printCSV(List<List<string>> dataArray)
        {
            int[] dimenArray = listDimens(dataArray);
            Array.ForEach(dimenArray, Console.WriteLine);
            for (int i = 0; i < dimenArray[1]; i++)
            {
                for(int j = 0; j < dimenArray[0]; j++)
                {
                    Console.Write(dataArray[i][j] + "\t");
                }
                Console.WriteLine("");
            }
        }

        /// <summary>
        /// Initializes the personalizer client.
        /// </summary>
        /// <param name="url">Azure endpoint</param>
        /// <returns>Personalizer client instance</returns>
        static PersonalizerClient InitializePersonalizerClient(string url)
        {
            PersonalizerClient client = new PersonalizerClient(
                new ApiKeyServiceClientCredentials(ApiKey))
            { Endpoint = url };

            return client;
        }

        /// <summary>
        /// Get users car preference.
        /// </summary>
        /// <returns>Preference selected by the user.</returns>
        static string GetUsersCarChoice()
        {
            string[] carType = new string[] { "Sedan", "SUV", "Wagen", "Compact", "Convertible" };

            Console.WriteLine("\nWhat type of car do you like? (enter number)? 1. Sedan 2. SUV 3. Wagen 4. Compact 5. Convertible");
            if (!int.TryParse(GetKey(), out int timeIndex) || timeIndex < 1 || timeIndex > carType.Length)
            {
                Console.WriteLine("\nEntered value is invalid. Setting feature value to " + carType[0] + ".");
                timeIndex = 1;
            }

            return carType[timeIndex - 1];
        }

        /// <summary>
        /// Gets user car features.
        /// </summary>
        /// <returns>Car feature selected by the user.</returns>
        static string GetUsersCarFeatures()
        {
            string[] carFeatures = new string[] { "Compact" , "Performance" , "Midsize" , "Premium" , "Stylish" , "Family" , "Sports" , "Adventurous" };

            Console.WriteLine("\nWhat type of car features do you prefer (enter number)? 1. Compact 2. Performance 3. Midsize 4. Premium 5. Stylish 6. Family 7. Sports 8. Adventurous");
            if (!int.TryParse(GetKey(), out int tasteIndex) || tasteIndex < 1 || tasteIndex > carFeatures.Length)
            {
                Console.WriteLine("\nEntered value is invalid. Setting feature value to " + carFeatures[0] + ".");
                tasteIndex = 1;
            }

            return carFeatures[tasteIndex - 1];
        }

        /// <summary>
        /// Creates personalizer actions feature list.
        /// </summary>
        /// <returns>List of actions for personalizer.</returns>
        static IList<RankableAction> GetActions(List<List<string>> csvFile)
        {
            int xCount = 0;
            string actionName = csvFile[xCount][0];
            List<string> featureList = csvFile[xCount].GetRange(1, csvFile[0].Count - 1);
            //foreach (string s in featureList){   Console.Write(s + " ");} Console.WriteLine();

            xCount++;
            IList<RankableAction> actions = new List<RankableAction>();
            //RankableAction nextAction;
            while (xCount < csvFile.Count)
            {
                int yCount = 1;
                string actAdd = csvFile[xCount][0];
                IList<object> listFeatAdd = new List<object>();
                Dictionary<string, string> dic = new Dictionary<string, string>();
                //object nextFeat;
                while (yCount < csvFile[0].Count - 1)
                {
                    string key = csvFile[0][yCount];
                    string value = csvFile[xCount][yCount];
                    dic.Add(key, value);
                    yCount++;
                }
                foreach (var item in dic)
                {
                    listFeatAdd.Add(item);
                }
                actions.Add(new RankableAction { Id = actAdd, Features = listFeatAdd });
                xCount++;
            }
            return actions;
        }

        private static string GetKey()
        {
            return Console.ReadKey().Key.ToString().Last().ToString().ToUpper();
        }
    }
}