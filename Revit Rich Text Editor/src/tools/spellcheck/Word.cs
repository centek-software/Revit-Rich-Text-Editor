using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTEK_Rich_Text_Editor.src.tools.spellcheck
{
    class Word
    {
        private List<string> suggestions;
        private bool spelledCorrect;

        public Word(bool correct, List<string> suggest)
        {
            spelledCorrect = correct;
            suggestions = suggest;
        }

        public List<string> getSuggestions()
        {
            return suggestions;
        }

        public bool spelledCorrectly()
        {
            return spelledCorrect;
        }
    }
}
