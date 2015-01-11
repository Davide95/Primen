using MPI;
using Primen.Properties;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using SQLite;
using System.Linq;
using System.Collections.Generic;

namespace Primen
{
    static class Program
    {
        static void Main(string[] args)
        {
            using (var mpi = new MPI.Environment(ref args))
            {
                WelcomeMessage();

                // The process has the highest possible priority.
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

                BigInteger key = readKey();

                db = Database.getDatabase();
                var factors = (from rainbowItem in db.Table<RainbowTable>()
                             where (BigInteger)rainbowItem.Key == key
                             select rainbowItem.Factor).Take(1);

                switch(factors.Count())
                {
                    case 0:
                        Factorization(key);
                        break;
                        
                    default:
                        var factor = factors.First();

                        Console.WriteLine(String.Format(CultureInfo.CurrentCulture,
                            Resources.FactorizationCompletedMessage, factor, key / factor));
                        break;
                }

                Console.ReadLine();
                Communicator.world.Abort(0);
            }
        }

        /// <summary>
        /// Shows a welcome message with informations about the MPI world.
        /// </summary>
        private static void WelcomeMessage()
        {
            Console.WriteLine(
                    String.Format(CultureInfo.CurrentCulture, Resources.WelcomeMessage
                    , Communicator.world.Rank, MPI.Environment.ProcessorName));

            if (Communicator.world.Rank == ROOT_RANK)
                Console.WriteLine(String.Format(CultureInfo.CurrentCulture,
                    Resources.NumberOfProcessesMessage, Communicator.world.Size));
        }

        /// <summary>Read the key to factorize from the command line arguments.</summary>
        /// <returns>Returns the key to factorize.</returns>
        private static BigInteger readKey()
        {
            string[] args = System.Environment.GetCommandLineArgs();

            if (args.Length < 2)
            {
                if (Communicator.world.Rank == ROOT_RANK)
                {
                    Console.WriteLine(Resources.Error111);
                    Communicator.world.Abort(111);
                }
            }

            BigInteger key;
            if (!BigInteger.TryParse(args[1], out key))
            {
                if (Communicator.world.Rank == ROOT_RANK)
                { 
                    Console.WriteLine(String.Format(CultureInfo.CurrentCulture,
                        Resources.Error112, args[1]));
                    Communicator.world.Abort(112);
                }
            }

            return key;
        }

        private static void Factorization(BigInteger key)
        {
            var trialDivision = new TrialDivision(key);

            Stopwatch rootRankWatch = null;
            if (Communicator.world.Rank == ROOT_RANK)
                rootRankWatch = Stopwatch.StartNew();

            BigInteger factor = BigInteger.Zero;
            try
            {
                factor = trialDivision.Factorization();
            }
            catch (ArithmeticException)
            {
                Console.WriteLine(String.Format(CultureInfo.CurrentCulture,
                    Resources.Error113, key));
                Communicator.world.Abort(113);
            }

            if (Communicator.world.Rank == ROOT_RANK)
            {
                rootRankWatch.Stop();

                Console.WriteLine(String.Format(CultureInfo.CurrentCulture,
                   Resources.FactorizationCompletedMessage, factor, key / factor));

                var rainbowItem = new RainbowTable();
                rainbowItem.Key = BigInteger.Abs(key);
                rainbowItem.Factor = factor;
                rainbowItem.TimeOfCalculation = rootRankWatch.Elapsed;
                db.Insert(rainbowItem);
            }
        }

        public const int ROOT_RANK = 0;

        private static Database db;
    }
}
