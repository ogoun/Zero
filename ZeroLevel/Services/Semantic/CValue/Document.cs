using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZeroLevel.Services.Semantic.CValue
{
    public class Document
    {
        private String path;
        private List<String> sentenceList;
        private List<LinkedList<Token>> tokenList;
        private String name;
        private List<Term> termList;

        public Document(String pPath, String pName)
        {
            path = pPath;
            name = pName;
            termList = new List<Term>();
        }

        public String getPath()
        {
            return path;
        }

        public void setPath(String path)
        {
            this.path = path;
        }

        public List<String> getSentenceList()
        {
            return sentenceList;
        }

        public void setSentenceList(List<String> sentenceList)
        {
            this.sentenceList = sentenceList;
        }

        public List<LinkedList<Token>> getTokenList()
        {
            return tokenList;
        }

        public void List(List<LinkedList<Token>> tokenList)
        {
            this.tokenList = tokenList;
        }

        public String getName()
        {
            return name;
        }

        public void setName(String name)
        {
            this.name = name;
        }

        public List<Term> getTermList()
        {
            return termList;
        }

        public void setTermList(List<Term> termList)
        {
            this.termList = termList;
        }
    }
}
