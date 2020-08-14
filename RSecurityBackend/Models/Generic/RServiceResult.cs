namespace RSecurityBackend.Models.Generic
{
    /// <summary>
    /// a generic class containing service method return values + an exception string as a more readable replacement for Tuple 
    /// and also a solution for writing async methods which do not permit using out parameters
    /// </summary>
    public class RServiceResult<T>
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="result"></param>
        /// <param name="exceptionString"></param>
        public RServiceResult(T result, string exceptionString = "")
        {
            Result = result;
            ExceptionString = exceptionString;
        }

        /// <summary>
        /// Actual result
        /// </summary>
        public T Result { get; set; }

        /// <summary>
        /// Exception String
        /// </summary>
        public string ExceptionString { get; set; }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(ExceptionString))
                return ExceptionString;
            return Result.ToString();
        }
    }
}
