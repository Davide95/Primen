using MPI;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Primen
{
    /// <summary>
    /// Implements trial division, an integer factorization algorithm.
    /// </summary>
    internal class TrialDivision
    {
        /// <summary>
        /// Initializes a new instance of the <c>TrialDivision</c> class.
        /// </summary>
        /// <param name="key">The product of two prime numbers.</param>
        public TrialDivision(BigInteger key)
        {
            n = BigInteger.Abs(key);
            isFinished = !IS_FINISHED;
        }

        /// <summary>
        /// Factorizes the key.
        /// </summary>
        /// <returns>If it is the root rank returns one factor, otherwise 0.</returns>
        /// <exception cref="System.ArithmeticException">Thrown when the key to factorize is zero or one.</exception>
        public BigInteger Factorization()
        {
            // Zero and one can't be factorized.
            if (n.IsZero || (n == BigInteger.One))
                throw new ArithmeticException("NaN");

            else if (n == 2)
                return BigInteger.One;

            else if (n % 2 == 0)
                return 2;

            var sqrtOfN = n.Sqrt();
            if (sqrtOfN < MIN_FROM)
                return BigInteger.One;

            // If there are only one (or two) nodes, it uses only tasks parallelization.
            if (Communicator.world.Size <= 2)
            {
                if (Communicator.world.Rank == Program.ROOT_RANK)
                    return ParallelFactorization(MIN_FROM, sqrtOfN);

                else
                    // Only root rank know the result.
                    return NOT_VALID_FACTOR;
            }
                
            return MpiFactorization();
        }

        /// <summary>
        /// Uses <c>MPI.NET</c> to support parallel computing.
        /// </summary>
        /// <returns>If it is the root rank returns one factor, otherwise 0.</returns>
        private BigInteger MpiFactorization()
        {
            var sqrtOfN = n.Sqrt();

            // Rank 0 doesn't calculate anything.
            var worldSize = Communicator.world.Size - 1;
            var myRank = Communicator.world.Rank - 1;
            var blockSize = sqrtOfN / worldSize;

            // If the key is too small, it uses only tasks parallelization.
            if (blockSize < MIN_FROM)
            {
                if (Communicator.world.Rank == Program.ROOT_RANK)
                    return ParallelFactorization(MIN_FROM, sqrtOfN);

                // Only root rank know the result.
                return NOT_VALID_FACTOR;
            }

            if (Communicator.world.Rank == Program.ROOT_RANK)
            {
                // Waiting for other sources.
                do
                {
                    BigIntegerSerializable factorS;
                    Communicator.world.Receive<BigIntegerSerializable>(
                        MPI.Communicator.anySource, FACTOR_TAG, out factorS);

                    BigInteger factor = factorS;
                    // If it has received a valid factor from one node, it stop all the task from all nodes.
                    if (!factor.IsOne)
                    {
                        for (int rank = Program.ROOT_RANK + 1; rank < Communicator.world.Size; rank++)
                            Communicator.world.ImmediateSend(IS_FINISHED, rank, STOP_TAG);

                        return factor;
                    }

                    worldSize--;
                } while (worldSize != 0);

                // If there aren't factor, it is prime.
                return BigInteger.One;
            }
            else
            {
                // It waits when one node finds the factor.
                Task.Run(() =>
                {
                    isFinished = Communicator.world.Receive<bool>(Program.ROOT_RANK, STOP_TAG);
                });

                var from = (myRank == Program.ROOT_RANK) ? MIN_FROM : (myRank * blockSize);
                var to = (myRank == worldSize - 1) ? (sqrtOfN-1) : ((myRank + 1) * blockSize);

                Communicator.world.Send((BigIntegerSerializable)ParallelFactorization(from, to), 0, FACTOR_TAG);

                // Only root rank know the result.
                return NOT_VALID_FACTOR;
            }
        }

        /// <summary>
        /// Uses <c>Parallel.For</c> to support tasks parallelization.
        /// </summary>
        /// <param name="from">The start index, inclusive.</param>
        /// <param name="to">The end index, inclusive.</param>
        /// <returns>Returns one factor.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when <c>from < MIN_FROM</c>, or <c>from > to</c>, or <c>to > n</c>.
        /// </exception>
        private BigInteger ParallelFactorization(BigInteger from, BigInteger to)
        {
            if (from < MIN_FROM)
                throw new ArgumentOutOfRangeException("from");
            if (from > to)
                throw new ArgumentOutOfRangeException("from");
            if (to > n)
                throw new ArgumentOutOfRangeException("to");

            var blockSize = (to - from) / System.Environment.ProcessorCount;

            // If the key is too small, it doesn't use tasks parallelization.
            if (blockSize < MIN_FROM)
                return Factorization(from, to);

            BigInteger factor = BigInteger.One;
            Parallel.For(0, System.Environment.ProcessorCount, (i, loopState) =>
                {
                    var fromP = from + i * blockSize;
                    var toP =  (i == System.Environment.ProcessorCount - 1) ? to : (from + (i + 1) * blockSize);

                    var result = Factorization(fromP, toP, ref loopState);

                    if ((!result.IsOne) && (!result.IsZero))
                        factor = result;
                });

            return factor;
        }

        /// <summary>Check if there is a factor in [<c>from</c>; <c>to</c>] range.</summary>
        /// <param name="from">The start index, inclusive.</param>
        /// <param name="to">The end index, inclusive.</param>
        /// <returns>Returns one factor.</returns>
        private BigInteger Factorization(BigInteger from, BigInteger to)
        {
            ParallelLoopState fakeLoop = null;
            return Factorization(from, to, ref fakeLoop);
        }

        /// <summary>Check if there is a factor in [<c>from</c>; <c>to</c>] range.</summary>
        /// <param name="from">The start index, inclusive.</param>
        /// <param name="to">The end index, inclusive.</param>
        /// <param name="loopState">The <c>ParallelLoopState</c> of the main <c>Parallel.For</c>.</param>
        /// <returns>Returns one factor.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when <c>from < MIN_FROM</c>, or <c>from > to</c>, or <c>to > n</c>.
        /// </exception>
        private BigInteger Factorization(BigInteger from, BigInteger to,
            ref ParallelLoopState loopState)
        {
            if (from < MIN_FROM)
                throw new ArgumentOutOfRangeException("from");
            if (from > to)
                throw new ArgumentOutOfRangeException("from");
            if (to > n)
                throw new ArgumentOutOfRangeException("to");

            while(from <= to)
            {
                // If the parallel loop is stopped, it returns 0.
                if ((loopState != null) && (loopState.IsStopped))
                    return NOT_VALID_FACTOR;

                // If someone has found one factor, all ranks have to stop.
                if (isFinished)
                    return NOT_VALID_FACTOR;

                if((n % from).IsZero)
                {
                    if(loopState != null)
                        loopState.Stop();

                    return from;
                }

                from += 2;
            }

            // If it didn't find any factor, it returns 1.
            return BigInteger.One;
        }

        public const int NOT_VALID_FACTOR = 0;

        private BigInteger n;
        private bool isFinished;

        private const int MIN_FROM = 3;
        private const int FACTOR_TAG = 1;
        private const int STOP_TAG = 2;
        private const bool IS_FINISHED = true;
    }
}
