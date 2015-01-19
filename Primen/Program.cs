using MPI;
using Primen.Properties;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Primen
{
    static class Program
    {
        static void Main(string[] args)
        {
            using (new MPI.Environment(ref args))
            {
                // The process has the highest possible priority.
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

                WelcomeMessage();

                while(true)
                {
                    BigInteger key = ReadKey();

                    var trialDivision = new TrialDivision(key);

                    BigInteger factor;

                    Stopwatch swFactorization = null;
                    if (Communicator.world.Rank == ROOT_RANK)
                        swFactorization = Stopwatch.StartNew();
                        
                    try
                    {
                        factor = trialDivision.Factorization();
                    }
                    catch(ArithmeticException)
                    {
                        factor = key;
                    }

                    if (Communicator.world.Rank == ROOT_RANK)
                    {
                        swFactorization.Stop();

                        Console.WriteLine(String.Format(CultureInfo.CurrentCulture,
                                Resources.FactorizationCompletedMessage, factor, key / factor));

                        Console.WriteLine(String.Format(CultureInfo.CurrentCulture,
                            Resources.TimeElapsedMessage, swFactorization.Elapsed));

                        Console.WriteLine();
                    }

                    Communicator.world.Barrier();
                }
            }
        }

        /// <summary>
        /// Shows info about the MPI world.
        /// </summary>
        private static void WelcomeMessage()
        {
            Console.WriteLine(String.Format(
                        CultureInfo.CurrentCulture, Resources.WelcomeMessage, 
                        Communicator.world.Rank, MPI.Environment.ProcessorName));

            Communicator.world.Barrier();

            if (Communicator.world.Rank == ROOT_RANK)
            {
                Console.WriteLine(String.Format(
                    CultureInfo.CurrentCulture, Resources.NumberOfProcessesMessage,
                    Communicator.world.Size));
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Reads the key from the standard input stream.
        /// </summary>
        /// <remarks>
        /// If the standard input stream is empty it aborts all ranks.
        /// </remarks>
        /// <returns>Returns a valid key.</returns>
        private static BigInteger ReadKey()
        {
            string input;
            var key = BigInteger.Zero;

            if(Communicator.world.Rank == ROOT_RANK)
            {
                do
                {
                    Console.Write(Resources.InsertKeyMessage);
                    input = Console.ReadLine();

                    if (String.IsNullOrEmpty(input))
                        Communicator.world.Abort(NO_ERRORS);

                } while(!BigInteger.TryParse(input, out key));
            }

            // It sends the key to all ranks.
            Communicator.world.Broadcast(ref key, ROOT_RANK);

            return key;
        }

        public const int ROOT_RANK = 0;
        private const int NO_ERRORS = 0;
    }
}
