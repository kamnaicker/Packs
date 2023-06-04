using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Text;

namespace Packer
{
    public class APIException : Exception
    {
        public APIException(string message): base(message)
        {

        }
    }

    public static class PackerAPI    
    {
        
        static string Pack(string filepath)
        {
            //String Array to store the lines of data in the file
            string[] data;
            
            //Regular Expressions for Finding the first value (MaxWeight) in the UTF-8 File
            Regex RegexMaxWeight = new Regex(@"(?<MaxWeight>\d+)\s*:\s*");
            
            //Regular Expressions for Finding the Package Parameters one or more times enclosed in () in the UTF-8 File
            Regex RegexPackParams = new Regex(@"(\((?<Index>\d+),(?<Weight>[0-9.]+),€(?<Cost>\d+)\))+");
            
            //Custom Type List of data extracted from the UTF-8 File
            List<(int MaxWeight, int Index, decimal Weight, int Cost)> dataExtracted = new();

           

            //Try catch for opening the file, if doesn't exits throw error
            try
            {
                //get the data into an array of each line in file
                data = File.ReadAllLines($"{filepath}");
                                
                //Try Match on Regex if file is empty throw error
                try
                {
                    //iterate through each line of data
                    foreach (string entry in data)
                    {
                        //start setting the MaxWeight as type Match for each line of data as there is only one occurrence per line
                        Match MaxWeightMatch = RegexMaxWeight.Match(entry);
                        int MaxWeight = int.Parse(MaxWeightMatch.Groups["MaxWeight"].Value);

                        //start building the Package Parameters collection of type MatchCollection from the data entries as theire are multiple parameters per line
                        MatchCollection PackParametersMatches = RegexPackParams.Matches(entry);


                        //iterate through each Package Parameter of Max Weight set earlier
                        foreach (Match match in PackParametersMatches)
                        {
                            //set values according to match groups setup in the Regex, Parse them to datatypes specified in list as they will be in the type Match                         

                            int Index = int.Parse(match.Groups["Index"].Value);
                            decimal Weight = decimal.Parse(match.Groups["Weight"].Value);
                            int Cost = int.Parse(match.Groups["Cost"].Value);
                            try
                            {
                                //Constraints if Package weight is greater than 100 or less than 0, then invalid skip over, and item cost less than 0 or greater than 100, then invalid
                                if ((MaxWeight < 0 || MaxWeight > 100))
                                {
                                    throw new APIException($"Package {Index}: Maximum Weight {MaxWeight},does not meet the Max/Min Weight Range Requirements (0-100) of the Package");
                                }
                                else if ((Cost < 0 || Cost > 100))
                                {

                                    throw new APIException($"Package {Index}: Cost {Cost},does not meet the Max/Min Cost Range Requirements (0-100) of the Package");
                                }
                                else
                                {
                                    // Add the extracted data to the dataExtracted list collection
                                    dataExtracted.Add((MaxWeight, Index, Weight, Cost));
                                }
                            }
                            catch (APIException ex)
                            {
                                Console.WriteLine(ex);
                                Console.WriteLine();
                            }

                        }

                    }
                }
                catch (ArgumentNullException e)
                {
                    Console.WriteLine(e);
                }

                /*created a variable to store the grouped data, found a method using the LINQ library to define a parameter Maxweight, and using
                .GroupBy would group the data based on the passed parameter pack.Maxweight
                Realise that the previous list dataExtracted is already grouped by maxWeight but in order to set the group Key and use the query, i will need to set the groups*/

                var GroupOfMaxWeight = dataExtracted.GroupBy(pack => pack.MaxWeight);

                //define and instantiate a new StringBuilder object, which will allow the appending of lines in the Foreach loop on final packages as original string output
                //was being overwritten in the return value, this was used instead of string.concat as it seems more efficient to use a class that already performs string manipulation
                //without the need for using the Environment.newline multiple time with String.Concat
                StringBuilder output = new();

                //The foreach loop will iterate through the List of grouped data
                foreach (var group in GroupOfMaxWeight)
                {
                    //ran a LINQ Query to Sort the data where the pack.weight is less than or equal to the group.Key - pack.MaxWeight, and order the list
                    //by descending pack.cost for the highest cost first, then return a list with the .ToList() method from IEnumerable
                    var items = group.Where(pack => pack.Weight <= group.Key)
                                     .OrderByDescending(pack => pack.Cost)
                                     .ToList();
                    //Set FinalPackages equal to the FindIndex, when passing our items list and group.key to the Find Index Method
                    var finalPackages = FindIndex(items, group.Key);
                    
                    //Check if finalPackages has a value if not the output "-"
                    if(finalPackages.Count == 0)
                    {
                        output.AppendLine("-");
                    }
                    else
                    {
                        //If FinalPackages has value then add to string builder and append further values
                        output.AppendLine(string.Join(", ", finalPackages));
                        output.AppendLine();//Extra Line between values
                    }                  
                                 
                }
                //Set StringBuilder to a String and Return value
                return output.ToString();
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e);
                return null;
            }


        }
        /*Static Method that will return a List of Indexes given the required Constraints: Maximum Cost, Weight less than or equal to the Package MaxWeight
        Method takes var list items as the sortedList and int MaxWeight as Max Weight*/

        static List<int> FindIndex(List<(int MaxWeight, int index, decimal weight, int cost)> sortedList, int maxWeight)
        {
            //Creating the Decimal for comparing the Weight of the items with the MaxWeight
            decimal currentWeight = maxWeight;
            //Created the return List for indexes
            List<int> SortedPackages = new();

            /*For Loop will check if the sortedList (sorted by Cost, Grouped by Max Weight) which is var index, has an item weight that is greater than the Maxweight of the Package
            if the weight is greater than max weight it is ignored, if not it will add that item into
            Sorted packages since it is both under the weight limit and the Maximum Cost possible*/
            try
            {
                foreach (var sorted in sortedList)
                {
                    if (sorted.weight <= currentWeight)
                    {
                        SortedPackages.Add(sorted.index);
                        currentWeight -= sorted.weight;
                    }
                    else
                    {
                        throw new APIException($"Package {sorted.index}: Maximum Weight of Package Reached");
                    }
                }
            }
            catch (APIException ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine();
            }
            
            //Return the list of Indexes
            return SortedPackages;
        }
    }
}
