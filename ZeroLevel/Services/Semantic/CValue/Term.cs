using System;

namespace ZeroLevel.Services.Semantic.CValue
{
    public class Term
    {
        private String term;
        private float score;


        public Term()
        {

        }

        public Term(String pTerm)
        {
            term = pTerm;
            score = -1;
        }

        public Term(String pTerm, float pScore)
        {
            term = pTerm;
            score = pScore;
        }

        public String getTerm()
        {
            return term;
        }

        public void setTerm(String term)
        {
            this.term = term;
        }

        public float getScore()
        {
            return score;
        }

        public void setScore(float score)
        {
            this.score = score;
        }

        public override string ToString()
        {
            return term + "\t" + score;
        }


        public override bool Equals(object obj)
        {
            return Equals(obj as Term);
        }

        private bool Equals(Term other)
        {
            if (other == null) return false;
            return this.term.Equals(other.term, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            int hash = 7;
            hash = 97 * hash + this.term.GetHashCode();
            return hash;
        }
    }
}
