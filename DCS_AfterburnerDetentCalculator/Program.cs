using System;
using System.Collections.Generic;

namespace DCS_AfterburnerDetentCalculator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Tested using this, do not edit this!
            int originalValue = 16384;

            // ## EDIT THESE ACCORDING TO YOUR SETUP ######
            // (I looked up mine from the VPC software)

            // Throttle at 100%, the total range of the axis
            int totalValue = 16384;

            // The value of the physical detent on the throttle,
            // each user has their own value depending where their physical detent is located 
            int detentValue = 12330;

            // Set up your hard detent value that stops the engines from shutting down
            // The curve will try to optimise so when your physical throttle is at for example, 5%, hardstop,
            // can't continue without lifting detents, the plane will stay at idle throttle to avoid being moved on the ground
            // This is not tested properly yet, pretty much work in progress
            int hardDetentValue = 754;

            bool setHardDetentValue = false;

            // If false, throttle will go from 0 to 100 (left to right), otherwise 100 to 0 (right to left)
            // Set according to what type of throttle you have
            bool ReverseThrottle = true;

            // If true, prints the value with a dot (for pasting in to dcs config),
            // otherwise with a comma (for pasting to programs such as JoyPro)
            bool printForDcsConfigLuaFile = true;

            // #############################################

            // Calculate a percentage out of those 
            float detentPercentage = (float)detentValue / (float)totalValue;

            // Used for calculating the planes multiplier (is 1 when your throttle has 16384 axis values)
            float multiplier = totalValue / originalValue;

            // Aircraft afterburner values
            Dictionary<string, int> AircraftKVPs = new Dictionary<string, int>();
            AircraftKVPs.Add("FA-18C", 12800);
            AircraftKVPs.Add("F-16C", 12950);
            AircraftKVPs.Add("MiG-21", 14950);
            AircraftKVPs.Add("F-14", 13110); // Could tune this up to 13250, but the afterburner stays on after putting the throttle back to soft detent for some reason
            AircraftKVPs.Add("F-5E", 13380);
            AircraftKVPs.Add("AJS37", 13400);

            // Loops through all of the aircrafts
            foreach (KeyValuePair<string, int> kvp in AircraftKVPs)
            {
                int planeAfterburnerValue = kvp.Value * (int)multiplier; // Maybe get rid of the castings later on, use floats only?

                // Offset, not needed at least on my throttle, shouldn't be needed if the afterburner ignition values above are correct
                //planeAfterburnerValue = planeAfterburnerValue - 200;

                // Calculates all the necessary values for the curve optimisation
                float hardDententValuePercentage = (float)hardDetentValue / (float)totalValue;
                float afterburnerTurnOnPercentage = (float)planeAfterburnerValue / (float)totalValue;
                float modifyCurveBy = afterburnerTurnOnPercentage - detentPercentage;

                // Calculate the initial values
                // Idk if the reverse forloop is needed here anymore tbh, it's flipped to normal order for rest of the calculations
                float[] userCurve = new float[11];
                for (int i = userCurve.Length - 1; i >= 0; i--)
                {
                    if (i == userCurve.Length - 1)
                    {
                        userCurve[i] = 1;
                    }
                    else
                    {
                        userCurve[i] = (float)i / 10 + modifyCurveBy;
                    }
                }

                // Starts setting up the hard detent positions
                userCurve[0] = 0f;
                if (setHardDetentValue)
                {
                    userCurve[1] = hardDententValuePercentage / 2f;
                }

                // How many times the means of the curve values will be calculated
                int smoothingIterations = 120;

                // Calculates the point which it will flatten out the curve
                int countHowManyBeforeTheSoftDetent = 0;

                for (int i = 1; i < userCurve.Length; i++)
                {
                    if (userCurve[i] > afterburnerTurnOnPercentage)
                    {
                        break;
                    }
                    else countHowManyBeforeTheSoftDetent++;
                }

                // Smoothens the curve according to the int set earlier
                for (int s = 0; s < smoothingIterations; s++)
                {
                    for (int i = 1; i < countHowManyBeforeTheSoftDetent; i++)
                    {
                        // Skips the first and 2nd element, the hard detent is defined already between 0-1 (curve number 1-2 in configs)
                        if (i > 1)
                        {
                            userCurve[i] = CalculateMean(userCurve[i - 1], userCurve[i + 1]);
                        }
                        else if (!setHardDetentValue && i == 1)
                        {
                            userCurve[i] = CalculateMean(userCurve[i - 1], userCurve[i + 1]);
                        }
                    }
                }

                // If the curve goes over 1 at any point, correct it
                for (int i = 0; i < userCurve.Length; i++)
                {
                    if (userCurve[i] > 1)
                    {
                        userCurve[i] = 1-(userCurve[i - 1]- userCurve[i-2])/10;
                        //Console.WriteLine("calculated: " + userCurve[i]);
                    }
                }

                if (ReverseThrottle)
                {
                    userCurve = RevertCurve(userCurve);
                }

                // Prints the results, place them to program such as JoyPro etc (printed with commas)
                if (!printForDcsConfigLuaFile)
                {
                    Console.WriteLine(kvp.Key.ToString() + "'s curve with commas: ");
                    for (int i = 0; i < userCurve.Length; i++)
                    {
                        Console.WriteLine(userCurve[i]);
                    }
                }
                // Place in dcs config (printed with dots)
                else
                {
                    System.Globalization.NumberFormatInfo nfi = new System.Globalization.NumberFormatInfo();
                    nfi.NumberDecimalSeparator = ".";

                    Console.WriteLine(kvp.Key.ToString() + "'s curve with dots: ");
                    for (int i = 0; i < userCurve.Length; i++)
                    {
                        Console.WriteLine("[" + (i+1) + "] = " + userCurve[i].ToString(nfi) + ",");
                    }
                }
                Console.WriteLine();
            }
        }
        static float CalculateMean(float _previousNum, float _nextNum)
        {
            return (_previousNum + _nextNum) / 2;
        }

        // Reverses the curve for users thats throttles go from right to left
        static float[] RevertCurve(float[] _userCurve)
        {
            float[] newUserCurve = new float[_userCurve.Length];
            int valueToReplaceWith = _userCurve.Length-1;
            for (int i = 0; i < _userCurve.Length-1; i++)
            {
                newUserCurve[i] = 1 - _userCurve[valueToReplaceWith];
                valueToReplaceWith--;
            }
            newUserCurve[10] = 1;
            return newUserCurve;
        }
    }
}