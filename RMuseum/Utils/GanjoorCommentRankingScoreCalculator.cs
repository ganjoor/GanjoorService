using System;

namespace RMuseum.Utils
{
    /// <summary>
    /// comment ranking score calculator
    /// </summary>
    public class GanjoorCommentRankingScoreCalculator
    {
        public static int ComputeRankingScore(int likes, int dislikes)
        {
            int score = likes - dislikes;
            int votes = likes + dislikes;

            int confidence =
                VoteConfidenceBonus[
                    Math.Min(votes, VoteConfidenceBonus.Length - 1)];

            return score * 1_000_000
                 + confidence * 1_000
                 + likes;
        }
        private static readonly short[] VoteConfidenceBonus =
        {
              0, //0 votes
            200, //1
            350, //2
            470, //3
            560, //4
            630, //5
            690, //6
            740, //7
            780, //8
            810, //9
            840, //10
            860, //11
            880, //12
            895, //13
            910, //14
            920, //15
            930, //16
            940, //17
            948, //18
            955, //19
            960  //20+
        };
    }
}
