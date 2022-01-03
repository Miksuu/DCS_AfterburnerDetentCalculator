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
            // (I looked up mines from the VPC software)

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

            // #############################################

            // Calculate a percentage out of those 
            float detentPercentage = (float)detentValue / (float)totalValue;

            // Used for calculating the planes multiplier (is 1 when your throttle has 16384 axis values)
            float multiplier = totalValue / originalValue;

            // ## CURRENTLY TESTED PLANES: FA-18C, F-16, MiG-21 ##
            // WIP: Create a dictionary of the planes, will tell all of the planes values at once then to replace in the configs
            // Where the afterburner lights,
            // FA-18C: 12800 // Might be a bit too high on hornet, f16, since the game seems to have a little detent on the plane
            // F16 : 12950
            // MiG-21: 15060
            int planeAfterburnerValue = 12800 * (int)multiplier; // Maybe get rid of the castings later on, use floats only?

            // Offset, not needed, at least on my throttle, shouldn't be needed if the afterburner ignition values above are correct
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

            // This might not be the best way to go around the problem, but happens really rarely too,
            // Only if the number between your actual detent and planes is more than 10%
            // Tested to work with mig21, which has insanely far AB pos in game (+93%?~~) while hornet etc have like 80%
            for (int i = 0; i < userCurve.Length; i++)
            {
                if (userCurve[i] > 1)
                {
                    // Move to function
                    float mean = (userCurve[i + 1] + userCurve[i - 1]) / 2;
                    userCurve[i] = mean;
                }
            }

            // Starts setting up the hard detent positions
            userCurve[0] = 0f;
            userCurve[1] = hardDententValuePercentage / 2f;

            /*
            // Debug
            for (int i = 0; i < userCurve.Length; i++)
            {
                Console.WriteLine(userCurve[i]);
            } */

            // How many times the means of the curve values will be calculated (15 maybe too unnecessary??)
            int smoothingIterations = 15;

            // Calculates the point which it will flatten out the curve
            int countHowManyBeforeTheSoftDetent = 0;

            for (int i = 0; i < userCurve.Length; i++)
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
                for (int i = 0; i < countHowManyBeforeTheSoftDetent; i++)
                {
                    // Skips the first and 2nd element, the hard detent is defined already between 0-1 (curve number 1-2 in configs)
                    if (i > 1)
                    {
                        // Move to function
                        float mean = (userCurve[i + 1] + userCurve[i - 1]) / 2;
                        userCurve[i] = mean;
                    }
                }
            }

            // Prints the results, place them to the DCS config or JoyPro etc.
            Console.WriteLine("Here's your curve for whatever plane you setup as the value: ");
            for (int i = 0; i < userCurve.Length; i++)
            {
                Console.WriteLine(userCurve[i]);
            }
        }
    }
}