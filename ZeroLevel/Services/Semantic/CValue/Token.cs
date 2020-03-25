using System;

namespace ZeroLevel.Services.Semantic.CValue
{
    public class Token
    {
        private String wordForm;
        private String posTag;
        private String chunkerTag;
        private String lemma;
        private int pos; //position inside the sentence?

        public Token(String pWordForm)
        {
            wordForm = pWordForm;
        }

        public Token(String pWordForm, String pPostag)
        {
            wordForm = pWordForm;
            posTag = pPostag;
        }

        public Token(String pWordForm, String pPostag, String pLemma)
        {
            wordForm = pWordForm;
            posTag = pPostag;
            lemma = pLemma;
        }

        public Token(String pWordForm, String pPostag, String pLemma, String pChunker)
        {
            wordForm = pWordForm;
            posTag = pPostag;
            lemma = pLemma;
            chunkerTag = pChunker;
        }

        public String getWordForm()
        {
            return wordForm;
        }

        public void setWordForm(String wordForm)
        {
            this.wordForm = wordForm;
        }

        public String getPosTag()
        {
            return posTag;
        }

        public void setPosTag(String posTag)
        {
            this.posTag = posTag;
        }

        public override string ToString()
        {
            return wordForm + "\t" + posTag;
        }

        public String getLemma()
        {
            return lemma;
        }

        public void setLemma(String lemma)
        {
            this.lemma = lemma;
        }

        public String getChunkerTag()
        {
            return chunkerTag;
        }

        public void setChunkerTag(String chunkerTag)
        {
            this.chunkerTag = chunkerTag;
        }
    }
}
